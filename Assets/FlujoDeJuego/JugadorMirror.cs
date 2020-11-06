using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class JugadorMirror : NetworkBehaviour
{
    public static List<JugadorMirror> jugadores = new List<JugadorMirror>();
    public static JugadorMirror local;

    [SyncVar]
    public string nombreVisible = "Innombrado";
    [SyncVar]
    public bool preparade = false;

    [SyncVar] public int gorritoDeseado = 0;

    [SyncVar] public bool modUno;
    [SyncVar] public bool modDos;
    [SyncVar] public bool modTres;

    void Awake() {
        DontDestroyOnLoad(gameObject);
        jugadores.Add(this);
    }
    void OnDestroy() {
        jugadores.Remove(this);
    }

    void Start(){
        if(isServer) gorritoDeseado = Random.Range(0,35);
        if (hasAuthority) local = this;
    }

    [Command]
    public void CmdCambiarNombre(string nuevo) {
        nombreVisible = string.IsNullOrEmpty(nuevo)?"Innombrado":nuevo;
    }
    [Command]
    public void CmdCambiarGorrito(int nuevo) {
        gorritoDeseado = nuevo;
    }
    [Command]
    public void CmdTogglePreparade() {
        preparade = !preparade;
    }

    [Command]
    public void CmdToggleMod(int index) {
        if (index==0) modUno = !modUno;
        else if (index==1) modDos = !modDos;
        else if (index==2) modTres = !modTres;
    }

    public bool EstaModActivo(int index) {
        if (index==0) return modUno;
        else if (index==1) return modDos;
        else if (index==2) return modTres;
        return false;
    }

}
