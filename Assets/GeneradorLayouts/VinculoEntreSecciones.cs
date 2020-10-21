using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

//pertenece al arbol
//en caso de superposicinon de Colliders.. deberian er REcts en vez de Lines los detalles
[System.Serializable]
public class VinculoEntreSecciones
{
    [SerializeField]
    bool indirecto = false;
    [SerializeField]
    SeccionDeLayout seccionX;
    [SerializeField]
    SeccionDeLayout seccionY;

    public string Label
    {
        get
        {
            return seccionX.name + "-" + seccionY.name+"("+puertas.Count+")";
        }
    }
    public float LargoTotal
    {
        get => detalles.Sum(det => det.lineas.Sum(lin => lin.Largo));
    }
    public GeneradorMapaArbol Arbol
    {
        get
        {
            if (seccionX && seccionX.arbol) return seccionX.arbol;
            else if (seccionY && seccionY.arbol) return seccionY.arbol;
            Debug.LogError("Sin Arbol aqui - " + seccionX + " - " + seccionY);
            return null;
        }
    }

    [System.Serializable]
    public struct Linea
    {
        public Vector2 a, b;
        public float Largo { get => Vector2.Distance(a, b); }
        public Vector2 MoveTowardsAB(float dist)
        {
            return Vector2.MoveTowards(a, b, dist);
        }
    }
    [System.Serializable]
    public class DetalleDeVinculo
    {
        public LayoutCuarto cuartoX, cuartoY;
        public List<Linea> lineas = new List<Linea>();

        public DetalleDeVinculo(LayoutCuarto cuax, LayoutCuarto cuay)
        {
            this.cuartoX = cuax;
            this.cuartoY = cuay;

            if (cuax.derecha.Contains(cuay.BoxCol)) CalcularLinea(0, cuax.BoxCol.bounds, cuay.BoxCol.bounds);
            if (cuax.izquierda.Contains(cuay.BoxCol)) CalcularLinea(0, cuay.BoxCol.bounds, cuax.BoxCol.bounds);
            if (cuax.arriba.Contains(cuay.BoxCol)) CalcularLinea(1, cuax.BoxCol.bounds, cuay.BoxCol.bounds);
            if (cuax.abajo.Contains(cuay.BoxCol)) CalcularLinea(1, cuay.BoxCol.bounds, cuax.BoxCol.bounds);
        }

        public void CalcularLinea(int eje, Bounds miBounds, Bounds tuBounds)
        {//Physics2D.defaultContactOffset*2f
            if (miBounds.max[eje] > tuBounds.min[eje] + Physics2D.defaultContactOffset * 2f) return;//No overlapean

            int otroEje = (eje + 1) % 2;
            // float centro = (miBounds.max[eje] + tuBounds.min[eje])/2f;
            float centro = Mathf.Min(miBounds.max[eje], tuBounds.min[eje]);
            float ladoMin = Mathf.Min(tuBounds.max[otroEje], miBounds.max[otroEje]);
            float ladoMax = Mathf.Max(tuBounds.min[otroEje], miBounds.min[otroEje]);

            var ptoA = Vector2.zero;
            var ptoB = Vector2.zero;
            ptoA[eje] = ptoB[eje] = centro;
            ptoA[otroEje] = ladoMin;
            ptoB[otroEje] = ladoMax;
            lineas.Add(new Linea() { a = ptoA, b = ptoB });
        }
    }

    public void DrawGizmos()
    {
#if UNITY_EDITOR
        foreach(var puerta in puertas){
            Handles.DrawWireDisc( puerta,Vector3.forward,Arbol.OverlapMinimoParaConectar/2f );
        }

        if (this == null || this.detalles == null) return;
        foreach (var detalle in this.detalles)
        {
            if (detalle == null) continue;
            foreach (var linea in detalle.lineas)
            {
                var lineDiff = linea.a - linea.b;
                var pos = (linea.a + linea.b) * 0.5f;
                var rot = Vector2.SignedAngle(Vector2.right, lineDiff);
                var quat = Quaternion.Euler(0f, 0f, rot);

                var size = new Vector2(lineDiff.magnitude, HandleUtility.GetHandleSize(pos) * .05f);

                Handles.matrix = Matrix4x4.TRS(pos, quat, Vector3.one);
                Handles.DrawSolidRectangleWithOutline(new Rect(-size / 2f, size), Handles.color, Color.clear);
                Handles.matrix = Matrix4x4.identity;
            }
        }
#endif
    }

    public List<DetalleDeVinculo> detalles = new List<DetalleDeVinculo>();
    public List<Vector2> puertas = new List<Vector2>();

    public SeccionDeLayout Otro(SeccionDeLayout yo)
    {
        UnityEngine.Assertions.Assert.IsTrue(yo == seccionX || yo == seccionY);
        return seccionX == yo ? seccionY : seccionX;
    }
    public bool Contains(SeccionDeLayout seccion)
    {
        return seccionX == seccion || seccionY == seccion;
    }
    public VinculoEntreSecciones(SeccionDeLayout seccionX, SeccionDeLayout seccionY, bool indirecto = false)
    {
        Set(seccionX, seccionY, indirecto);
    }
    public void Set(SeccionDeLayout seccionX, SeccionDeLayout seccionY, bool indirecto = false)
    {
        if (!seccionX || !seccionY || seccionX == seccionY)
        {
            Debug.LogError("VinculoEntreSecciones " + seccionX + " " + seccionY);
        }
        this.seccionX = seccionX;
        this.seccionY = seccionY;
        this.indirecto = indirecto;
    }

    public static readonly ComparadorInterno Comparador = new ComparadorInterno();
    public class ComparadorInterno : IEqualityComparer<VinculoEntreSecciones>
    {
        public bool Equals(VinculoEntreSecciones a, VinculoEntreSecciones b)
        {
            if (a == b) return true;
            if ((a.seccionX == b.seccionX && a.seccionY == b.seccionY) || (a.seccionX == b.seccionY && a.seccionY == b.seccionX)) return true;
            else return false;
        }
        public int GetHashCode(VinculoEntreSecciones yo)
        {
            return yo.seccionX.GetHashCode() + yo.seccionY.GetHashCode();
        }
    }

    public void IdentificarEjes()
    {
        IdentificarEjes(Arbol.puertasCada, Arbol.OverlapMinimoParaConectar);
    }
    public void IdentificarEjes(float puertasCada, float diametroPuerta)
    {
        detalles.Clear();
        AddVinculoCadaSeccion();
        if (indirecto) IdentificarEjesIndirecto();
        else IdentificarEjesDirectos();

        PuertasDirecto(puertasCada, diametroPuerta);
    }
    public void AddVinculoCadaSeccion()
    {
        seccionX.AddVinculo(this);
        seccionY.AddVinculo(this);
    }

    public void PuertasDirecto(float puertasCada, float diametroPuerta)
    {
        int cantPuertas = 1;
        if (LargoTotal > puertasCada)
        {
            cantPuertas = Mathf.FloorToInt(LargoTotal / puertasCada);
            if (Random.value < (LargoTotal % puertasCada) / puertasCada)
            {
                cantPuertas++;
            }
        }

        var lineas = new List<Linea>(detalles.SelectMany(detalle => detalle.lineas).Where(linea => linea.Largo > diametroPuerta));
        // UnityEngine.Assertions.Assert.AreNotEqual(0, lineas.Count);

        for (int c = 0; c < cantPuertas; c++)
        {

            var espacio = lineas.Sum(l => l.Largo);
            espacio = Random.Range(0f, espacio);
            for (int i = 0; i < lineas.Count; i++)
            {
                if (espacio > lineas[i].Largo) espacio -= lineas[i].Largo;
                else
                {

                    espacio = Mathf.Min(espacio, lineas[i].Largo - diametroPuerta);
                    // TODO
                    // Hacer Algo para evitar overlapping (duplicar lineas cortadas, eliminar lineas muy cortas)
                    puertas.Add(lineas[i].MoveTowardsAB(espacio+diametroPuerta/2f));
                    break;

                }
            }

        }
    }

    void IdentificarEjesDirectos()
    {
        foreach (var cuartoX in seccionX.cuartosPropios)
        {
            foreach (var cuartoY in seccionY.cuartosPropios)
            {
                if (cuartoX.pegadosFiltrados.Contains(cuartoY))
                {
                    // UnityEngine.Assertions.Assert.IsTrue(cuartoY.pegadosFiltrados.Contains(cuartoX));
                    detalles.Add(new DetalleDeVinculo(cuartoX, cuartoY));
                }
            }
        }
    }

    void IdentificarEjesIndirecto() { }
}
