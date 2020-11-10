using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.AI;
using Mirror;

public class WachinEnemigo : NetworkBehaviour
{
    public static List<WachinEnemigo> todes = new List<WachinEnemigo>();
    public static int Count => todes.Count;

    Atacable _objetivo;
    public LayerMask buscadoEnPatrulla;
    public LayerMask visionBlocker;
    public float maxViewDist = 10f;
    public Vector2 distanciaPrudente = new Vector2(12f, 18f);
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
        todes.Add(this);
    }
    void OnDestroy()
    {
        todes.Remove(this);
        if (_patrulla != null) _patrulla.Remove(this);
    }

    [ServerCallback]
    void Start()
    {
        if (Atacable) Atacable.AlRecibirAtaque += RecibirAtaque;
    }
    [ServerCallback]
    void OnEnable()
    {
        rutina = StartCoroutine(Rutina());
    }
    [ServerCallback]
    void OnDisable()
    {
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
        var distanciaPrudenteActual = Random.Range(distanciaPrudente[0], distanciaPrudente[1]);
        var offsetDePosDeAtaque = Quaternion.Euler(0f, Random.value * 360f, 0f) * Vector3.right * distanciaPrudenteActual;
        var cambiarPosicionCuando = Time.time + Random.Range(cambiarPosicionCada[0], cambiarPosicionCada[1]);
        var dispararCuando = Time.time + Random.Range(dispararCada[0], dispararCada[1]);

        var wachinObjetivo = _objetivo.GetComponent<WachinLogica>();

        while (_objetivo)
        {
            if (Time.time >= cambiarPosicionCuando)
            {
                cambiarPosicionCuando = Time.time + Random.Range(cambiarPosicionCada[0], cambiarPosicionCada[1]);

                distanciaPrudenteActual = Random.Range(distanciaPrudente[0], distanciaPrudente[1]);
                offsetDePosDeAtaque = offsetDePosDeAtaque.normalized * distanciaPrudenteActual;

                offsetDePosDeAtaque = Quaternion.Euler(0f, Random.value * 360f, 0f) * offsetDePosDeAtaque;

                var nuevoObjetivo = VigilarPorNuevoObjetivo();
                if (nuevoObjetivo)
                {
                    Patrulla.NuevoObjetivo(nuevoObjetivo);
                }
                // var posibleNuevoObjetivo = Patrulla.TomarObjetivoRandom();
                var posiblesNuevosObjetivos = Patrulla.objetivosRegistrados.Select(at => at.GetComponent<WachinLogica>())
                    .Where(wa => wa && !wa.Noqueade).ToArray();
                if (posiblesNuevosObjetivos.Length > 0){
                    var posibleNuevoObjetivo = posiblesNuevosObjetivos[Random.Range(0, posiblesNuevosObjetivos.Length)];
                    if (posibleNuevoObjetivo)
                    {
                        _objetivo = posibleNuevoObjetivo.GetComponent<Atacable>();
                        wachinObjetivo = posibleNuevoObjetivo;
                    }
                }
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

            if (Time.time >= dispararCuando && wachinObjetivo && !wachinObjetivo.Noqueade)
            {
                dispararCuando = Time.time + Random.Range(dispararCada[0], dispararCada[1]);
                posOfInterest = transform.position + offUp;//shoot from
                if (!Physics.Linecast(attPos, posOfInterest, out hit, visionBlocker))
                {
                    Wachin.ItemActivo.Activar();
                }
            }


            yield return null;
        }
    }

    IEnumerator Patrullar()
    {
        Wachin.Rifle = false;

        var offsetDePatrulla = Quaternion.Euler(0f, Random.value * 360f, 0f) * Vector3.right;
        var distOffsetDePatrulla = Random.value * (liderazgo ? liderazgo.rangoAutoAsociarPatrulla : 1f);
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
                    var newPos = Patrulla.PosLider + offsetDePatrulla * distOffsetDePatrulla;
                    var hit = new NavMeshHit();
                    if (NavMesh.Raycast(Patrulla.PosLider, newPos, out hit, NavMesh.AllAreas))
                    {
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
                distOffsetDePatrulla = Random.value * liderazgo.rangoAutoAsociarPatrulla;
            }

            var objetivoDePatrulla = Patrulla.TomarObjetivoRandom();
            if (objetivoDePatrulla)
            {
                _objetivo = objetivoDePatrulla;
            }
            else
            {
                var nuevoObjetivo = VigilarPorNuevoObjetivo();
                if (nuevoObjetivo)
                {
                    Patrulla.NuevoObjetivo(_objetivo = nuevoObjetivo);
                    yield break;
                }
            }

            yield return null;
        }
    }

    Atacable VigilarPorNuevoObjetivo()
    {
        var offUp = Wachin.Agent.height * Vector3.up * 0.5f;
        var rayOrigin = transform.position + offUp;
        var posObjetivos = Atacable.atacables.Where(a => !Patrulla.ObjetivoRegistrado(a))
            .Where(at => buscadoEnPatrulla == (buscadoEnPatrulla | (1 << at.gameObject.layer)))
            .Where(at => Vector3.Distance(transform.position, at.transform.position) < maxViewDist);
        foreach (var _posObjetivo in posObjetivos)
        {

            var attPos = _posObjetivo.transform.position + offUp;
            if (!Physics.Raycast(rayOrigin, attPos - rayOrigin, out hit, Vector3.Distance(rayOrigin, attPos), visionBlocker))
            {
                return _posObjetivo;
            }

        }
        return null;
    }

    void OnDrawGizmosSelected()
    {
        if (_objetivo)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, Vector3.MoveTowards(_objetivo.transform.position, transform.position, distanciaPrudente[0]));

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
