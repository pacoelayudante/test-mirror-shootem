using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BibliotecaBrutaDeMods : ScriptableObject
{
    public float armaduraFactorVelocidad = 0.75f;
    public int armaduraVidaExtra = 1;

    public int precisionCostoVida = 1;
    public float precisonFactorDeError = 0.65f;

    public Escopeta.Stats escopetaStats = new Escopeta.Stats();
    public int escopetaClipSize = 4;
    public float escopetaFactorReloadTime = 2f;

    public void ModFijoArmaduraLenta(WachinLogica wachin)
    {
        var jug = wachin.GetComponent<WachinJugador>();
        var enemi = wachin.GetComponent<WachinEnemigo>();
        wachin.MaxVel *= armaduraFactorVelocidad;
        if (jug) jug.maxHp += armaduraVidaExtra;
        if (enemi) enemi.fullHp += armaduraVidaExtra;
    }

    public void ModFijoPrecisionPeligrosa(WachinLogica wachin)
    {
        var jug = wachin.GetComponent<WachinJugador>();
        var enemi = wachin.GetComponent<WachinEnemigo>();

        var rifle = wachin.GetComponentInChildren<Rifle>();
        if (rifle) rifle.amplitudRandom *= precisonFactorDeError;

        if (jug) jug.maxHp -= precisionCostoVida;
        if (enemi) enemi.fullHp -= precisionCostoVida;
    }

    public void ModFijoEscopeta(WachinLogica wachin)
    {
        var rifle = wachin.GetComponentInChildren<Rifle>();

        var escopeta = rifle.gameObject.AddComponent<Escopeta>();
        escopeta.stats = escopetaStats;
        escopeta.equipo = rifle.equipo;
        escopeta.salidaDisparo = rifle.salidaDisparo;
        escopeta.disparoPrefab = rifle.disparoPrefab;
        escopeta.vfxPrefab = rifle.vfxPrefab;

        var jug = wachin.GetComponent<WachinJugador>();
        if (jug) {
            jug.ClipSize = escopetaClipSize;
            jug.reloadDuration *= escopetaFactorReloadTime;
        }

        Destroy(rifle);
    }
}
