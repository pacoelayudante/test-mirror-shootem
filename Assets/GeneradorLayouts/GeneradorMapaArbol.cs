using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

//Este Generador de Arbol debe tambien generar (objetos nuevos) que son la conexion entre dos Secciones
//Indican las areas de conexion (paredes podsibles de poner una puerta)
// Tambien con ello podra tener el "nivel de acceso"
public class GeneradorMapaArbol : MonoBehaviour, ISerializationCallbackReceiver
{
    public GeneradorLayouts origen;
    public float OverlapMinimoParaConectar {
        get=>origen?origen.overlapMinimoParaConectar:1f;
    }
    public float puertasCada = 15f;

    Dictionary<LayoutCuarto, SeccionDeLayout> arbol = new Dictionary<LayoutCuarto, SeccionDeLayout>();
    public List<SeccionDeLayout> nodos = new List<SeccionDeLayout>();
    public List<VinculoEntreSecciones> vinculos = new List<VinculoEntreSecciones>();

    public SeccionDeLayout this[LayoutCuarto key]
    {
        get { return arbol.ContainsKey(key) ? arbol[key] : null; }
        set
        {
            if (arbol.ContainsKey(key)) arbol[key] = value;
            else arbol.Add(key, value);
        }
    }

    public void Add(LayoutCuarto key, SeccionDeLayout val)
    {
        arbol.Add(key, val);
    }

    SeccionDeLayout NuevoNodo(LayoutCuarto cuarto)
    {
        var nuevaSeccion = new GameObject("nodo" + nodos.Count).AddComponent<SeccionDeLayout>();
        nuevaSeccion.Iniciar(cuarto, this);
        nuevaSeccion.name += "" + nuevaSeccion.categoria;
        nuevaSeccion.transform.parent = transform;
        return nuevaSeccion;
    }

    public void Clear()
    {
        foreach (var seccion in nodos)
        {
            if (Application.isPlaying) Destroy(seccion.gameObject);
#if UNITY_EDITOR
            else DestroyImmediate(seccion.gameObject);
#endif
        }
        nodos.Clear();
        arbol.Clear();
    }

    public void OnBeforeSerialize() {        
    }
    public void OnAfterDeserialize()
    {
        EditorApplication.delayCall += () =>
        {
            arbol.Clear();
            foreach (var seccion in nodos)
            {
                seccion.ClearVinculos();
                foreach (var cuarto in seccion.cuartosPropios)
                {
                    arbol.Add(cuarto, seccion);
                }
            }
            foreach(var vinculo in vinculos){
                vinculo.AddVinculoCadaSeccion();
            }
        };
    }

    public void Generar()
    {
        Clear();
        if (origen && origen.generados != null)
        {
            Generar(origen.generados);
        }
    }
    public void Generar(List<LayoutCuarto> cuartos)
    {
        foreach (var cuarto in cuartos)
        {
            if (!arbol.ContainsKey(cuarto))
            {
                nodos.Add(NuevoNodo(cuarto));
            }
        }

        vinculos.Clear();
        List<VinculoEntreSecciones> vinculosConDuplicados = new List<VinculoEntreSecciones>();
        foreach (var nodo in nodos)
        {
            nodo.BuscarVecinos();

            foreach (var vecino in nodo.vecinos)
            {
                var nuevoVinculo = new VinculoEntreSecciones(nodo, vecino);
                vinculosConDuplicados.Add(nuevoVinculo);
            }
        }
        vinculos.AddRange(vinculosConDuplicados.Distinct(VinculoEntreSecciones.Comparador));
        foreach (var vinculo in vinculos)
        {
            vinculo.IdentificarEjes();
        }

    }

#if UNITY_EDITOR
    [CustomEditor(typeof(GeneradorMapaArbol))]
    public class GeneradorArbolEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var gen = target as GeneradorMapaArbol;
            GUILayout.Label("Count " + gen.arbol.Count);
            GUILayout.Label("Uniones Directas " + gen.nodos.Sum(nodo => nodo.vecinos.Count) / 2+" ("+gen.vinculos.Sum(v=>v.puertas.Count)+")");
            GUILayout.Label("Uniones Indirectas " + gen.nodos.Sum(nodo => nodo.vecinosIndirectos.Count) / 2);
            if (GUILayout.Button("Generar"))
            {
                gen.Generar();
            }
        }

        protected void OnSceneGUI()
        {
            var gen = target as GeneradorMapaArbol;
            SceneGUIPorGen(gen);
        }
        public static void SceneGUIPorGen(GeneradorMapaArbol gen)
        {
            if (!gen.origen) return;
            int controlId = GUIUtility.GetControlID(FocusType.Passive);
            if (Event.current.type != EventType.MouseDown || Event.current.button != 0) return;
            var mouse = Event.current.mousePosition;
            var layer = gen.origen ? gen.origen.layerGenerador : 31;

            foreach (var cuarto in gen.origen.generados)
            {
                cuarto.Simulated = true;
            }

            foreach (var hit in Physics2D.GetRayIntersectionAll(UnityEditor.HandleUtility.GUIPointToWorldRay(mouse), Mathf.Infinity, 1 << layer))
            {
                if (hit)
                {
                    var cuarto = hit.collider.GetComponent<LayoutCuarto>();
                    if (cuarto && gen.arbol.ContainsKey(cuarto))
                    {
                        GUIUtility.hotControl = controlId;
                        Event.current.Use();
                        Selection.activeGameObject = gen.arbol[cuarto].gameObject;
                        // DrawNodoHandle(gen.arbol[cuarto],Color.cyan);
                        // foreach(var vecino in gen.arbol[cuarto].vecinos){
                        //     DrawNodoHandle(vecino,Color.yellow,0.99f);
                        //     Handles.DrawLine(gen.arbol[cuarto].Centro,vecino.Centro);
                        // }
                        // cuarto.OnDrawGizmosSelected();
                        break;
                    }
                }
            }

            foreach (var cuarto in gen.origen.generados)
            {
                cuarto.Simulated = false;
            }
        }
    }
#endif
}
