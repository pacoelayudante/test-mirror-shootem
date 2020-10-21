using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HitFlash : MonoBehaviour
{
    ControlFlechitas _jugadorLocal;
    ControlFlechitas JugadorLocal => _jugadorLocal?_jugadorLocal:_jugadorLocal=FindObjectOfType<ControlFlechitas>();
    
    Image _img;
    Image Img => _img?_img:_img=GetComponent<Image>();

    Atacable _att;

    private void OnEnable() {
        if (JugadorLocal && JugadorLocal.Atacable) {
            (_att = JugadorLocal.Atacable).AlRecibirAtaque += Flash;
        }
    }
    private void OnDisable() {
        if (_att != null) _att.AlRecibirAtaque -= Flash;
    }

    void Flash(float dmg) {
        if (dmg > 0f) {
            StartCoroutine(FlashCo());
        }
    }
    IEnumerator FlashCo() {
        Img.enabled = true;
        yield return null;
        Img.enabled = false;
    }
}
