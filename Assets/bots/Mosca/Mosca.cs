using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Guazu.DrawersCopados;

[DisallowMultipleComponent]
public class Mosca : MonoBehaviour
{
    static List<List<Mosca>> todosLosGrupos = new List<List<Mosca>>();

    static List<Mosca> todasLasMoscas = new List<Mosca>();

    List<Mosca> _grupo = null;
    public List<Mosca> Grupo
    {
        get => _grupo;
        set
        {
            if (value == _grupo) return;
            if (_grupo != null)
            {
                _grupo.Remove(this);
                if (_grupo.Count == 0) todosLosGrupos.Remove(_grupo);
            }
            _grupo = value;
            if (_grupo != null)
            {
                _grupo.Add(this);
                if (_grupo.Count == 1)
                {
                    _direccionDeGrupo = Velocidad;
                    // _anguloDeGrupo = Angulo;
                    _centroDeGrupo = transform.position;
                    todosLosGrupos.Add(_grupo);
                }
            }
        }
    }

    Rigidbody2D _rigid;
    public Rigidbody2D Rigid => _rigid ? _rigid : _rigid = GetComponent<Rigidbody2D>();
    ColorControl _colorControl;
    ColorControl ColorControl => _colorControl?_colorControl:_colorControl = GetComponent<ColorControl>();
    Collider2D[] hit = new Collider2D[1];

    [CreameScriptable]
    [SerializeField] MoscaStats _moscaStats = null;

    [Ocultador("_moscaStats", true)]
    [SerializeField] int tamDeGrupo = 7;
    int TamDeGrupo => _moscaStats ? _moscaStats.tamDeGrupo : tamDeGrupo;
    [Ocultador("_moscaStats", true)]
    [SerializeField] float radioDeInfluencia = 4f;
    float RadioDeInfluencia => _moscaStats ? _moscaStats.radioDeInfluencia : radioDeInfluencia;
    [Ocultador("_moscaStats", true)]
    [SerializeField] float demasiadoCerca = .5f;
    float DemasiadoCerca => _moscaStats ? _moscaStats.demasiadoCerca : demasiadoCerca;

    [Ocultador("_moscaStats", true)]
    [Range(0f, 1f)]
    [SerializeField] float alineacion = 1f, cohesion = 1f, separacion = 1f;
    float Alineacion => _moscaStats ? _moscaStats.alineacion : alineacion;
    float Cohesion => _moscaStats ? _moscaStats.cohesion : cohesion;
    float Separacion => _moscaStats ? _moscaStats.separacion : separacion;

    [Ocultador("_moscaStats", true)]
    [SerializeField] float velMaximia = 5f;
    float VelMaximia => _moscaStats ? _moscaStats.velMaximia : velMaximia;
    [Ocultador("_moscaStats", true)]
    [SerializeField] float rotMaxima = 90f;
    float RotMaxima => _moscaStats ? _moscaStats.rotMaxima : rotMaxima;
    Vector2 _direccionDeGrupo = Vector2.zero;
    Vector2 _centroDeGrupo = Vector2.zero;
    Vector2 _objetivoGrupo = Vector2.zero;
    // float _anguloDeGrupo = 0f;
    
    [SerializeField] SimpleFXs fxMuerte;
    [SerializeField] float fxMuerteScale = 1f;

    ContactFilter2D PilotoContact => _moscaStats ? _moscaStats.pilotoContact : new ContactFilter2D();

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

    Atacable enemigo;

    bool PuedenDesagruparse => _moscaStats ? _moscaStats.puedenDesagruparse : false;

    public Vector2 Velocidad
    {
        get => Rigid ? Rigid.velocity : Vector2.zero;
        set
        {
            if (Rigid) Rigid.velocity = value;
        }
    }
    public float Angulo
    {
        get => Vector2.SignedAngle(Vector2.right, Velocidad);
        set => Velocidad = Quaternion.Euler(0f, 0f, value) * Vector2.right * VelMaximia;
    }

    void Start()
    {
        // if (Rigid) Rigid.velocity = Quaternion.Euler(0f, 0f, Random.value * 360f) * Vector2.right * velMaximia;
        Angulo = Random.value * 360f;
        Atacable.AlRecibirAtaque += RecibirAtaque;
    }

    void RecibirAtaque(float daño)
    {
        if (Atacable.dañoAcumulado >= _moscaStats.maxHP)
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

    void OnEnable()
    {
        todasLasMoscas.Add(this);
    }
    void OnDisable()
    {
        Grupo = null;
        todasLasMoscas.Remove(this);
    }

    void Update()
    {
        UpdateCheckGrupo();

        CoherenciaDeGrupo();
        var dt = Time.inFixedTimeStep ? Time.fixedDeltaTime : Time.deltaTime;

        var alejarse = Vector2.zero;
        if (Grupo != null && Grupo.Count > 2)
        {
            var moscaCercanaCombo = Grupo.Where(mosca => mosca != this).Select(mosca => new { mosca = mosca, dist = Vector2.Distance(mosca.Rigid.position, Rigid.position) })
                // .OrderBy(combo=>combo.dist).FirstOrDefault(combo=>combo.dist<demasiadoCerca)?.mosca;
                .OrderBy(combo => combo.dist).FirstOrDefault();
            if (moscaCercanaCombo != null)
            {
                if (PuedenDesagruparse && moscaCercanaCombo.dist > RadioDeInfluencia)
                {
                    Grupo = null;
                }
                else if (moscaCercanaCombo.dist < demasiadoCerca)
                {
                    alejarse = (Rigid.position - moscaCercanaCombo.mosca.Rigid.position).normalized;
                }
            }
        }
        var dirDeseada = (_direccionDeGrupo * Alineacion + (_centroDeGrupo - Rigid.position).normalized * Cohesion + alejarse * Separacion).normalized;
        var anguloDeseado = Vector2.SignedAngle(Vector2.right, dirDeseada);

        Angulo = Mathf.MoveTowardsAngle(Angulo, anguloDeseado, RotMaxima * dt);

        if (Rigid.OverlapCollider(PilotoContact, hit) > 0)
        {
            var piloto = hit[0].GetComponent<Piloto>();
            if (piloto) piloto.Stun(_moscaStats.stunStrength);
        }
    }
    void UpdateCheckGrupo()
    {
        if (todasLasMoscas[Time.frameCount % todasLasMoscas.Count] == this)
        {
            ActualizarAgrupamiento();
        }
    }

    void CoherenciaDeGrupo()
    {
        if (Grupo != null)
        {
            if (Grupo[0] == this)
            {
                if (Random.value < Time.realtimeSinceStartup % .03f)
                {
                    _objetivoGrupo = Quaternion.Euler(0, 0, Random.value * 360f) * Vector2.right * 20f;
                }

                _direccionDeGrupo = Grupo.Select(mosca => mosca.Velocidad).Aggregate(Vector2.zero, (vel1, vel2) => vel1 + vel2).normalized;
                // _direccionDeGrupo = Grupo.Select(mosca => mosca.Velocidad).Aggregate(_objetivoGrupo*Grupo.Count, (vel1, vel2) => vel1 + vel2).normalized;
                _centroDeGrupo = Grupo.Select(mosca => (Vector2)mosca.transform.position)
                    .Aggregate(_objetivoGrupo, (p1, p2) => p1 + p2) / (Grupo.Count + 1f);
                // _anguloDeGrupo = Vector2.SignedAngle(Vector2.right, _direccionDeGrupo);
            }
            else
            {
                _direccionDeGrupo = Grupo[0]._direccionDeGrupo;
                _centroDeGrupo = Grupo[0]._centroDeGrupo;
                // _anguloDeGrupo = Grupo[0]._anguloDeGrupo;
            }
        }
    }

    void LateUpdate()
    {
        if (Rigid)
        {
            transform.rotation = Quaternion.Euler(0f, 0f, Vector2.SignedAngle(Vector2.right, Rigid.velocity));
        }
    }

    void ActualizarAgrupamiento()
    {
        // los grupos grandes absorven moscas sueltas o que estan en grupos mas pequeños
        // las moscas sueltas se unen a un grupo, o crean nuevos grupos
        var posiblesMoscas = todasLasMoscas.Where(mosca => mosca != this && mosca);//todas las que no soy yo (y existen)
        if (Grupo != null && Grupo.Count == 1) Grupo = null;//si mi grupo es yo solo, me actualizo a no tener grupo
        if (Grupo == null)
        {
            var moscaParaSumar = posiblesMoscas //conque este cerca alcanza
                .FirstOrDefault(mosca => Vector2.Distance(mosca.transform.position, transform.position) < RadioDeInfluencia);
            if (moscaParaSumar == null) return;//no hay moscas cerca

            //me sumo a su grupo, y si no tiene grupo formamos uno nuevo
            Grupo = (moscaParaSumar.Grupo != null ? moscaParaSumar.Grupo : moscaParaSumar.Grupo = new List<Mosca>());
        }
        else if (Grupo.Count >= TamDeGrupo + 2)
        {
            var nuevo = Grupo[Grupo.Count - 1].Grupo = new List<Mosca>();
            Grupo[Grupo.Count - 1].Grupo = nuevo;
        }
        else
        {
            var moscaParaSumar = posiblesMoscas// si no tiene grupo, o esta en un grupo mas chico (y el grupo grande le falta llenar)
                .Where(mosca => mosca.Grupo != Grupo)// y que no sea de mi mismo grupo
                .Where(mosca => mosca.Grupo == null || (Grupo.Count < TamDeGrupo && mosca.Grupo.Count <= Grupo.Count))
                .FirstOrDefault(mosca => Vector2.Distance(mosca.transform.position, transform.position) < RadioDeInfluencia);

            if (moscaParaSumar == null) return;//no hay moscas cerca
            moscaParaSumar.Grupo = Grupo;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (todasLasMoscas.Count > 0 && todasLasMoscas[Time.frameCount % todasLasMoscas.Count] == this)
        {
            Gizmos.color = Grupo == null ? Color.blue : Color.red;
            Gizmos.DrawWireSphere(transform.position, RadioDeInfluencia);
        }

        if (Grupo != null && Grupo.Count > 0)
        {
            Gizmos.color = Grupo.Count == TamDeGrupo || Grupo.Count == TamDeGrupo + 1 ? Color.yellow * 0.8f : Color.white * 0.65f;
            var ulti = Grupo[0];
            foreach (var cada in Grupo)
            {
                if (cada == Grupo[0])
                {
                    Gizmos.DrawRay(cada.transform.position, cada._direccionDeGrupo);
                    Gizmos.DrawWireCube(cada._centroDeGrupo, Vector2.one * .25f);
                    Gizmos.DrawWireCube(cada._objetivoGrupo, Vector2.one * .25f);
                }
                if (ulti == cada) continue;
                Gizmos.DrawLine(cada.transform.position, ulti.transform.position);
                ulti = cada;
            }
        }
    }
}
