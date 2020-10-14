using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class WachinEnemigo : MonoBehaviour
{
    Atacable _objetivo;
    public LayerMask buscadoEnPatrulla;
    public LayerMask visionBlocker;
    public float maxViewDist = 10f;
    public float distanciaPrudente = 4f;

    public float fullHp = 3;
    public SimpleFXs muereFx;
    WachinLogica _wachin;
    WachinLogica Wachin => _wachin ? _wachin : _wachin = GetComponent<WachinLogica>();
    Atacable _atacable;
    public Atacable Atacable => _atacable ? _atacable : _atacable = GetComponent<Atacable>();

    RaycastHit hit;

    void Start()
    {
        if (Atacable) Atacable.AlRecibirAtaque += RecibirAtaque;
        StartCoroutine( Rutina () );
    }

    void RecibirAtaque(float dmg)
    {
        if (Atacable.dañoAcumulado >= fullHp)
        {
            Destroy(gameObject);
            if (muereFx)
            {
                var fx = Instantiate(muereFx, transform.position + Vector3.up * Wachin.Agent.height / 2f, Quaternion.identity);
                fx.transform.LookAt(Camera.main.transform, Vector3.up);
            }
        }
    }

    IEnumerator Rutina()
    {
        while (this)
        {
            if (_objetivo) yield return StartCoroutine(Combate());
            else yield return StartCoroutine(Patrulla());

            yield return null;
        }
    }

    IEnumerator Combate() {
        Wachin.Rifle = true;
        while (_objetivo)
        {
            Wachin.miraHacia = _objetivo.transform.position;

            var offUp = Wachin.Agent.height * Vector3.up * 0.5f;
            var rayOrigin = transform.position + offUp;
            var attPos = _objetivo.transform.position + offUp;
            if (Physics.Linecast(attPos, rayOrigin, out hit, visionBlocker) && hit.distance <= distanciaPrudente)
            {
                Wachin.PosBuscada = hit.point;
            }
            else {
                Wachin.PosBuscada = _objetivo.transform.position+(transform.position-_objetivo.transform.position).normalized*distanciaPrudente;
                if (!hit.collider) Wachin.ItemActivo.Activar();
            }


            yield return null;
        }
    }

    IEnumerator Patrulla()
    {
        Wachin.Rifle = false;
        while (!_objetivo)
        {
            var offUp = Wachin.Agent.height * Vector3.up * 0.5f;
            var rayOrigin = transform.position + offUp;
            var posObjetivos = Atacable.atacables.Where(at => buscadoEnPatrulla == (buscadoEnPatrulla | (1 << at.gameObject.layer)))
                .Where(at => Vector3.Distance(transform.position, at.transform.position) < maxViewDist);
            foreach (var _posObjetivo in posObjetivos)
            {

                var attPos = _posObjetivo.transform.position + offUp;
                if (!Physics.Raycast(rayOrigin, attPos-rayOrigin, out hit, Vector3.Distance(rayOrigin, attPos), visionBlocker))
                {
                    _objetivo = _posObjetivo;
                    yield break;
                    //alerta!
                }

            }

            yield return null;
        }
    }

    void OnDrawGizmosSelected() {
        if (_objetivo) {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, Vector3.MoveTowards(_objetivo.transform.position, transform.position, distanciaPrudente));

            Gizmos.DrawWireSphere(hit.point,.1f);
        }
    }
}
