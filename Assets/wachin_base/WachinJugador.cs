using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System.Linq;

[DisallowMultipleComponent]
public class WachinJugador : NetworkBehaviour
{
    public static WachinJugador local;

    public GameObject[] posiblesGorros = new GameObject[0];

    public float maxHp = 3;
    public KeyCode der = KeyCode.RightArrow, aba = KeyCode.DownArrow, izq = KeyCode.LeftArrow, arr = KeyCode.UpArrow;
    public KeyCode derAlt = KeyCode.D, abaAlt = KeyCode.S, izqAlt = KeyCode.A, arrAlt = KeyCode.W;
    public float rifleTimeToLower = 3f;
    float currentRifleLowerTime = 0;

    [SyncVar]
    public int gorroIndex = -1;

    [SyncVar]
    [SerializeField] int _clipSize = 8;
    [SyncVar] public float reloadDuration = 1f;

    [SerializeField, SyncVar] int _currentBulletCount;
    public int CurrentBulletCount => _currentBulletCount;
    public int ClipSize
    {
        get => _clipSize;
        set => _clipSize = value;
    }
    // [SerializeField, SyncVar]
    float _reloadingMark;
    public bool IsReloading => Time.time < _reloadingMark;
    public float ReloadingProgress => 1f - (_reloadingMark - Time.time) / reloadDuration;

    [SerializeField] Transform cabezaRefe;

    [SerializeField] int mouseAttackButton = 0, mouseRollButton = 1;
    [Header("Mal Herido")]
    public float factorMovMalherido = 0.65f;
    public float factorDisparosMalherido = 1.5f;
    public float factorRecargaMalherido = 1.5f;

    WachinLogica _wachin;
    public WachinLogica Wachin => _wachin ? _wachin : _wachin = GetComponent<WachinLogica>();

    bool shotIntent;
    [Command]
    void CmdShotIntent(bool value)
    {
        shotIntent = value;
    }

    bool rollIntent;
    [Command]
    void CmdRollIntent(bool value)
    {
        rollIntent = value;
    }

    public float HPActual => maxHp - (Atacable ? Atacable.dañoAcumulado : 0);
    Atacable _atacable;
    public Atacable Atacable => _atacable ? _atacable : _atacable = GetComponent<Atacable>();

    bool _malHerido = false;
    bool MalHerido {
        get => _malHerido;
        set {
            if (_malHerido != value) {
                _malHerido = value;
                if (_malHerido) {
                    Wachin.puedeRollar = false;
                    Wachin.MaxVel *= factorMovMalherido;
                    reloadDuration *= factorRecargaMalherido;
                    var rifle = GetComponentInChildren<Rifle>();
                    if (rifle) rifle.cooldown *= factorDisparosMalherido;
                    var escopeta = GetComponentInChildren<Escopeta>();
                    if (escopeta) escopeta.stats.cooldown *= factorDisparosMalherido;
                }
                else {
                    Wachin.puedeRollar = true;
                    Wachin.MaxVel /= factorMovMalherido;
                    reloadDuration /= factorRecargaMalherido;
                    var rifle = GetComponentInChildren<Rifle>();
                    if (rifle) rifle.cooldown /= factorDisparosMalherido;
                    var escopeta = GetComponentInChildren<Escopeta>();
                    if (escopeta) escopeta.stats.cooldown /= factorDisparosMalherido;
                }
            }
        }
    }

    [ServerCallback]
    void Awake()
    {
        _currentBulletCount = _clipSize;
        if (Atacable) Atacable.AlRecibirAtaque += AlRecibirAtaque;
    }

    IEnumerator Start()
    {
        if (hasAuthority) local = this;

        if (gorroIndex >= 0)
        {
            var gorro = Instantiate(posiblesGorros[gorroIndex % posiblesGorros.Length], cabezaRefe.position, cabezaRefe.rotation, cabezaRefe.parent);
            gorro.transform.localPosition = cabezaRefe.localPosition;
            gorro.transform.localRotation = cabezaRefe.localRotation;
            gorro.name = cabezaRefe.name;
            Destroy(cabezaRefe.gameObject);
            yield return null;
            Wachin.Animator.Rebind();
        }
    }

    void AlRecibirAtaque(float dmg)
    {
        if (Atacable.dañoAcumulado >= maxHp)
        {
            Noquear();
            // Destroy(gameObject);
            // NetworkServer.Destroy(gameObject);
        }
    }

    void Noquear() {
        MalHerido = true;
        Wachin.Noqueade = true;
        StartCoroutine(GameUtils.EsperarTrueLuegoHacerCallback(
            ()=>!Wachin.Noqueade
                || !WachinEnemigo.todes
                    .Any(enemigo=>Vector3.Distance(transform.position,enemigo.transform.position)<enemigo.maxViewDist),
            ()=>Wachin.Noqueade = false
        ));
    }

    IEnumerator Reload()
    {
        if (IsReloading) yield break;
        // _reloadingMark = Time.time + reloadDuration;
        RpcSetReloadingMark();
        yield return new WaitForSeconds(reloadDuration);
        _currentBulletCount = _clipSize;
    }
    [ClientRpc]
    void RpcSetReloadingMark()
    {
        _reloadingMark = Time.time + reloadDuration - (float)NetworkTime.offset;
    }

    void Update()
    {
        if (hasAuthority)
        {

            var intent = Vector3.zero;
            var isDer = Input.GetKey(der) || Input.GetKey(derAlt);
            var isIzq = Input.GetKey(izq) || Input.GetKey(izqAlt);
            var isArr = Input.GetKey(arr) || Input.GetKey(arrAlt);
            var isAba = Input.GetKey(aba) || Input.GetKey(abaAlt);
            if (isDer ^ isIzq)
            {
                intent += isDer ? Vector3.right : Vector3.left;
            }
            if (isAba ^ isArr)
            {
                intent += isArr ? Vector3.forward : Vector3.back;
            }
            // Wachin.PosBuscada = transform.position+intent*Wachin.maxVel*Time.deltaTime*2f;
            Wachin.MovDir = intent;

            if (Input.mousePosition.x >= 0f && Input.mousePosition.y >= 0f
                && Input.mousePosition.x <= Screen.width && Input.mousePosition.y <= Screen.height)
            {
                var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                var plane = new Plane(transform.up, transform.position);
                float enter;
                if (plane.Raycast(ray, out enter))
                {
                    Wachin.MiraHacia = ray.GetPoint(enter);
                }
            }

            var shotIntentNow = Input.GetMouseButton(mouseAttackButton);
            if (shotIntentNow != shotIntent) CmdShotIntent(shotIntent = shotIntentNow);

            if (Input.GetMouseButtonDown(mouseRollButton) && intent != Vector3.zero && !Wachin.IsRolling) Wachin.CmdRoll(intent);
            // var rollIntentNow = Input.GetMouseButtonDown(mouseRollButton);
            // if (rollIntentNow != rollIntent) CmdRollIntent(rollIntent = rollIntentNow);
        }

        if (!isServer) return;

        if (shotIntent && !Wachin.IsRolling && !Wachin.Noqueade)
        {
            if (_currentBulletCount > 0)
            {
                if (Wachin.ItemActivo.Activable)
                {
                    _currentBulletCount--;
                    Wachin.Rifle = true;
                    Wachin.ItemActivo.Activar();
                    currentRifleLowerTime = Time.time + rifleTimeToLower;
                }
            }
            else if (!IsReloading)
            {
                StartCoroutine(Reload());
            }
        }
        // if (rollIntent && !Wachin.IsRolling && Wachin.MovDir != Vector3.zero)
        // {
        //     Wachin.Roll(Wachin.MovDir);
        // }
        if (Wachin.Rifle && Time.time > currentRifleLowerTime)
        {
            Wachin.Rifle = false;
        }
    }

    [Command]
    void CmdActivarItem()
    {
        if (Wachin.IsRolling) return;
        if (_currentBulletCount > 0)
        {
            if (Wachin.ItemActivo.Activable)
            {
                _currentBulletCount--;
                Wachin.Rifle = true;
                Wachin.ItemActivo.Activar();
                currentRifleLowerTime = Time.time + rifleTimeToLower;
            }
        }
        else if (!IsReloading)
        {
            StartCoroutine(Reload());
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(Wachin.MiraHacia, .2f);
        if (RondaActual.actual)
        {
            var ind = RondaActual.actual.GetPositionIndex(transform.position);
            var pos = RondaActual.actual.GetIndexedPosition(ind);
#if UNITY_EDITOR
            GUI.color = Color.red;
            UnityEditor.Handles.Label(transform.position + Vector3.up, $"indice : {ind}\npos : {pos}");
            GUI.color = Color.white;
#endif

            Gizmos.DrawCube(pos, Vector3.one * RondaActual.actual.gridStepSize / 3f);
        }
    }
}
