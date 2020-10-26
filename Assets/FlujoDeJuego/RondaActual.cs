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

    [SerializeField, SyncVar]
    bool rondaIniciada = false;
    Dictionary<JugadorMirror, WachinLogica> jugadoresIniciados = new Dictionary<JugadorMirror, WachinLogica>();
    List<JugadorMirror> jugadoresDerrotados = new List<JugadorMirror>();

    public WachinLogica prefabJugador;
    public WachinLogica prefabGuardia;
    public Vector2Int cantGuardiasPorPatrulla = new Vector2Int(3, 6);
    public Vector2Int cantPatrullas = new Vector2Int(9, 15);

    Vector3 posInicial;

    public class LevelGeneratorData : NetworkMessage {
        public Vector2[] posGrandes,tamGrandes,posPeques,tamPeques,puertas;
    }
    public class LevelDataRequest : NetworkMessage {}

    void Start()
    {
        actual = this;
        if (isServer)
        {
            NetworkServer.RegisterHandler<LevelDataRequest>(LevelDataRequestHandler,false);

            PrepararRonda();
        }
        else
        {
            NetworkServer.RegisterHandler<LevelGeneratorData>(LevelDataProcessor);
        }
    }
    void OnDestroy() {
        if (actual == this) actual = null;
    }

    void PrepararRonda()
    {
        StartCoroutine(PrepararRondaRutina());
    }
    IEnumerator PrepararRondaRutina()
    {
        yield return levelGenerator.GenerarLayout();
        levelGenerator.RenderNivel(true);
        levelGenerator.ActualizarColliderYNavSurface();

        GenerarPatrullas();
        GenerarJugadoresIniciales();

        rondaIniciada = true;
    }

    void LevelDataRequestHandler(NetworkConnection jug, LevelDataRequest request) {
        var agrupados = levelGenerator.generadorLayouts.generados.GroupBy(cuarto=>cuarto.categoria);
        var grandes = agrupados.FirstOrDefault(grupo=>grupo.Key==LayoutCuarto.Categoria.Grande);
        var peques = agrupados.FirstOrDefault(grupo=>grupo.Key==LayoutCuarto.Categoria.Peque);
        var data = new LevelGeneratorData(){
            posGrandes = grandes.Select(cada=>(Vector2)cada.BoxCol.transform.TransformPoint(cada.BoxCol.offset)).ToArray(),
            tamGrandes = grandes.Select(cada=>(Vector2)cada.BoxCol.transform.TransformVector(cada.BoxCol.size)).ToArray(),
            posPeques = peques.Select(cada=>(Vector2)cada.BoxCol.transform.TransformPoint(cada.BoxCol.offset)).ToArray(),
            tamPeques = peques.Select(cada=>(Vector2)cada.BoxCol.transform.TransformVector(cada.BoxCol.size)).ToArray(),
            puertas = levelGenerator.generadorMapaArbol.vinculos.SelectMany(vinc=>vinc.puertas).ToArray()
        };

        NetworkServer.SendToClientOfPlayer(jug.identity, data);
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
            var guardias = Random.Range(cantGuardiasPorPatrulla[0], cantGuardiasPorPatrulla[1]);
            var pos = LayoutEnMundo.PuntoRandomEnLayout();
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

            RpcIniciarParaJugadores();
        }
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
                Debug.Log("requesting level data");
                NetworkClient.Send(new LevelDataRequest());
            }
        ));
    }

    void LevelDataProcessor(LevelGeneratorData data) {
        Debug.Log("received data from level");
        levelGenerator.generadorColliderGlobal.GenerarAMano(
            data.posGrandes, data.tamGrandes, data.posPeques, data.tamPeques, data.puertas
        );
        levelGenerator.ActualizarColliderYNavSurface();
    }
}
