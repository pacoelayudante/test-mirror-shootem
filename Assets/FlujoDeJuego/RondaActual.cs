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

    // [SerializeField, SyncVar] 
    Vector3 gridPos;
    // [SerializeField, SyncVar] 
    uint gridSizeX, gridSizeY;

    [SerializeField, SyncVar]
    bool rondaIniciada = false;
    Dictionary<JugadorMirror, WachinJugador> jugadoresIniciados = new Dictionary<JugadorMirror, WachinJugador>();
    List<JugadorMirror> jugadoresDerrotados = new List<JugadorMirror>();

    public float gridStepSize = .15f;
    public WachinJugador prefabJugador;
    public WachinLogica prefabGuardia;
    public Vector2Int cantGuardiasPorPatrulla = new Vector2Int(3, 6);
    public Vector2Int cantPatrullas = new Vector2Int(9, 15);
    public float distMinimaGuardias = 30f;
    [Guazu.DrawersCopados.CreameScriptable]
    public BibliotecaBrutaDeMods modsList;

    Vector3[] posList;

    Vector3 posInicial;

    public class LevelGeneratorData : NetworkMessage
    {
    public Vector3 gridPos;
    public uint gridSizeX, gridSizeY;
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
            NetworkClient.Send(new LevelDataRequest());
        }

        StartCoroutine(GameUtils.EsperarTrueLuegoHacerCallback(
            () => WachinJugador.local,
            () =>
            {
                followCam.Follow = WachinJugador.local.transform;
                camInicial.enabled = false;
            }));
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
        // (gridSize.x, gridSize.y) = (Mathf.CeilToInt(tam.x / gridStepSize), Mathf.CeilToInt(tam.z / gridStepSize));
        gridSizeX = (uint)Mathf.CeilToInt(tam.x / gridStepSize);
        gridSizeY = (uint)Mathf.CeilToInt(tam.z / gridStepSize);
        posList = new Vector3[gridSizeX * gridSizeY];
        for (int i = 0; i < posList.Length; i++)
        {
            var x = i % gridSizeX;
            var y = i / gridSizeX;
            posList[i] = gridPos + new Vector3(gridStepSize * x, 0f, gridStepSize * y);
        }
        Debug.Log($"estimated bits needed - {Mathf.Ceil(Mathf.Log(gridSizeX * gridSizeY, 2))}");

        GenerarJugadoresIniciales();
        GenerarPatrullas();

        rondaIniciada = true;

        StartCoroutine(GeneradorDeJugadores());
    }

    IEnumerator GeneradorDeJugadores()
    {
        while (gameObject)
        {
            foreach (var jug in JugadorMirror.jugadores.Except(jugadoresIniciados.Keys))
            {
                if (jug.preparade)
                {
                    IniciarJugadorEnCombate(jug);
                }

            }
            yield return null;
        }
    }

    [Server]
    void IniciarJugadorEnCombate(JugadorMirror jug)
    {
        var pj = Instantiate(prefabJugador, posInicial, Quaternion.identity);
        pj.gorroIndex = jug.gorritoDeseado % pj.posiblesGorros.Length;

        if (modsList)
        {
            if (jug.modUno) modsList.ModFijoArmaduraLenta(pj.Wachin);
            if (jug.modDos) modsList.ModFijoEscopeta(pj.Wachin);
            if (jug.modTres) modsList.ModFijoPrecisionPeligrosa(pj.Wachin);
        }
        // prefabJugador.gameObject.SetActive(true);
        pj.Wachin.Agent.Warp(pj.transform.position);
        NetworkServer.Spawn(pj.gameObject, jug.gameObject);

        jugadoresIniciados.Add(jug, pj);
    }

    [Server]
    void LevelDataRequestHandler(NetworkConnection jug, LevelDataRequest request)
    {
        Debug.Log("level data being requested");
        StartCoroutine(GameUtils.EsperarTrueLuegoHacerCallback(
            () => rondaIniciada,
            () =>
            {
                Debug.Log("level data ready, sending");
                var agrupados = levelGenerator.generadorLayouts.generados.GroupBy(cuarto => cuarto.categoria);
                var grandes = agrupados.FirstOrDefault(grupo => grupo.Key == LayoutCuarto.Categoria.Grande);
                var peques = agrupados.FirstOrDefault(grupo => grupo.Key == LayoutCuarto.Categoria.Peque);
                var data = new LevelGeneratorData()
                {
                    gridPos= gridPos,
                    gridSizeX = gridSizeX,
                    gridSizeY = gridSizeY,
                    posGrandes = grandes.Select(cada => (Vector2)cada.BoxCol.transform.TransformPoint(cada.BoxCol.offset)).ToArray(),
                    tamGrandes = grandes.Select(cada => (Vector2)cada.BoxCol.transform.TransformVector(cada.BoxCol.size)).ToArray(),
                    posPeques = peques.Select(cada => (Vector2)cada.BoxCol.transform.TransformPoint(cada.BoxCol.offset)).ToArray(),
                    tamPeques = peques.Select(cada => (Vector2)cada.BoxCol.transform.TransformVector(cada.BoxCol.size)).ToArray(),
                    puertas = levelGenerator.generadorMapaArbol.vinculos.SelectMany(vinc => vinc.puertas).ToArray()
                };

                jug.Send(data);
            }));
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

    [ClientRpc]
    void RpcPosicionarCamaraPreviamente()
    {
        // if (followCam) followCam.
    }

    void GenerarJugadoresIniciales()
    {
        posInicial = LayoutEnMundo.PuntoRandomEnLayout();

        // RpcIniciarParaJugadores();
    }

    [ClientRpc, System.Obsolete]
    void RpcIniciarParaJugadores()
    {
        followCam.Follow = WachinJugador.local.transform;
        camInicial.enabled = false;
        // StartCoroutine(GameUtils.EsperarTrueLuegoHacerCallback(
        //     () => WachinJugador.local,
        //     () =>
        //     {
        //         followCam.Follow = WachinJugador.local.transform;
        //         camInicial.enabled = false;
        //         if (!isServer)
        //         {
        //             Debug.Log("requesting level data");
        //             NetworkClient.Send(new LevelDataRequest());
        //         }
        //     }
        // ));
    }

    [Client]
    void LevelDataProcessor(LevelGeneratorData data)
    {
        gridPos= data.gridPos;
        gridSizeX = data.gridSizeX;
        gridSizeY = data.gridSizeY;

        Debug.Log("received data from level");
        levelGenerator.generadorColliderGlobal.GenerarAMano(
            data.posGrandes, data.tamGrandes, data.posPeques, data.tamPeques, data.puertas
        );

        Debug.Log($"{gridSizeX}-{gridSizeY}");
        posList = new Vector3[gridSizeX * gridSizeY];
        for (int i = 0; i < posList.Length; i++)
        {
            var x = i % gridSizeX;
            var y = i / gridSizeX;
            posList[i] = gridPos + new Vector3(gridStepSize * x, 0f, gridStepSize * y);
        }
        //no es necesario crear los colliders en el cliente // levelGenerator.ActualizarColliderYNavSurface();
    }

    public Vector3 GetIndexedPosition(ulong posIndex)
    {
        if (posList == null || posList.Length == 0) return Vector3.zero;
        // return gridPos + new Vector3(gridStepSize*(posIndex%gridSizeX), 0f, gridStepSize*(posIndex/gridSizeX));
        return posList[posIndex];
    }
    public ulong GetPositionIndex(Vector3 pos)
    {
        if (gridSizeX == 0 || gridSizeY == 0) return 0;
        return (ulong)(Mathf.RoundToInt((pos.x - gridPos.x) / gridStepSize)
            + gridSizeX * Mathf.RoundToInt((pos.z - gridPos.z) / gridStepSize));
    }

}
