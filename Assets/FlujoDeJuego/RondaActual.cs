using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Cinemachine;
using UnityEngine.AI;

public class RondaActual : NetworkBehaviour
{
    [SerializeField] CinemachineVirtualCamera camInicial;
    [SerializeField] NavMeshSurface surface;
    [SerializeField] MeshCollider meshCol;
    [SerializeField] LevelGenerator levelGenerator;

    List<JugadorMirror> jugadoresIniciados = new List<JugadorMirror>();
    List<JugadorMirror> jugadoresDerrotados = new List<JugadorMirror>();

    void PrepararRonda() {

    }
    void PrepararRondaRutina() {

    }

    void IniciarJugadorEnCombate() {
        StartCoroutine(IniciarJugadorEnCombateRutina());
    }
    IEnumerator IniciarJugadorEnCombateRutina() {
        yield return null;
    }
}
