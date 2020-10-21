using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Guazu.DrawersCopados;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu]
public class GenSeccionCuartosRampas : GenSeccionCuartos {
    [Range(0f,0.5f)]
    public float margenRampaPared = .2f;
    public float anguloDeRampa = 45f;

    List<Piso> pisos = new List<Piso>();

    class Piso {
        public bool solido = false;
        public float altura = 0f;
        public List<Vector2> segmentos = new List<Vector2>(new []{new Vector2(0f,1f)});
    }

    public override bool Generar(SeccionDeLayout seccion)
    {
        var primeraPasada = base.Generar(seccion);
        if (!primeraPasada) return false;

        var diamPuertas = seccion.arbol.OverlapMinimoParaConectar;
        
        var bouds = new Bounds();
        bouds.SetMinMax( new Vector3(
            seccion.cuartosPropios.Min(cuarto=>cuarto.Offset.x-cuarto.Size.x/2f),
            seccion.cuartosPropios.Min(cuarto=>cuarto.Offset.y-cuarto.Size.y/2f),0f ),
            new Vector3(
            seccion.cuartosPropios.Max(cuarto=>cuarto.Offset.x+cuarto.Size.x/2f),
            seccion.cuartosPropios.Max(cuarto=>cuarto.Offset.y+cuarto.Size.y/2f),0f )
        );

        // seccion.cuartosPropios.OrderBy(cuarto=>cuarto.)
        // pisos.AddRange(
        // seccion.cuartosPropios.Select(cuarto=>new Piso(){
        //     altura = cuarto.Offset.y-cuarto.Size.y/2f,
        //     segmentos = new List<Vector2>(new []{new Vector2(bouds.min.x,bouds.max.x)}),
        // }).Concat(
        // seccion.Puertas.Select(puerta=>new Piso(){
        //     altura = puerta.y-diamPuertas/2f,
        //     segmentos = new List<Vector2>(new []{new Vector2(bouds.min.x,bouds.max.x)}),
        // }).Concat(
        //     subCuartos.
        // ).ToArray());

        return primeraPasada;
    }

#if UNITY_EDITOR
    public override void OnDrawGizmosSelected() {
        // base.OnDrawGizmosSelected();
    }
    public override void OnSceneGUIGenerador()
    {
        base.OnSceneGUIGenerador();

        var paredes = new List<Vector2[]>();

        // Handles.color = Color.yellow;
        Handles.matrix = cuartoAfectado.transform.localToWorldMatrix;
        var tamMin = umbralTam*Mathf.Min(1f-porcentajeTamCorte[0],porcentajeTamCorte[1]);
        var margen = Vector2.right*tamMin*margenRampaPared;
        foreach(var subcuarto in subCuartos) {
            paredes.Add( new[]{(subcuarto.min+margen),(Vector2.up*subcuarto.size.y)} );
            paredes.Add( new[]{(subcuarto.max-margen),(-Vector2.up*subcuarto.size.y)} );
            // paredes.Add( new[]{(subcuarto.meta.min),(subcuarto.meta.max)} );
            // Handles.DrawWireDisc(subcuarto.meta.max,Vector3.forward,.3f);
            paredes.Add(new[]{(subcuarto.min+Vector2.up*margen.x),(Vector2.right*subcuarto.size.x)});
        }

        // Handles.matrix = seccionActual.transform.localToWorldMatrix;
        foreach(var p in paredes) {
            Handles.color = p[1].y==0?Color.yellow : (p[1].y>0?Color.magenta:Color.cyan);
            Handles.DrawLine(p[0],p[0]+p[1]);
            // Handles.DrawWireDisc(p[0],Vector3.forward,.3f);
        }

        foreach(var piso in pisos) {
            foreach(var segmento in piso.segmentos){
                // Handles.DrawLine(new Vector3(segmento[0],piso.altura,0f),new Vector3(segmento[1],piso.altura,0f));
            }
        }
    }
#endif
}