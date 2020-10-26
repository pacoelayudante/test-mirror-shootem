using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Cinemachine;
using UnityEngine.AI;

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

    void Start()
    {
        actual = this;
        if (isServer)
        {
            PrepararRonda();
        }
        else
        {
            camInicial.enabled = false;
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
            }
        ));
    }
}
