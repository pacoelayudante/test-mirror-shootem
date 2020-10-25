using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class JugadorMirror : NetworkBehaviour
{
    public static List<JugadorMirror> jugadores = new List<JugadorMirror>();

    [SyncVar]
    public string nombreVisible = "Innombrado";

    void Awake() {
        DontDestroyOnLoad(gameObject);
        jugadores.Add(this);
    }
    void OnDestroy() {
        jugadores.Remove(this);
    }
}
