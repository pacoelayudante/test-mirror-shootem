using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class Atacable : MonoBehaviour
{
    public int equipo = -1;
    public static List<Atacable> atacables = new List<Atacable>();
    
    public Vector3 Pos => transform.position;

    public float dañoAcumulado = 0f;
    public event System.Action<float> AlRecibirAtaque;
    public void RecibirAtaque(float daño) {
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
