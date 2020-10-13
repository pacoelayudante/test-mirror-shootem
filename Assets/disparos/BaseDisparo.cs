using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class BaseDisparo : MonoBehaviour
{
    Collider2D _collider;
    Collider2D Collider => _collider?_collider:_collider = GetComponent<Collider2D>();

    [SerializeField] float daño = 1f;
    [SerializeField] Vector3 velocidadLocal = Vector3.right*30f;
    [SerializeField] float duracion = 1f;
    float tiempoDestruccion;

    [SerializeField] ContactFilter2D _contactFilter;
    RaycastHit2D[] hits = new RaycastHit2D[1];

    [SerializeField] SimpleFXs fxOnHit = null;

    void Start() {
        tiempoDestruccion = Time.time+duracion;
    }

    void Update() {
        var dt = Time.inFixedTimeStep?Time.fixedDeltaTime:Time.deltaTime;
        var recorrido = velocidadLocal*dt;

        if (Collider.Cast( transform.TransformVector(recorrido.normalized), _contactFilter, hits, recorrido.magnitude) > 0) {
            recorrido = recorrido.normalized * hits[0].distance;
            var atacado = hits[0].collider.GetComponent<Atacable>();            
            if (atacado) {
                tiempoDestruccion = 0f;
                Instantiate( fxOnHit, hits[0].point, Quaternion.identity);
                atacado.RecibirAtaque(daño);
            }
        }

        transform.Translate(velocidadLocal*dt);

        if (Time.time > tiempoDestruccion) {
            Destroy(gameObject);
        }
    }
}
