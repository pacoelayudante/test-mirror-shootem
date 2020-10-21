using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rifle : MonoBehaviour
{
    public Equipo equipo;
    public Transform salidaDisparo;
    public Disparo disparoPrefab;
    public ColorControl vfxPrefab;
    public float velocidadDisparo = 20;
    public float distanciaDisparo = 40;
    public float cooldown = .4f;
    public float daño = 1f;
    public float amplitudRandom = 0f;

    ItemActivo _itemActivo;
    ItemActivo ItemActivo => _itemActivo ? _itemActivo : _itemActivo = GetComponent<ItemActivo>();
    float tSiguienteDisparo;

    void Awake()
    {
        if (ItemActivo) {
            ItemActivo.alActivar += Activar;
            ItemActivo.SetActivableCheck(()=>Time.time >= tSiguienteDisparo);
        }
    }
    void OnDestroy()
    {
        if (ItemActivo) ItemActivo.alActivar -= Activar;
    }

    void Activar()
    {
        if (Time.time < tSiguienteDisparo) return;
        tSiguienteDisparo = Time.time+cooldown;

        var rotacionRandom = Quaternion.Euler(0f,Random.Range(-amplitudRandom,amplitudRandom),0f);
        var disparo = Instantiate(disparoPrefab, salidaDisparo.position, rotacionRandom * salidaDisparo.rotation);
        disparo.Velocidad = disparo.transform.forward * velocidadDisparo;
        disparo.StartCoroutine(AutoDestruirDisparo(disparo, distanciaDisparo / velocidadDisparo));

        var vfx = Instantiate(vfxPrefab, salidaDisparo.position, salidaDisparo.rotation);
        vfx.ColorFade(fadeFrom: false);
        Destroy(vfx.gameObject, vfx.fadeTimeDefault);
    }

    IEnumerator AutoDestruirDisparo(Disparo disparo, float t)
    {
        disparo.onTriggerEnter += (col) =>
        {//on hit
            if (!disparo) return;
            var atacable = col.GetComponent<Atacable>();
            if (atacable) {
                if (equipo==atacable.equipo) return;
                atacable.RecibirAtaque(daño);
            }

            var vfx = Instantiate(vfxPrefab, disparo.transform.position, Quaternion.Euler(0f, 180f, 0f) * disparo.transform.rotation);
            vfx.ColorFade(fadeFrom: false);
            Destroy(vfx.gameObject, vfx.fadeTimeDefault);
            Destroy(disparo.gameObject);
        };
        yield return new WaitForSeconds(t);
        if (disparo) Destroy(disparo.gameObject);
    }
}
