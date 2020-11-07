using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HitFlash : MonoBehaviour
{
    WachinJugador _jugadorLocal;
    WachinJugador JugadorLocal => WachinJugador.local;

    Image _img;
    Image Img => _img ? _img : _img = GetComponent<Image>();

    Atacable _att;

    private void OnEnable()
    {
        StartCoroutine(GameUtils.EsperarTrueLuegoHacerCallback(
            () => JugadorLocal,
            () =>
            {
                if (JugadorLocal && JugadorLocal.Atacable)
                {
                    (_att = JugadorLocal.Atacable).AlRecibirAtaqueClient += Flash;
                }
            }
        ));
    }
    // void Update()
    // {
        // if (!_att) OnEnable();
    // }
    private void OnDisable()
    {
        if (_att != null)
        {
            _att.AlRecibirAtaqueClient -= Flash;
            _att = null;
        }
    }

    void Flash()
    {
        StartCoroutine(FlashCo());
    }
    IEnumerator FlashCo()
    {
        Img.enabled = true;
        yield return null;
        Img.enabled = false;
    }
}
