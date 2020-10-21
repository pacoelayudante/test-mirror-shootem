using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HitFlash : MonoBehaviour
{
    WachinJugador _jugadorLocal;
    WachinJugador JugadorLocal => _jugadorLocal?_jugadorLocal:_jugadorLocal=FindObjectOfType<WachinJugador>();
    
    Image _img;
    Image Img => _img?_img:_img=GetComponent<Image>();

    Atacable _att;

    private void OnEnable() {
        if (JugadorLocal && JugadorLocal.Atacable) {
            (_att = JugadorLocal.Atacable).AlRecibirAtaque += Flash;
        }
    }
    void Update() {
        if (!_att) OnEnable();
    }
    private void OnDisable() {
        if (_att != null) {
            _att.AlRecibirAtaque -= Flash;
            _att = null;
        }
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
