using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ControlFlechitas : MonoBehaviour
{
    public KeyCode der = KeyCode.RightArrow,aba = KeyCode.DownArrow,izq = KeyCode.LeftArrow,arr = KeyCode.UpArrow;
    public KeyCode derAlt = KeyCode.D,abaAlt = KeyCode.S,izqAlt = KeyCode.A,arrAlt = KeyCode.W;
    public float rifleTimeToLower = 3f;
    float currentRifleLowerTime = 0;

    WachinLogica _wachin;
    WachinLogica Wachin => _wachin?_wachin:_wachin = GetComponent<WachinLogica>();

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

        if (Input.GetMouseButton(0)) {
            Wachin.Rifle = true;
            Wachin.ItemActivo.Activar();
            currentRifleLowerTime = Time.time+rifleTimeToLower;
        }
        if (Wachin.Rifle && Time.time > currentRifleLowerTime) {
            Wachin.Rifle = false;
        }
    }
}
