using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

public abstract class GeneradorSeccionDeLayout : ScriptableObject
{
    public SeccionDeLayout seccionActual { get; protected set; }
    public virtual float SeccionUsable(SeccionDeLayout seccion) { return 1f; }
    public abstract bool Generar(SeccionDeLayout seccion);
    public abstract void Clear();
    public bool ReGenerar(SeccionDeLayout seccion) {
        Clear();
        return Generar(seccion);
    }
    public virtual void OnDrawGizmos() { }
    public virtual void OnDrawGizmosSelected() { }
    public virtual void InspectorGUIGenerador() { }
    public virtual void OnSceneGUIGenerador() { }
}
public class SeccionDeLayout : MonoBehaviour
{
    public GeneradorSeccionDeLayout generadorReferencia;

    public LayoutCuarto.Categoria categoria;
    public GeneradorMapaArbol arbol = null;
    public GameObject objColliders;

    GeneradorSeccionDeLayout generadorActual;
    public List<LayoutCuarto> cuartosPropios = new List<LayoutCuarto>();
    public List<SeccionDeLayout> vecinos = new List<SeccionDeLayout>();
    public List<SeccionDeLayout> vecinosIndirectos = new List<SeccionDeLayout>();

    List<VinculoEntreSecciones> vinculoEntreSecciones = new List<VinculoEntreSecciones>();
    public void AddVinculo(VinculoEntreSecciones vinculoNuevo)
    {
        UnityEngine.Assertions.Assert.IsTrue(vinculoNuevo.Contains(this));
        if (!vinculoEntreSecciones.Contains(vinculoNuevo)) vinculoEntreSecciones.Add(vinculoNuevo);
    }
    public void ClearVinculos()
    {
        vinculoEntreSecciones.Clear();
    }
    public IEnumerable<Vector2> Puertas
    {
        get
        {
            return vinculoEntreSecciones.SelectMany(x => x.puertas);
        }
    }

    public int Count { get { return cuartosPropios.Count; } }

    public LayoutCuarto this[int index] { get { return Count == 0 ? null : cuartosPropios[(index % Count + Count) % Count]; } }

    public Vector3 Centro
    {
        get
        {
            var centro = Vector3.zero;
            foreach (var cuarto in cuartosPropios)
            {
                if (!cuarto) continue;
                centro += cuarto.transform.TransformPoint(cuarto.BoxCol.offset);
            }
            centro /= cuartosPropios.Count;
            return centro;
        }
    }

    public void Iniciar(LayoutCuarto inicial, GeneradorMapaArbol arbol)
    {
        this.arbol = arbol;
        categoria = inicial.categoria;
        cuartosPropios.Add(inicial);
        arbol.Add(inicial, this);
        if (inicial.categoria == LayoutCuarto.Categoria.Peque)
        {
            AddRecursivo(inicial.pegadosFiltrados, arbol);
        }

        transform.position = Centro + Vector3.forward * transform.position.z;
    }
    void AddRecursivo(List<LayoutCuarto> grupoInput, GeneradorMapaArbol arbol)
    {
        foreach (var cuarto in grupoInput)
        {
            if (cuarto.categoria == LayoutCuarto.Categoria.Peque)
            {
                if (!cuartosPropios.Contains(cuarto))
                {
                    cuartosPropios.Add(cuarto);
                    arbol.Add(cuarto, this);
                    AddRecursivo(cuarto.pegadosFiltrados, arbol);
                }
            }
        }
    }

    public void BuscarVecinos()
    {
        vecinos.Clear();
        vecinosIndirectos.Clear();
        vinculoEntreSecciones.Clear();
        foreach (var cuarto in cuartosPropios)
        {
            foreach (var pegado in cuarto.pegadosFiltrados)
            {
                if (!cuartosPropios.Contains(pegado) && !vecinos.Contains(arbol[pegado]))
                {
                    vecinos.Add(arbol[pegado]);
                }
            }
        }
        foreach (var propio in cuartosPropios)
        {
            if (!propio || !arbol) continue;
            foreach (var tocadoPoco in propio.pegadosTocandoPoco)
            {
                var posibleVecino = arbol[tocadoPoco];
                if (!cuartosPropios.Contains(tocadoPoco) && !vecinosIndirectos.Contains(posibleVecino) && !vecinos.Contains(posibleVecino)) vecinosIndirectos.Add(arbol[tocadoPoco]);
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (generadorActual) generadorActual.OnDrawGizmos();
    }
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        var amarilloHalf = Color.yellow;
        amarilloHalf.a *= .1f;
        DrawNodoHandle(amarilloHalf);

        Gizmos.color = Color.yellow;
        DrawMuroGizmos();

        var cyanHalf = Color.cyan;
        cyanHalf.a *= .5f;
        foreach (var vecino in this.vecinos)
        {
            if (!vecino) continue;
            vecino.DrawNodoHandle(Color.cyan, 0.99f);
            Handles.color = cyanHalf;
            Handles.DrawLine(this.Centro, vecino.Centro);
        }
        Handles.color = Color.yellow;
        foreach (var indirecto in vecinosIndirectos)
        {
            Handles.DrawLine(this.Centro, indirecto.Centro);
        }

        // foreach (var cuarto in cuartosPropios)
        // {
        //     if (cuarto) cuarto.OnDrawGizmosSelected();
        // }
        Gizmos.color = Color.green * 0.75f;
        if (generadorActual) generadorActual.OnDrawGizmosSelected();
        DrawGizmosVinculos();
    }

    void DrawGizmosVinculos()
    {
        Handles.color = Color.green;
        foreach (var vinculo in vinculoEntreSecciones)
        {
            vinculo.DrawGizmos();
        }
    }

    void DrawMuroGizmos()
    {
        foreach (var cuarto in cuartosPropios)
        {
            if (!cuarto) continue;
            Gizmos.matrix = cuarto.transform.localToWorldMatrix;
            foreach (var muro in cuarto.muros)
            {
                muro.OnDrawGizmosSelected();
            }
        }
    }

    public void DrawNodoHandle(Color col, float esc = 1f)
    {
        Handles.color = col;
        foreach (var cuarto in this.cuartosPropios)
        {
            if (!cuarto) continue;
            Handles.matrix = cuarto.transform.localToWorldMatrix;
            Handles.DrawWireCube(cuarto.BoxCol.offset, cuarto.BoxCol.size * esc);
        }
        Handles.matrix = Matrix4x4.identity;
    }
#endif

    void ActualizarVinculos()
    {
        foreach (var vinculo in vinculoEntreSecciones)
        {
            vinculo.IdentificarEjes();
        }
    }

    [ContextMenu("Generar")]
    public bool Generar()
    {
        if (generadorActual)
        {
            if (Application.isPlaying) Destroy(generadorActual);
#if UNITY_EDITOR
            else DestroyImmediate(generadorActual);
#endif
        }
        generadorActual = null;
        if (generadorReferencia)
        {
            generadorActual = Instantiate(generadorReferencia);
            return generadorActual.Generar(this);
        }
        else return false;
    }

#if UNITY_EDITOR
    [CanEditMultipleObjects]
    [CustomEditor(typeof(SeccionDeLayout))]
    public class SeccionDeLayoutEditor : Editor
    {
        List<GeneradorMapaArbol> arbolesMostrados = new List<GeneradorMapaArbol>();
        List<SeccionDeLayout> seccionMostradora = new List<SeccionDeLayout>();
        static Editor editorGen;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (GUILayout.Button("Actualizar Vinculos"))
            {
                foreach (SeccionDeLayout seccion in targets) seccion.ActualizarVinculos();
            }
            if (GUILayout.Button("Generar"))
            {
                foreach (SeccionDeLayout seccion in targets) seccion.Generar();
            }
            arbolesMostrados.Clear();
            seccionMostradora.Clear();
            foreach (SeccionDeLayout secc in targets)
            {
                if (secc.arbol && !arbolesMostrados.Contains(secc.arbol))
                {
                    arbolesMostrados.Add(secc.arbol);
                    seccionMostradora.Add(secc);
                }

                GUILayout.Label("vinculos " + secc.vinculoEntreSecciones.Count);
                foreach (var vinculo in secc.vinculoEntreSecciones)
                {
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button(vinculo.Label, "Label", GUILayout.ExpandWidth(false)))
                    {
                        EditorGUIUtility.PingObject(vinculo.Otro(secc));
                    }
                    float tot = 0f;
                    foreach (var detalle in vinculo.detalles)
                    {
                        var largo = detalle.lineas.Sum(cada => cada.Largo);
                        GUILayout.Label("+" + largo.ToString("0.00"), GUILayout.ExpandWidth(false), GUILayout.MinWidth(0));
                        tot += largo;
                    }
                    GUILayout.Label("= " + tot.ToString("0.00"), GUILayout.ExpandWidth(false));
                    EditorGUILayout.EndHorizontal();
                }

                // if(secc.generadorActual){
                //     secc.generadorActual.InspectorGUIGenerador();
                // }
            }
            EditorGenerador();
        }

        void EditorGenerador()
        {
            if (targets.Length == 1)
            {
                var seccion = target as SeccionDeLayout;
                if (seccion.generadorActual)
                {
                    if (!editorGen) Editor.CreateCachedEditor(seccion.generadorActual, null, ref editorGen);
                    else if (editorGen.target != seccion.generadorActual)
                    {
                        Editor.CreateCachedEditor(seccion.generadorActual, null, ref editorGen);
                    }
                    if (editorGen) editorGen.OnInspectorGUI();
                    seccion.generadorActual.InspectorGUIGenerador();
                }
            }
        }

        public void OnSceneGUI()
        {
            var seccion = target as SeccionDeLayout;
            if (seccion.arbol && seccionMostradora.Contains(seccion))
            {
                GeneradorMapaArbol.GeneradorArbolEditor.SceneGUIPorGen(seccion.arbol);
            }
            if (seccion.generadorActual)
            {
                seccion.generadorActual.OnSceneGUIGenerador();
            }
        }
    }
#endif
}
