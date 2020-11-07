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
    public event System.Action AlRecibirAtaqueClient;

    [Server]
    public void RecibirAtaque(float daño)
    {
        dañoAcumulado += daño;
        AlRecibirAtaque?.Invoke(daño);
        RpcRecibirAtaque();
    }
    // [Server]
    // public void RecibirAtaque(float daño) {
    //     if (hasAuthority) CmdRecibirAtaque(daño);
    // }
    [ClientRpc]
    void RpcRecibirAtaque() {
        AlRecibirAtaqueClient?.Invoke();
    }

    private void OnEnable()
    {
        atacables.Add(this);
    }
    private void OnDisable()
    {
        atacables.Remove(this);
    }
}
