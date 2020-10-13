using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PointDefense : MonoBehaviour
{
    [SerializeField] Transform[] posiciones = null;
    [SerializeField] BaseDisparo baseDisparo = null;
    [SerializeField] float cooldown = .2f;
    [SerializeField] float magnitudError = 4f;

    float siguienteDisparo = 0f;

    public void Disparar(Vector2 dir) {
        Disparar(Quaternion.Euler(0,0,Vector2.SignedAngle(Vector2.right,dir)));
    }
    public void Disparar(Quaternion rotacion)
    {
        if (posiciones == null || !baseDisparo) return;
        if (Time.time < siguienteDisparo) return;
        foreach (var pos in posiciones)
        {
            var disparo = Instantiate(baseDisparo, pos.position, rotacion * Quaternion.Euler(0f,0f,Random.Range(-magnitudError,magnitudError)));
        }
        siguienteDisparo = Time.time + cooldown;
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white/2f;
        foreach(var posDisparo in FindObjectsOfType<PosibleObjetivo>()) posDisparo.Gizmo();
    }
}
