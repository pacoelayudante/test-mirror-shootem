using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class Atacable : MonoBehaviour
{
    public Vector3 Pos => transform.position;

    public float dañoAcumulado = 0f;
    public event System.Action<float> AlRecibirAtaque;
    public void RecibirAtaque(float daño) {
        dañoAcumulado += daño;
        AlRecibirAtaque?.Invoke(daño);
    }
}
