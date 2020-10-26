using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.AI;
using Mirror;

public class WachinEnemigo : NetworkBehaviour
{
    static List<WachinEnemigo> wachinEnemigos = new List<WachinEnemigo>();
    public static int Count => wachinEnemigos.Count;

    Atacable _objetivo;
    public LayerMask buscadoEnPatrulla;
    public LayerMask visionBlocker;
    public float maxViewDist = 10f;
    public float distanciaPrudente = 4f;
    public Vector2 cambiarPosicionCada = new Vector2(4f, 9f);
    public Vector2 dispararCada = new Vector2(0f, 2f);

    public float fullHp = 3;
    public SimpleFXs muereFx;
    [Guazu.DrawersCopados.CreameScriptable]
    [SerializeField] LiderTacticStats liderazgo;
    WachinLogica _wachin;
    WachinLogica Wachin => _wachin ? _wachin : _wachin = GetComponent<WachinLogica>();
    Atacable _atacable;
    public Atacable Atacable => _atacable ? _atacable : _atacable = GetComponent<Atacable>();

    Coroutine rutina;

    RaycastHit hit;
    Patrulla _patrulla;
    Patrulla Patrulla
    {
        get
        {
            if (_patrulla == null)
            {
                if (liderazgo)
                {
                    _patrulla = Patrulla.PatrullaEnRango(transform.position, liderazgo.rangoAutoAsociarPatrulla);
                }
                if (_patrulla == null) _patrulla = new Patrulla();
                _patrulla.Add(this);
            }
            return _patrulla;
        }
        set
        {
            if (_patrulla != null) _patrulla.Remove(this);
            _patrulla = value;
            if (_patrulla != null) _patrulla.Add(this);
        }
    }

    void Awake()
    {
        wachinEnemigos.Add(this);
    }
    void OnDestroy()
    {
        wachinEnemigos.Remove(this);
        if (_patrulla != null) _patrulla.Remove(this);
    }

    [ServerCallback]
    void Start()
    {
        if (Atacable) Atacable.AlRecibirAtaque += RecibirAtaque;
    }
    void OnEnable() {
        rutina = StartCoroutine(Rutina());
    }
    void OnDisable() {
        StopCoroutine(rutina);
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
                NetworkServer.Spawn(fx.gameObject);
            }
        }
    }

    IEnumerator Rutina()
    {
        while (this)
        {
            if (_objetivo) yield return StartCoroutine(Combatir());
            else yield return StartCoroutine(Patrullar());

            yield return null;
        }
    }

    IEnumerator Combatir()
    {
        Wachin.Rifle = true;
        var offsetDePosDeAtaque = Quaternion.Euler(0f, Random.value * 360f, 0f) * Vector3.right * distanciaPrudente;
        var cambiarPosicionCuando = Time.time + Random.Range(cambiarPosicionCada[0], cambiarPosicionCada[1]);
        var dispararCuando = Time.time + Random.Range(dispararCada[0], dispararCada[1]);
        while (_objetivo)
        {
            if (Time.time >= cambiarPosicionCuando)
            {
                cambiarPosicionCuando = Time.time + Random.Range(cambiarPosicionCada[0], cambiarPosicionCada[1]);
                offsetDePosDeAtaque = Quaternion.Euler(0f, Random.value * 360f, 0f) * offsetDePosDeAtaque;
            }
            Wachin.MiraHacia = _objetivo.transform.position;

            var offUp = Wachin.Agent.height * Vector3.up * 0.5f;
            var attPos = _objetivo.transform.position + offUp;
            var posOfInterest = attPos + offsetDePosDeAtaque;//move to
            if (Physics.Linecast(attPos, posOfInterest, out hit, visionBlocker))
            {
                Wachin.PosBuscada = hit.point;
            }
            else
            {
                Wachin.PosBuscada = posOfInterest;//_objetivo.transform.position+(transform.position-_objetivo.transform.position).normalized*distanciaPrudente;
            }

            if (Time.time >= dispararCuando)
            {
                dispararCuando = Time.time + Random.Range(dispararCada[0], dispararCada[1]);
                posOfInterest = transform.position + offUp;//shoot from
                if (!Physics.Linecast(attPos, posOfInterest, out hit, visionBlocker))
                {
                    // Wachin.ItemActivo.Activar();
                }
            }


            yield return null;
        }
    }

    IEnumerator Patrullar()
    {
        Wachin.Rifle = false;

        var offsetDePatrulla = Quaternion.Euler(0f, Random.value * 360f, 0f) * Vector3.right;
        var distOffsetDePatrulla = Random.value*(liderazgo?liderazgo.rangoAutoAsociarPatrulla:1f);
        var cambiarPosicionCuando = Time.time + Random.Range(cambiarPosicionCada[0], cambiarPosicionCada[1]);

        while (!_objetivo)
        {
            if (liderazgo)
            {
                if (Patrulla.Lider == this)
                {
                    liderazgo.LiderarPatrulla(Patrulla);
                    Wachin.PosBuscada = Patrulla.destinoPatrulla;
                }
                else
                {
                    var newPos = Patrulla.PosLider + offsetDePatrulla*distOffsetDePatrulla;
                    var hit = new NavMeshHit();
                    if (NavMesh.Raycast(Patrulla.PosLider, newPos, out hit, NavMesh.AllAreas)) {
                        Wachin.PosBuscada = hit.position;
                    }
                    else Wachin.PosBuscada = newPos;
                }
                Wachin.MiraHacia = Wachin.PosBuscada;
            }

            if (Time.time >= cambiarPosicionCuando)
            {
                cambiarPosicionCuando = Time.time + Random.Range(cambiarPosicionCada[0], cambiarPosicionCada[1]);
                offsetDePatrulla = Quaternion.Euler(0f, Random.value * 360f, 0f) * offsetDePatrulla;
                distOffsetDePatrulla = Random.value*liderazgo.rangoAutoAsociarPatrulla;
            }

            var offUp = Wachin.Agent.height * Vector3.up * 0.5f;
            var rayOrigin = transform.position + offUp;
            var posObjetivos = Atacable.atacables.Where(at => buscadoEnPatrulla == (buscadoEnPatrulla | (1 << at.gameObject.layer)))
                .Where(at => Vector3.Distance(transform.position, at.transform.position) < maxViewDist);
            foreach (var _posObjetivo in posObjetivos)
            {

                var attPos = _posObjetivo.transform.position + offUp;
                if (!Physics.Raycast(rayOrigin, attPos - rayOrigin, out hit, Vector3.Distance(rayOrigin, attPos), visionBlocker))
                {
                    _objetivo = _posObjetivo;
                    yield break;
                    //alerta!
                }

            }

            yield return null;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (_objetivo)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, Vector3.MoveTowards(_objetivo.transform.position, transform.position, distanciaPrudente));

            Gizmos.DrawWireSphere(hit.point, .1f);
        }

        if (_patrulla != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, Patrulla.destinoPatrulla);
            Gizmos.DrawLine(transform.position, Patrulla.PosPseudoMedian);
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, Patrulla.PosPromedio);
        }
    }
}
