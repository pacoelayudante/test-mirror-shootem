using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Guazu.DrawersCopados;
using System.Linq;

[DisallowMultipleComponent]
public class Piloto : MonoBehaviour
{
    static List<Piloto> pilotos = new List<Piloto>();

    Animator _animator;
    public Animator Animator => _animator ? _animator : _animator = GetComponent<Animator>();

    Rigidbody2D _rigid;
    public Rigidbody2D Rigid => _rigid ? _rigid : _rigid = GetComponent<Rigidbody2D>();
    
    Atacable _atacable;
    public Atacable Atacable => _atacable ? _atacable : _atacable = GetComponent<Atacable>();

    PointDefense _pointDefense;
    PointDefense PointDefense => _pointDefense ? _pointDefense : _pointDefense = GetComponent<PointDefense>();
    [SerializeField] bool pointDefenseAcelerando = true;

    [SerializeField] Cinemachine.CinemachineVirtualCamera virtualCamera = null;
    [Ocultador("virtualCamera")]
    [SerializeField] float tamMenor = 7f, tamMayor = 12f;

    [AnimatorStringList(AnimatorStringListAttribute.Tipo.Parametros)]
    [SerializeField] string animDireccion = "";

    [SerializeField] float velocidadPasiva = 1f, velocidadActiva = 3f;
    [SerializeField] float acelPasiva = 4f;
    [SerializeField] float rotVelPasiva = 180f, rotVelActiva = 90f;

    [SerializeField] float arcoActivoPointDefense = 20f;

    [SerializeField] float stunMaxAmp = 90f;
    // [SerializeField] float stunNoiseScale = .01f;
    // [SerializeField] float stunDecayTime = .2f;
    float stun = 0f;

    public static Piloto Cercano(Vector2 pos)=>pilotos.OrderBy(piloto=>Vector2.Distance(pos,piloto.transform.position)).FirstOrDefault();

    void OnEnable(){
        pilotos.Add(this);
    }
    void OnDisable(){
        pilotos.Remove(this);
    }

    void Update()
    {
        if (PointDefense && (!Input.GetMouseButton(0) || pointDefenseAcelerando) )
        {
            var mouseWorld = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition + Vector3.forward);
            var vectorDeseado = mouseWorld - Rigid.position;
            var difAngular = Vector2.Angle(Rigid.velocity, vectorDeseado);
            if (difAngular < arcoActivoPointDefense && PosibleObjetivo.ObjetivoCerca(mouseWorld))
            {
                var anguloDeseado = Vector2.SignedAngle(Vector2.right, vectorDeseado);
                PointDefense.Disparar(Quaternion.Euler(0, 0, anguloDeseado));
            }
        }

        StunResolution();
    }

    void FixedUpdate()
    {
        var velDeseada = Input.GetMouseButton(0) ? velocidadActiva : velocidadPasiva;
        var rotVel = Input.GetMouseButton(0) ? rotVelActiva : rotVelPasiva;

        var dt = Time.inFixedTimeStep ? Time.fixedDeltaTime : Time.deltaTime;
        var mouseWorld = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition + Vector3.forward);
        var vectorDeseado = mouseWorld - Rigid.position;
        var anguloDeseado = Vector2.SignedAngle(Vector2.right, vectorDeseado);
        var anguloActual = Vector2.SignedAngle(Vector2.right, Rigid.velocity);
        anguloActual = Mathf.MoveTowardsAngle(anguloActual, anguloDeseado, rotVel * dt);
        var newVel = Mathf.MoveTowards(Rigid.velocity.magnitude, velDeseada, acelPasiva * dt);
        Rigid.velocity = Quaternion.Euler(0f, 0f, anguloActual) * Vector2.right * newVel;
    }

    private void LateUpdate()
    {
        virtualCamera.m_Lens.OrthographicSize = Mathf.Lerp(tamMenor, tamMayor,
            Mathf.InverseLerp(velocidadPasiva, velocidadActiva, Rigid.velocity.magnitude));

        // virtualCamera.m_Lens.Dutch = Mathf.Lerp(-90,90, Mathf.PerlinNoise(transform.position.x*0.1f, transform.position.y*0.1f));

        if (Rigid && Animator)
        {
            transform.localScale = Rigid.velocity.x > 0f ? Vector3.one : new Vector3(-1f, 1f, 1f);

            Animator.SetFloat(animDireccion, Vector2.Angle(Vector2.down, Rigid.velocity) / 180f);
        }
    }

    public void Stun(float stunStrenght)
    {
        if (stunStrenght > stun)
        {
            stun = stunStrenght;
            // StunResolution();
        }
    }
    void StunResolution()
    {
        var rotVel = stunMaxAmp*Time.deltaTime * (Input.GetMouseButton(0)?.2f:1f);
        if (stun > 0f)
        {
            stun -= Time.deltaTime;
            // if (stun <= 0f)
            // {
            //     virtualCamera.m_Lens.Dutch = 0f;
            //     return;
            // }

            // var stunState = stun>stunDecayTime?1f:stun/stunDecayTime;
            virtualCamera.m_Lens.Dutch = Mathf.MoveTowardsAngle(virtualCamera.m_Lens.Dutch, virtualCamera.m_Lens.Dutch+45f, rotVel);
            // virtualCamera.m_Lens.Dutch = stunState * Mathf.Lerp(-stunMaxAmp, stunMaxAmp, Mathf.PerlinNoise(Time.realtimeSinceStartup * stunNoiseScale, 0f));
        }
        else {
            virtualCamera.m_Lens.Dutch = Mathf.MoveTowardsAngle(virtualCamera.m_Lens.Dutch, 0f, rotVel);
        }
    }
}
