using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Patrulla 
{
    static List<Patrulla> patrullas = new List<Patrulla>();

    List<WachinEnemigo> wachines = new List<WachinEnemigo>();

    public static Patrulla PatrullaEnRango(Vector3 pos, float rango) => patrullas.FirstOrDefault(p=>Vector3.Distance(pos,p.PosLider)<rango);

    public Vector3 PosPromedio => wachines.Aggregate(Vector3.zero,(a,b)=>a+b.transform.position)/wachines.Count;
    public Vector3 PosPseudoMedian {
        get {
            var take = 2 - wachines.Count%2;
            var x = wachines.Select(w=>w.transform.position.x).OrderBy(v=>v).Skip(wachines.Count/2).Take(take).Average();
            var y = wachines.Select(w=>w.transform.position.y).OrderBy(v=>v).Skip(wachines.Count/2).Take(take).Average();
            var z = wachines.Select(w=>w.transform.position.z).OrderBy(v=>v).Skip(wachines.Count/2).Take(take).Average();
            return new Vector3(x,y,z);
        }
    }
    public Vector3 PosLider => wachines[0].transform.position;
    public WachinEnemigo Lider => wachines[0];

    public Vector3 destinoPatrulla;
    public int Count => wachines.Count;

    public float reconsiderarPatrullaCuando = 0f;

    public void Remove(WachinEnemigo wachin) {
        wachines.Remove(wachin);
        if (!patrullas.Contains(this)) Debug.LogError("(remove) Super RARO!");
        if (wachines.Count == 0) patrullas.Remove(this);
    }
    public void Add(WachinEnemigo wachin) {
        if (wachines.Count == 0) {
            destinoPatrulla = wachin.transform.position;
            if (patrullas.Contains(this)) Debug.LogError("(add) Super RARO!");
            patrullas.Add(this);
        }
        wachines.Add(wachin);
    }
}
