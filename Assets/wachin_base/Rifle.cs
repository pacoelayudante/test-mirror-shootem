﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rifle : MonoBehaviour
{
    public Transform salidaDisparo;
    public Disparo disparoPrefab;
    public ColorControl vfxPrefab;
    public float velocidadDisparo = 20;
    public float distanciaDisparo = 40;
    public float cooldown = .4f;
    public float daño = 1f;

    ItemActivo _itemActivo;
    ItemActivo ItemActivo => _itemActivo ? _itemActivo : _itemActivo = GetComponent<ItemActivo>();
    float tSiguienteDisparo;

    void Awake()
    {
        if (ItemActivo) ItemActivo.alActivar += Activar;
    }
    void OnDestroy()
    {
        if (ItemActivo) ItemActivo.alActivar -= Activar;
    }

    void Activar()
    {
        if (Time.time < tSiguienteDisparo) return;
        tSiguienteDisparo = Time.time+cooldown;

        var disparo = Instantiate(disparoPrefab, salidaDisparo.position, salidaDisparo.rotation);
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
            var atacable = col.GetComponent<Atacable>();
            if (atacable) {
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
