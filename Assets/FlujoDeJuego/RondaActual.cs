using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Cinemachine;
using UnityEngine.AI;
using System.Linq;

public class RondaActual : NetworkBehaviour
{
    public static RondaActual actual;

    [SerializeField] CinemachineVirtualCamera camInicial, followCam;
    [SerializeField] NavMeshSurface surface;
    [SerializeField] MeshCollider meshCol;
    [SerializeField] LevelGenerator levelGenerator;

    [SerializeField, SyncVar] Vector3 gridPos;
    [SerializeField, SyncVar] Vector2Int gridSize;
    public ulong GridSizeX => (ulong)gridSize.x;
    public ulong GridSizeY => (ulong)gridSize.y;

    [SerializeField, SyncVar]
    bool rondaIniciada = false;
    Dictionary<JugadorMirror, WachinLogica> jugadoresIniciados = new Dictionary<JugadorMirror, WachinLogica>();
    List<JugadorMirror> jugadoresDerrotados = new List<JugadorMirror>();

    public float gridStepSize = .15f;
    public WachinLogica prefabJugador;
    public WachinLogica prefabGuardia;
    public Vector2Int cantGuardiasPorPatrulla = new Vector2Int(3, 6);
    public Vector2Int cantPatrullas = new Vector2Int(9, 15);
    public float distMinimaGuardias = 30f;

    Vector3 posInicial;

    public class LevelGeneratorData : NetworkMessage
    {
        public Vector2[] posGrandes, tamGrandes, posPeques, tamPeques, puertas;
    }
    public class LevelDataRequest : NetworkMessage { }

    void Start()
    {
        actual = this;
        if (isServer)
        {
            NetworkServer.RegisterHandler<LevelDataRequest>(LevelDataRequestHandler, false);

            PrepararRonda();
        }
        else
        {
            NetworkClient.RegisterHandler<LevelGeneratorData>(LevelDataProcessor);
        }
    }
    void OnDestroy()
    {
        if (actual == this) actual = null;
    }

    [ContextMenu("Preparar Ronda")]
    void PrepararRonda()
    {
        StartCoroutine(PrepararRondaRutina());
    }
    IEnumerator PrepararRondaRutina()
    {
        yield return levelGenerator.GenerarLayout();
        levelGenerator.RenderNivel(true);
        // levelGenerator.ActualizarColliderYNavSurface();

        var agrupados = levelGenerator.generadorLayouts.generados.GroupBy(cuarto => cuarto.categoria);
        var grandes = agrupados.FirstOrDefault(grupo => grupo.Key == LayoutCuarto.Categoria.Grande);
        var peques = agrupados.FirstOrDefault(grupo => grupo.Key == LayoutCuarto.Categoria.Peque);
        var data = new LevelGeneratorData()
        {
            posGrandes = grandes.Select(cada => (Vector2)cada.BoxCol.transform.TransformPoint(cada.BoxCol.offset)).ToArray(),
            tamGrandes = grandes.Select(cada => (Vector2)cada.BoxCol.transform.TransformVector(cada.BoxCol.size)).ToArray(),
            posPeques = peques.Select(cada => (Vector2)cada.BoxCol.transform.TransformPoint(cada.BoxCol.offset)).ToArray(),
            tamPeques = peques.Select(cada => (Vector2)cada.BoxCol.transform.TransformVector(cada.BoxCol.size)).ToArray(),
            puertas = levelGenerator.generadorMapaArbol.vinculos.SelectMany(vinc => vinc.puertas).ToArray()
        };
        levelGenerator.generadorColliderGlobal.GenerarAMano(
            data.posGrandes, data.tamGrandes, data.posPeques, data.tamPeques, data.puertas
        );
        levelGenerator.ActualizarColliderYNavSurface();

        gridPos = levelGenerator.Bounds.min;
        var tam = levelGenerator.Bounds.size;
        (gridSize.x, gridSize.y) = (Mathf.CeilToInt(tam.x / gridStepSize), Mathf.CeilToInt(tam.z / gridStepSize));
        Debug.Log($"estimated bits needed - {Mathf.Ceil(Mathf.Log(gridSize.x * gridSize.y, 2))}");

        GenerarJugadoresIniciales();
        GenerarPatrullas();

        rondaIniciada = true;
    }

    [Server]
    void LevelDataRequestHandler(NetworkConnection jug, LevelDataRequest request)
    {
        Debug.Log("level data being requested");
        var agrupados = levelGenerator.generadorLayouts.generados.GroupBy(cuarto => cuarto.categoria);
        var grandes = agrupados.FirstOrDefault(grupo => grupo.Key == LayoutCuarto.Categoria.Grande);
        var peques = agrupados.FirstOrDefault(grupo => grupo.Key == LayoutCuarto.Categoria.Peque);
        var data = new LevelGeneratorData()
        {
            posGrandes = grandes.Select(cada => (Vector2)cada.BoxCol.transform.TransformPoint(cada.BoxCol.offset)).ToArray(),
            tamGrandes = grandes.Select(cada => (Vector2)cada.BoxCol.transform.TransformVector(cada.BoxCol.size)).ToArray(),
            posPeques = peques.Select(cada => (Vector2)cada.BoxCol.transform.TransformPoint(cada.BoxCol.offset)).ToArray(),
            tamPeques = peques.Select(cada => (Vector2)cada.BoxCol.transform.TransformVector(cada.BoxCol.size)).ToArray(),
            puertas = levelGenerator.generadorMapaArbol.vinculos.SelectMany(vinc => vinc.puertas).ToArray()
        };

        jug.Send(data);
        // NetworkServer.SendToClientOfPlayer(jug.identity, data);
    }

    void IniciarJugadorEnCombate()
    {
        StartCoroutine(IniciarJugadorEnCombateRutina());
    }
    IEnumerator IniciarJugadorEnCombateRutina()
    {
        yield return null;
    }

    void GenerarPatrullas()
    {
        var numPatrullas = Random.Range(cantPatrullas[0], cantPatrullas[1]);
        for (int i = 0; i < numPatrullas; i++)
        {
            var pos = LayoutEnMundo.PuntoRandomEnLayout();
            if (Vector3.Distance(posInicial, pos) < distMinimaGuardias)
            {
                i--;
                continue;
            }

            var guardias = Random.Range(cantGuardiasPorPatrulla[0], cantGuardiasPorPatrulla[1]);

            for (int g = 0; g < guardias; g++)
            {
                var guardia = Instantiate(prefabGuardia, pos, Quaternion.identity);
                NetworkServer.Spawn(guardia.gameObject);
            }
        }
    }

    void GenerarJugadoresIniciales()
    {
        posInicial = LayoutEnMundo.PuntoRandomEnLayout();
        foreach (var jug in JugadorMirror.jugadores)
        {
            var pj = Instantiate(prefabJugador, posInicial, Quaternion.identity);
            prefabJugador.gameObject.SetActive(true);
            prefabJugador.Agent.Warp(pj.transform.position);
            NetworkServer.Spawn(pj.gameObject, jug.gameObject);

            jugadoresIniciados.Add(jug, pj);

        }

        RpcIniciarParaJugadores();
    }

    [ClientRpc]
    void RpcIniciarParaJugadores()
    {
        StartCoroutine(GameUtils.EsperarTrueLuegoHacerCallback(
            () => WachinJugador.local,
            () =>
            {
                followCam.Follow = WachinJugador.local.transform;
                camInicial.enabled = false;
                if (!isServer)
                {
                    Debug.Log("requesting level data");
                    NetworkClient.Send(new LevelDataRequest());
                }
            }
        ));
    }

    [Client]
    void LevelDataProcessor(LevelGeneratorData data)
    {
        Debug.Log("received data from level");
        levelGenerator.generadorColliderGlobal.GenerarAMano(
            data.posGrandes, data.tamGrandes, data.posPeques, data.tamPeques, data.puertas
        );
        //no es necesario crear los colliders en el cliente // levelGenerator.ActualizarColliderYNavSurface();
    }

    public Vector3 GetIndexedPosition(ulong posIndex) {
        return gridPos + new Vector3(gridStepSize*(posIndex%GridSizeX), 0f, gridStepSize*(posIndex/GridSizeX));
    }
    public ulong GetPositionIndex(Vector3 pos) {
        return (ulong) (Mathf.RoundToInt((pos.x-gridPos.x)/gridStepSize)
            + Mathf.RoundToInt((pos.z-gridPos.z)/gridStepSize)*gridSize.x);
    }

}
