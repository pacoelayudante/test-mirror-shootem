using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Guazu.DrawersCopados;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GeneradorLayouts : MonoBehaviour
{
    [SingleLayer]
    public int layerGenerador, layerColliderEdges, layerColliderPasables;
    public Sprite spriteReferencia;
    public int cantGrandes = 7, cantMinis = 50;

    public Vector2 tamMinMaxGrandes = new Vector2(7, 20);
    public Vector2 tamMinMaxMinis = new Vector2(1.5f, 4);

public bool atractorPrendido = true;
[Ocultador("atractorPrendido")]
public PointEffector2D objetoAtractor;
    public bool pequesConJoint = true;
    public bool grandesConJoint = true;
    [Ocultador("pequesConJoint")]
    public float pequeJointFrec = 1f;
    [Ocultador("grandesConJoint")]
    public float grandeJointFrec = 1f;
    public bool autoMasaGrandes = true, autoMasaPeques = true;
    [Ocultador("autoMasaGrandes", true)]//negrar valor, se ve cuando esta false
    public float masaGrandes = 1f;
    [Ocultador("autoMasaPeques", true)]//negrar valor, se ve cuando esta false
    public float masaPeques = .1f;
    [Ocultador("autoMasaGrandes")]
    public float densidadGrandes = 1f;
    [Ocultador("autoMasaPeques")]
    public float densidadPeques = 1f;
    public PhysicsMaterial2D fisicMatGrande, fisicMatChico;

    public float tSimular = .01f;
    public int pasosSimular = 3000;
    public float tamCajaMuroMin = 1f;
    public float grosorMuros = 0f;
    public float overlapMinimoParaConectar = 1f;
    // Si el grosor de los muros es "0" los renderiza 
    // como Edges2D, en cambio, si es (> 0f)
    //entonces los renderiza como Box2D
    public BoxCollider2D prefabCaja;
    public float TamMinMuro { get { return Physics2D.defaultContactOffset * 3f; } }

    public List<LayoutCuarto> generados = new List<LayoutCuarto>();

    public bool ejecutarOnStart = false;
    public GameObject parentLayout, edgesObj;

    private void Start()
    {
        if (ejecutarOnStart)
        {
            Generar(true, false, true);
            Simular();
            Generar(false, true, false);
            Simular();
            RenderEdgeColliders();
        }
    }

    [ContextMenu("Generar")]
    public void Generar()
    {
        Generar(true, true, true);
    }
    public void Generar(bool clear, bool peques, bool grandes)
    {
        if (clear)
        {
            if (parentLayout)
            {
                if (Application.isPlaying) Destroy(parentLayout);
#if UNITY_EDITOR
                else DestroyImmediate(parentLayout);
#endif
            }
            parentLayout = null;

            generados.Clear();

        }
        if (!parentLayout && (peques || grandes))
        {
            parentLayout = new GameObject("parent layout");
            parentLayout.transform.position = transform.position;
        }

        for (var i = 0; i < (grandes ? cantGrandes : 0) + (peques ? cantMinis : 0); i++)
        {
            bool esGrande = i < cantGrandes && grandes;
            Vector2 tam = esGrande ? tamMinMaxGrandes : tamMinMaxMinis;
            tam.Set(Random.Range(tam[0], tam[1]), Random.Range(tam[0], tam[1]));

            var niu = new GameObject("layout " + (esGrande ? "grande" : "peque"));
            niu.transform.parent = parentLayout.transform;
            niu.transform.position = transform.position + (Vector3)Random.insideUnitCircle * tamMinMaxGrandes.magnitude / 2f;
            // niu.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor | HideFlags.;
            niu.layer = layerGenerador;
            if (spriteReferencia)
            {
                var srend = niu.AddComponent<SpriteRenderer>();
                var col = esGrande ? Color.white : Color.black;
                col.a = .2f;
                srend.color = col;
                srend.sprite = spriteReferencia;
                srend.drawMode = SpriteDrawMode.Sliced;
                srend.size = tam;
            }
            var collid = niu.AddComponent<BoxCollider2D>();
            collid.size = tam;
            var rigid = niu.AddComponent<Rigidbody2D>();
            rigid.simulated = false;
            rigid.freezeRotation = true;
            rigid.gravityScale = 0f;
            rigid.sharedMaterial = esGrande ? fisicMatGrande : fisicMatChico;
            rigid.useAutoMass = esGrande ? autoMasaGrandes : autoMasaPeques;
            if (rigid.useAutoMass)
            {
                collid.density = esGrande ? densidadGrandes : densidadPeques;
            }
            else
            {
                rigid.mass = esGrande ? masaGrandes : masaPeques;
            }
            if (esGrande & grandesConJoint || !esGrande & pequesConJoint)
            {
                var spring = niu.AddComponent<SpringJoint2D>();
                spring.connectedAnchor = transform.position;
                spring.frequency = esGrande ? grandeJointFrec : pequeJointFrec;
                spring.autoConfigureDistance = false;
            }

            var laycuarto = niu.AddComponent<LayoutCuarto>();
            laycuarto.categoria = esGrande ? LayoutCuarto.Categoria.Grande : LayoutCuarto.Categoria.Peque;
            generados.Add(laycuarto);
        }

    }

    [ContextMenu("Simular")]
    void Simular()
    {
        Simular(tSimular, pasosSimular);
    }

    void ClearEdgesColliders()
    {
        if (edgesObj)
        {
            if (Application.isPlaying) Destroy(edgesObj);
#if UNITY_EDITOR
            else DestroyImmediate(edgesObj);
#endif
        }
        edgesObj = null;
    }

    [ContextMenu("RenderEdges")]
    void RenderEdgeColliders()
    {
        ClearEdgesColliders();
        if (generados.Count > 0)
        {
            edgesObj = new GameObject("edges");
            edgesObj.transform.position = transform.position;
            edgesObj.layer = layerColliderEdges;
        }

        foreach (var lc in generados)
        {
            if (lc)
            {

                var obj = lc.RenderEdgeColliders(TamMinMuro, tamCajaMuroMin, grosorMuros, prefabCaja);
                obj.transform.parent = edgesObj.transform;
                obj.layer = edgesObj.layer;

                if (layerColliderPasables != 0)
                {
                    var pasable = new GameObject("pasable");
                    pasable.transform.parent = obj.transform;
                    pasable.transform.localPosition = Vector2.zero;
                    var boxcol = pasable.AddComponent<BoxCollider2D>();
                    boxcol.size = lc.Size;
                    pasable.layer = layerColliderPasables;
                }

            }
        }
    }

    void Simular(float t, int repeticiones = 1, bool noprogbar = false)
    {
        var original = Physics2D.autoSimulation;
        Physics2D.autoSimulation = false;

        if(objetoAtractor) objetoAtractor.gameObject.SetActive(atractorPrendido);

        var rigidsParaReactivar = new List<Rigidbody2D>();
        foreach (var rigid in FindObjectsOfType<Rigidbody2D>())
        {
            if (rigid.simulated)
            {
                rigidsParaReactivar.Add(rigid);
                rigid.simulated = false;
            }
        }
        foreach (var laycuarto in generados)
        {
            if (laycuarto) laycuarto.Simulated = true;
        }

#if UNITY_EDITOR
        if (repeticiones > 1 && !noprogbar) EditorUtility.DisplayProgressBar("Simulando", "", 0);
#endif
        for (int i = 0; i < repeticiones; i++)
        {
#if UNITY_EDITOR
            if (repeticiones > 1 && !noprogbar && i % (repeticiones / 10f) == 0f)
            {
                EditorUtility.DisplayProgressBar("Simulando", i + "/" + repeticiones, i / (repeticiones - 1f));
            }
#endif
            Physics2D.Simulate(t);
        }
#if UNITY_EDITOR
        if (repeticiones > 1 && !noprogbar) EditorUtility.ClearProgressBar();
#endif

        foreach (var laycuarto in generados)
        {
            if (laycuarto)
            {
                laycuarto.RevisarPegados(overlapMinimoParaConectar);
            }
        }

        Physics2D.autoSimulation = original;
        foreach (var laycuarto in generados)
        {
            if (laycuarto) laycuarto.Simulated = false;
        }
        foreach (var rigid in rigidsParaReactivar)
        {
            if (rigid) rigid.simulated = true;
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(GeneradorLayouts))]
    public class GeneradorLayoutsEditor : Editor
    {
        GeneradorLayouts genlay;
        bool simular;
        public bool Simular
        {
            get { return simular; }
            set
            {
                simular = value;
                EditorApplication.update -= SimularEditor;
                if (simular) EditorApplication.update += SimularEditor;
            }
        }
        int pasosSimular = 10;

        private void OnEnable()
        {
            genlay = target as GeneradorLayouts;
        }
        private void OnDisable()
        {
            Simular = false;
        }

        void SimularEditor()
        {
            if (pasosSimular < 2) pasosSimular = 2;
            if (genlay) genlay.Simular(genlay.tSimular, pasosSimular, true);
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (GUILayout.Button("Clear")) genlay.Generar(true, false, false);
            if (GUILayout.Button("Generar Peques")) genlay.Generar(false, true, false);
            if (GUILayout.Button("Generar Grandes")) genlay.Generar(false, false, true);
            if (GUILayout.Button("Simular " + genlay.pasosSimular)) genlay.Simular();
            if (GUILayout.Button("Secuencia Todo Junto"))
            {
                genlay.Generar(true, true, true);
                genlay.Simular();
            }
            if (GUILayout.Button("Secuencia Peques Luego Grandes"))
            {
                genlay.Generar(true, true, false);
                genlay.Simular();
                genlay.Generar(false, false, true);
                genlay.Simular();
            }
            if (GUILayout.Button("Secuencia Grandes Luego Peques"))
            {
                genlay.Generar(true, false, true);
                genlay.Simular();
                genlay.Generar(false, true, false);
                genlay.Simular();
            }

            if (GUILayout.Button("Clear Edges")) genlay.ClearEdgesColliders();
            if (GUILayout.Button("RenderColliders"))
            {
                genlay.RenderEdgeColliders();
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            var sim = EditorGUILayout.Toggle("Simular Continuo", Simular);
            if (EditorGUI.EndChangeCheck())
            {
                Simular = sim;
            }
            pasosSimular = EditorGUILayout.IntField("Cant Pasos", pasosSimular);
            EditorGUILayout.EndHorizontal();
        }
    }
#endif
}
