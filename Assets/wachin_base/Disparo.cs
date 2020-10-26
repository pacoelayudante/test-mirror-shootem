using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Disparo : NetworkBehaviour
{
    public event System.Action<Collision> onCollisionEnter;
    public event System.Action<Collider> onTriggerEnter;

    Rigidbody _rigid;
    Rigidbody Rigid => _rigid?_rigid:_rigid=GetComponent<Rigidbody>();

    Vector3 _vel;
    public Vector3 Velocidad {
        get => _vel;
        set => _vel = Rigid?Rigid.velocity=value:value;
    }

    [ServerCallback]
    private void OnCollisionEnter(Collision other) {
        onCollisionEnter?.Invoke(other);
    }
    [ServerCallback]
    private void OnTriggerEnter(Collider other) {
        onTriggerEnter?.Invoke(other);        
    }
}
