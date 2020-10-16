using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Equipo : MonoBehaviour
{
    public int equipo = -1;

    public static bool operator ==(Equipo a, Equipo b) => (object)a!=null&&(object)b!=null&&a.equipo!=-1&&b.equipo!=-1&&a.equipo==b.equipo;
    public static bool operator !=(Equipo a, Equipo b) => (object)a==null||(object)b==null||a.equipo==-1||b.equipo==-1||a.equipo!=b.equipo;
    public static bool operator ==(Equipo a, int b) => (object)a!=null&&a.equipo!=-1&&b!=-1&&a.equipo==b;
    public static bool operator !=(Equipo a, int b) => (object)a==null||a.equipo==-1||b==-1||a.equipo!=b;

    public override int GetHashCode()=>equipo.GetHashCode();
    public override bool Equals(object o)=>base.Equals(o);

    void Awake() {
        var atacable = GetComponent<Atacable>();
        if (atacable) atacable.equipo = equipo;
    }
}
