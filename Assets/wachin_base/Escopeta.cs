using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// using Mirror;

// public class Rifle : NetworkBehaviour
 public class Escopeta : MonoBehaviour
{
    public Equipo equipo;
    public Transform salidaDisparo;
    public Disparo disparoPrefab;
    public ColorControl vfxPrefab;
    
    [SerializeField]
    public Stats stats = new Stats();

    [System.Serializable]
    public class Stats {
    public float velocidadDisparo = 20;
    public float distanciaDisparo = 20;
    public float cooldown = .6f;
    public float daño = 0.5f;
    public float amplitud = 30f;
    public int cantidadDeTiros = 4;
    public float amplitudRandom = 0f;
    }

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
        tSiguienteDisparo = Time.time+stats.cooldown;

        var rotacionInicial = stats.cantidadDeTiros>1? Quaternion.Euler(0f,-.5f*stats.amplitud+Random.Range(-stats.amplitudRandom,stats.amplitudRandom),0f) : Quaternion.identity; 
        var rotacionPorTiro = stats.cantidadDeTiros>1? Quaternion.Euler(0f,stats.amplitud/(stats.cantidadDeTiros-1),0f) : Quaternion.identity; 
        
        for(int i=0; i<stats.cantidadDeTiros; i++) {
            
        var disparo = Instantiate(disparoPrefab, salidaDisparo.position, rotacionInicial * salidaDisparo.rotation);
        rotacionInicial *= rotacionPorTiro;
        disparo.Velocidad = disparo.transform.forward * stats.velocidadDisparo;
        disparo.StartCoroutine(AutoDestruirDisparo(disparo, stats.distanciaDisparo / stats.velocidadDisparo));
        Mirror.NetworkServer.Spawn(disparo.gameObject);

        var vfx = Instantiate(vfxPrefab, salidaDisparo.position, salidaDisparo.rotation);
        // vfx.ColorFade(fadeFrom: false);
        Destroy(vfx.gameObject, vfx.fadeTimeDefault);
        Mirror.NetworkServer.Spawn(vfx.gameObject);
        }
    }

    public void ReemplazarActivar(System.Action reemplazo) {
        if (ItemActivo) {
            ItemActivo.alActivar -= Activar;
            ItemActivo.alActivar += reemplazo;
        }
    }

    IEnumerator AutoDestruirDisparo(Disparo disparo, float t)
    {
        disparo.onTriggerEnter += (col) =>
        {//on hit
            if (!disparo) return;
            var atacable = col.GetComponent<Atacable>();
            if (atacable) {
                if (equipo==atacable.equipo) return;
                atacable.RecibirAtaque(stats.daño);
            }

            var vfx = Instantiate(vfxPrefab, disparo.transform.position, Quaternion.Euler(0f, 180f, 0f) * disparo.transform.rotation);
            // vfx.ColorFade(fadeFrom: false);
            Destroy(vfx.gameObject, vfx.fadeTimeDefault);
            Mirror.NetworkServer.Spawn(vfx.gameObject);
            Destroy(disparo.gameObject);
        };
        yield return new WaitForSeconds(t);
        if (disparo) Destroy(disparo.gameObject);
    }
}
