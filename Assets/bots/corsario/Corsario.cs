using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Guazu.DrawersCopados;

[DisallowMultipleComponent]
public class Corsario : MonoBehaviour
{
    static List<Corsario> corsarios = new List<Corsario>();

    PointDefense _pointDefense;
    PointDefense PointDefense => _pointDefense ? _pointDefense : _pointDefense = GetComponent<PointDefense>();
    Rigidbody2D _rigid;
    Rigidbody2D Rigid => _rigid ? _rigid : _rigid = GetComponent<Rigidbody2D>();

    [SerializeField]
    Corsario corsarioStats = null;
    Corsario Stats => corsarioStats ? corsarioStats : this;

    ColorControl _colorControl;
    ColorControl ColorControl => _colorControl?_colorControl:_colorControl = GetComponent<ColorControl>();

    Animator _animator;
    public Animator Animator => _animator ? _animator : _animator = GetComponent<Animator>();
    [AnimatorStringList(AnimatorStringListAttribute.Tipo.Parametros)]
    [SerializeField] string animApunta = "";

    [SerializeField] float velActiva = 25;
    float VelActiva => Stats.velActiva;
    [SerializeField] float velPasiva = 10;
    float VelPasiva => Stats.velPasiva;

    [SerializeField] float acelPasiva = 4f;
    float AcelPasiva => Stats.acelPasiva;
    // [SerializeField] float rotVelPasiva = 180f, rotVelActiva = 90f;
    // float RotVelPasiva => Stats.rotVelPasiva;
    // float RotVelActiva => Stats.rotVelActiva;

    [SerializeField] float maxHP = 15;

    [SerializeField] float maxDistanceToInterest = 15f;
    [SerializeField] float minDistanceToInterest = 4f;
    [SerializeField] Vector2 timeChangeGoto = new Vector2(3f, 9f);
    [SerializeField] Vector2 timeShoot = new Vector2(.1f, 3f);
    [SerializeField] float timePrediction = 1.5f;

    [SerializeField] SimpleFXs fxMuerte;
    [SerializeField] float fxMuerteScale = 1f;

    Vector2 _apuntaActual = Vector2.right;
    Vector2 ApuntaActual
    {
        get => _apuntaActual;
        set
        {
            _apuntaActual = value.normalized;
            transform.localScale = _apuntaActual.x > 0f ? Vector3.one : new Vector3(-1f, 1f, 1f);
            if (Animator)
            {
                Animator.SetFloat(animApunta, Vector2.Angle(Vector2.down, _apuntaActual) / 180f);
            }
        }
    }
    
    Atacable _atacable;
    Atacable Atacable
    {
        get
        {
            if (!_atacable)
            {
                _atacable = GetComponent<Atacable>();
                if (!_atacable) _atacable = gameObject.AddComponent<Atacable>();
            }
            return _atacable;
        }
    }

    Atacable objetivoActual;

    Vector2 vectorOfInterest;

    Vector2 positionOfInterest;
    Vector2 _gotoOffset;
    Vector2 Goto => positionOfInterest + _gotoOffset;
    // float lastdt = 1f;

    void OnEnable()
    {
        corsarios.Add(this);
    }
    void OnDisable()
    {
        corsarios.Remove(this);
    }

    void Start()
    {
        positionOfInterest = transform.position;
        StartCoroutine(ResetGotoOffset());
        StartCoroutine(Shoot());
        Atacable.AlRecibirAtaque += RecibirAtaque;
    }
    void RecibirAtaque(float daño)
    {
        if (Atacable.dañoAcumulado >= Stats.maxHP)
        {
            if (fxMuerte) {
                var fx = Instantiate(fxMuerte, transform.position, Quaternion.identity);
                fx.transform.localScale = Vector3.one* fxMuerteScale;
            }
            Destroy(gameObject);
        }
        else
        {
            if (ColorControl) ColorControl.ColorFlash();
        }
    }

    void Update()
    {
        if (objetivoActual)
        {
            ApuntaActual = objetivoActual.Pos - transform.position;
            if (Time.deltaTime != 0f)
            {
                vectorOfInterest = ((Vector2)objetivoActual.Pos - positionOfInterest) * timePrediction / Time.deltaTime;
            }
            positionOfInterest = objetivoActual.Pos;
        }
        else
        {
            objetivoActual = Piloto.Cercano(transform.position)?.Atacable;
        }

        if (Rigid) UpdateMovimiento();
    }

    IEnumerator ResetGotoOffset()
    {
        while (this)
        {
            _gotoOffset = Quaternion.Euler(0, 0, Random.value * 360f) * Vector2.right * Random.Range(minDistanceToInterest, maxDistanceToInterest);
            yield return new WaitForSeconds(Random.Range(timeChangeGoto.x, timeChangeGoto.y));
        }
    }
    IEnumerator Shoot()
    {
        while (this)
        {
            while (objetivoActual && Vector2.Distance(objetivoActual.Pos, transform.position) < maxDistanceToInterest)
            {
                PointDefense.Disparar(ApuntaActual);
                yield return new WaitForSeconds(Random.Range(timeShoot.x, timeShoot.y));
            }
            yield return null;
        }
    }

    private void UpdateMovimiento()
    {
        var dt = Time.inFixedTimeStep ? Time.fixedDeltaTime : Time.deltaTime;
        var diffInteres = positionOfInterest - (Vector2)transform.position;

        if (diffInteres.magnitude > maxDistanceToInterest)
        {
            var velDeseada = diffInteres.normalized * VelActiva;
            Rigid.velocity = Vector2.MoveTowards(Rigid.velocity, velDeseada, AcelPasiva * dt);
            // var dirToInterest = Vector2.SignedAngle()
        }
        else //if (diffInteres.magnitude < minDistanceToInterest)
        {
            diffInteres = Goto - (Vector2)transform.position; // estando cerca, se
            var velDeseada = diffInteres.normalized * velPasiva;
            Rigid.velocity = Vector2.MoveTowards(Rigid.velocity, velDeseada, AcelPasiva * dt);


        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.DrawRay(transform.position, ApuntaActual);
        Gizmos.DrawWireSphere(positionOfInterest, maxDistanceToInterest);
        Gizmos.DrawWireSphere(positionOfInterest, minDistanceToInterest);

        Gizmos.DrawWireCube(Goto, Vector3.one * 0.2f);

        Gizmos.DrawRay(positionOfInterest, vectorOfInterest);
    }
}
