using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class WachinJugador : MonoBehaviour
{
    public float maxHp = 3;
    public KeyCode der = KeyCode.RightArrow,aba = KeyCode.DownArrow,izq = KeyCode.LeftArrow,arr = KeyCode.UpArrow;
    public KeyCode derAlt = KeyCode.D,abaAlt = KeyCode.S,izqAlt = KeyCode.A,arrAlt = KeyCode.W;
    public float rifleTimeToLower = 3f;
    float currentRifleLowerTime = 0;

    [SerializeField] int _clipSize = 8;
    [SerializeField] float reloadDuration = 1f;
    int _currentBulletCount;
    public int CurrentBulletCount => _currentBulletCount;
    public int ClipSize => _clipSize;
    float _reloadingMark;
    public bool IsReloading => Time.time < _reloadingMark;
    public float ReloadingProgress => 1f - (_reloadingMark - Time.time) / reloadDuration;

    [SerializeField] int mouseAttackButton = 0, mouseRollButton = 1;

    WachinLogica _wachin;
    WachinLogica Wachin => _wachin?_wachin:_wachin = GetComponent<WachinLogica>();
    
    public float HPActual => maxHp-(Atacable?Atacable.dañoAcumulado:0);
    Atacable _atacable;
    public Atacable Atacable => _atacable ? _atacable : _atacable = GetComponent<Atacable>();

    void Start() {
        _currentBulletCount = _clipSize;
        if(Atacable)Atacable.AlRecibirAtaque += AlRecibirAtaque;
    }

    void AlRecibirAtaque(float dmg) {
        if (Atacable.dañoAcumulado >= maxHp) {
            Destroy(gameObject);
        }
    }

    IEnumerator Reload() {
        if (IsReloading) yield break;
        _reloadingMark = Time.time+reloadDuration;
        yield return new WaitForSeconds(reloadDuration);
        _currentBulletCount = _clipSize;
    }

    void Update() {
        var intent = Vector3.zero;
        var isDer = Input.GetKey(der) || Input.GetKey(derAlt);
        var isIzq = Input.GetKey(izq) || Input.GetKey(izqAlt);
        var isArr = Input.GetKey(arr) || Input.GetKey(arrAlt);
        var isAba = Input.GetKey(aba) || Input.GetKey(abaAlt);
        if (isDer^isIzq) {
            intent += isDer?Vector3.right:Vector3.left;
        }
        if (isAba^isArr) {
            intent += isArr?Vector3.forward:Vector3.back;
        }
        Wachin.PosBuscada = transform.position+intent*Wachin.maxVel*Time.deltaTime*2f;

        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        var plane = new Plane(transform.up, transform.position);
        float enter;
        if (plane.Raycast(ray, out enter)) {
            Wachin.miraHacia = ray.GetPoint(enter);
        }

        if (Input.GetMouseButton(mouseAttackButton) && !Wachin.IsRolling) {
            if (_currentBulletCount > 0) {
                if (Wachin.ItemActivo.Activable) {
                    _currentBulletCount--;
                    Wachin.Rifle = true;
                    Wachin.ItemActivo.Activar();
                    currentRifleLowerTime = Time.time+rifleTimeToLower;
                }
            }
            else if (!IsReloading) {
                StartCoroutine(Reload());
            }
        }
        if (Input.GetMouseButton(mouseRollButton) && !Wachin.IsRolling && intent!=Vector3.zero) {
            Wachin.Roll(intent);
        }
        if (Wachin.Rifle && Time.time > currentRifleLowerTime) {
            Wachin.Rifle = false;
        }
    }
}
