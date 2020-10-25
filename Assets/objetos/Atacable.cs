using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

[DisallowMultipleComponent]
public class Atacable : NetworkBehaviour
{
    public int equipo = -1;
    public static List<Atacable> atacables = new List<Atacable>();
    
    public Vector3 Pos => transform.position;

    [SyncVar]
    public float dañoAcumulado = 0f;
    public event System.Action<float> AlRecibirAtaque;

    [Server]
    public void RecibirAtaque(float daño) {
        if (hasAuthority) CmdRecibirAtaque(daño);
    }
    [Command]
    void CmdRecibirAtaque(float daño) {
        dañoAcumulado += daño;
        AlRecibirAtaque?.Invoke(daño);
    }

    private void OnEnable() {
        atacables.Add(this);
    }
    private void OnDisable() {
        atacables.Remove(this);
    }
}
