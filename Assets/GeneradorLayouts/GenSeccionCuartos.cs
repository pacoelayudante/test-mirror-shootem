using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Guazu.DrawersCopados;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu]
public class GenSeccionCuartos : GeneradorSeccionDeLayout
{
    public Vector2 porcentajeTamCorte = new Vector2(0.5f, 0.8f);
    public int maxCantCortesHard = 20;
    public float umbralTam = 1.5f;
    public float ultimoSubcuartoReconecta = .1f;
    public float grosorParedesBase = .2f;
    public bool pasillos = false;
    public bool ramificar = true;
    [Ocultador("ramificar")]
    public float factorUmbralRamificar = 4f;
    public Vector2 ejeMayorDesenfoque = Vector2.one;
    public LayoutCuarto cuartoAfectado;
    public GameObject ObjColliders
    {
        get => seccionActual?.objColliders;
        set => seccionActual.objColliders = value;
    }

    public override float SeccionUsable(SeccionDeLayout seccion)
    {
        if (seccion.Count == 1)
        {
            if (seccion[0] && seccion[0].categoria == LayoutCuarto.Categoria.Grande)
            {
                return 1f;
            }
            else
            {
                return 0f;
            }
        }
        else
        {
            return 0;
        }
    }

    [SerializeField]
    protected List<SubCuarto> subCuartos = new List<SubCuarto>();

    public override void Clear(){
        subCuartos.Clear();
        ClearColliders();
    }
    public override bool Generar(SeccionDeLayout seccion)
    {
        if (!seccion || seccion.Count == 0) return false;
#if UNITY_EDITOR
        UnityEngine.Assertions.Assert.IsTrue(string.IsNullOrEmpty(UnityEditor.AssetDatabase.GetAssetPath(this)));
        if (!string.IsNullOrEmpty(UnityEditor.AssetDatabase.GetAssetPath(this))) return false;
#endif
        this.seccionActual = seccion;
        cuartoAfectado = seccion[0];
        if (cuartoAfectado)
        {

            var ultimoRect = new Rect(-cuartoAfectado.Size / 2f, cuartoAfectado.Size);
            int cant = RecorteRectRecursivo(ultimoRect);
            // falta limitar de alguna forma la cantidad de cortes (por cant en vez de ancho? o sacar esa opcion y ya)
        }

        // ProcesarRects();
        // GenerarCollidersBasicos();

        return true;
    }

    protected int RecorteRectRecursivo(Rect input, AbreHacia abreHeredado = AbreHacia.NoAbre)
    {
        return RecorteRectRecursivo(input, input, abreHeredado);
    }
    protected int RecorteRectRecursivo(Rect input, Rect meta, AbreHacia abreHeredado = AbreHacia.NoAbre)
    {
        int cant = 0;
        var pos = input.position;
        var tam = input.size;
        // aca el desenfoque lo que hace es darle mas peso a un eje que al otro basicamente
        // de esta manera uno de los ejes puede ser considerado mayor que el otro aunque sea menor pero no por mucho
        var ejeMayor = tam.x * ejeMayorDesenfoque.x >= tam.y * ejeMayorDesenfoque.y ? 0 : 1;
        var signoEje = Random.value > .5f ? 1 : -1;

        // vamos cortando hasta tener un cuarto que sea mas pequeño que el umbral de tam
        // pero no nos olvidemos de agregar el ultimo rect!
        if (tam[ejeMayor] * (1f - porcentajeTamCorte[1]) <= umbralTam)
        {
            subCuartos.Add(new SubCuarto(input, abreHeredado));
            return cant + 1;
        }

        var valCorte = tam[ejeMayor] * Random.Range(porcentajeTamCorte[0], porcentajeTamCorte[1]);
        var valSobrante = tam[ejeMayor] - valCorte;
        var offsetCorte = valSobrante;
        var offsetSobrante = valCorte;

        var oldTam = tam;
        var oldPos = pos;

        tam[ejeMayor] = valCorte;
        if (signoEje > 0) pos[ejeMayor] += signoEje * offsetCorte;
        cant += RecorteRectRecursivo(new Rect(pos, tam));

        oldTam[ejeMayor] = valSobrante;
        if (signoEje < 0) oldPos[ejeMayor] -= signoEje * offsetSobrante;
        input.Set(oldPos.x, oldPos.y, oldTam.x, oldTam.y);
        if (abreHeredado == AbreHacia.NoAbre) abreHeredado = (AbreHacia)((ejeMayor + 1) * signoEje);

        if (pasillos && valSobrante / 2f > umbralTam)
        {
            if (signoEje > 0) oldPos[ejeMayor] += oldTam[ejeMayor] - umbralTam;
            else
            {
                var ipos = input.position;
                ipos[ejeMayor] += umbralTam;
                input.position = ipos;
            }
            var itam = input.size;
            itam[ejeMayor] -= umbralTam;
            input.size = itam;

            oldTam[ejeMayor] = umbralTam;
            var pasilloRect = new Rect(oldPos, oldTam);
            subCuartos.Add(new SubCuarto(pasilloRect, abreHeredado));
            subCuartos[subCuartos.Count - 1].meta = pasilloRect;
        }

        if (ramificar && Mathf.Min(input.width, input.height) > umbralTam * factorUmbralRamificar) cant += RecorteRectRecursivo(input, meta, abreHeredado);
        else
        {
            cant += 1;
            subCuartos.Add(new SubCuarto(input, abreHeredado));
            subCuartos[subCuartos.Count - 1].meta = meta;
        }

        // tam[ejeMayor] = valCorte;
        // if (signoEje > 0) pos[ejeMayor] += signoEje * offsetCorte;
        // input = new Rect(pos, tam);
        // cant += RecorteRectRecursivo(input);

        return cant;
    }

    void ProcesarRects()
    {
        // subCuartos.Reverse();
        // primero buscar conexiones
        foreach (var sub in subCuartos) sub.conectados.Clear();
        // Agrego al primero, luego, a medida que voy conectando, voy argegando a la lista de buscadores
        // asi cuando el presente termina de conectarse, salta al siguiente que haya sido conectado, y este intenta conectarse
        // ademas de ir agregando (resulta un estilo de propagacion en teoria)
        var buscaConexion = new List<SubCuarto>();
        buscaConexion.Add(subCuartos[0]);
        var cuartoDesconectados = new List<SubCuarto>(subCuartos);
        for (int busc = 0; busc < buscaConexion.Count; busc++)
        {
            var subBusca = buscaConexion[busc];
            // Todos los cuartos (de menor a mayor) traten de agarrar los cuartos conectados, que aun no hayan sido reclamados
            //esto deberia en teorica generar una arbol que se abre de mas grande a mas chico haciendo los camions mas cortos posibles
            // posiblemente generando espirales en algunso casos tambien
            cuartoDesconectados.Remove(subBusca);
            for (int i = cuartoDesconectados.Count - 1; i >= 0; i--)
            {
                if (subBusca.TocaConMargen(cuartoDesconectados[i], umbralTam / 2f))
                {
                    subBusca.ConectarCon(cuartoDesconectados[i]);
                    buscaConexion.Add(cuartoDesconectados[i]);
                    cuartoDesconectados.RemoveAt(i);
                }
            }
            if (cuartoDesconectados.Count == 0) break;
        }
        if (Random.value < ultimoSubcuartoReconecta)
        {
            var ultimoSub = subCuartos[subCuartos.Count - 1];
            var posibleConectado = subCuartos[Random.Range(0, subCuartos.Count)];
            if (ultimoSub != posibleConectado && !posibleConectado.conectados.Contains(ultimoSub) && ultimoSub.TocaConMargen(posibleConectado, umbralTam / 2f)) ultimoSub.ConectarCon(posibleConectado);
        }
    }
    void GenerarCollidersBasicos(bool debug = false)
    {
        ClearColliders();
        ObjColliders = new GameObject(name + "_objCollider");
        ObjColliders.transform.SetParent(seccionActual.transform, false);

        var tamPuerta = Vector3.one * ((seccionActual.arbol ? seccionActual.arbol.OverlapMinimoParaConectar : umbralTam / 2f) - grosorParedesBase * 2f);
        var puertas = seccionActual.Puertas;
        var puertasRects = puertas.Select(x => new Rect(ObjColliders.transform.InverseTransformPoint(x) - tamPuerta / 2f, tamPuerta));
        puertasRects = puertasRects.Concat(subCuartos.SelectMany(sub => sub.GenerarPuertasInternas(grosorParedesBase, umbralTam / 2f))).ToList();

        if (puertasRects.Count() == 0) puertasRects = null;

        if (debug)
        {
            foreach (var rectGen in puertasRects)
            {
                var col = ObjColliders.AddComponent<BoxCollider2D>();
                col.offset = rectGen.center;
                col.size = rectGen.size;
            }
        }
        else
        {
            foreach (var sub in subCuartos)
            {
                sub.GenerarColliders(ObjColliders, grosorParedesBase, puertasRects);
            }
        }
    }

    void OnDestroy()
    {
        ClearColliders();
    }
    void ClearColliders()
    {
        if (ObjColliders)
        {
            if (Application.isPlaying) Destroy(ObjColliders);
#if UNITY_EDITOR
            else DestroyImmediate(ObjColliders);
#endif
            ObjColliders = null;
        }
    }

    protected enum AbreHacia { Derecha = 1, Arriba = 2, Izquierda = -1, Abajo = -2, NoAbre = 0 }
    [System.Serializable]
    protected class SubCuarto
    {
        public Rect meta;
        Rect rect;
        public AbreHacia abreHacia;
        public Vector2 position { get => rect.position; }
        public Vector2 center { get => rect.center; }
        public Vector2 size { get => rect.size; }
        public Vector2 min { get => rect.min; }
        public Vector2 max { get => rect.max; }
        public static implicit operator Rect(SubCuarto yo)
        {
            return yo.rect;
        }

        public List<SubCuarto> conectados = new List<SubCuarto>();

        public SubCuarto(Rect rect, AbreHacia abreHacia)
        {
            this.rect = rect;
            this.abreHacia = abreHacia;
        }
        public bool Contains(Vector2 punto)
        {
            return this.rect.Contains(punto);
        }
        public bool Overlaps(Rect incomingRect)
        {
            return this.rect.Overlaps(incomingRect);
        }
        public bool TocaConMargen(Rect inRect, float margen)
        {
            var toca = TocaConChangui(inRect);
            if (!toca) return false;
            //esto es el overlap, en cada eje
            if (Mathf.Min(inRect.xMax, rect.xMax) - Mathf.Max(inRect.xMin, rect.xMin) > margen) return true;
            else if (Mathf.Min(inRect.yMax, rect.yMax) - Mathf.Max(inRect.yMin, rect.yMin) > margen) return true;
            else return false;
        }
        public bool TocaConChangui(Rect inRect)
        {
            return Toca(inRect, Physics2D.defaultContactOffset);
        }
        public bool Toca(Rect inRect, float changui = 0f)
        {
            //El changui da true cuando los valores son un toquecin (un changui) diferentes
            return (inRect.xMax >= rect.xMin - changui &&
                inRect.xMin <= rect.xMax + changui &&
                inRect.yMax >= rect.yMin - changui &&
                inRect.yMin <= rect.yMax + changui);
        }
        public void ConectarCon(SubCuarto otro)
        {
            conectados.Remove(otro);
            conectados.Add(otro);
            // if(!otro.conectados.Contains(this)) otro.ConectarCon(this);
        }

        public List<Rect> GenerarPuertasInternas(float grosorParedes = 0f, float tamPuertas = 1f)
        {
            var salida = new List<Rect>();
            
            bool arribaChico = false;
            bool abajoChico = false;

            foreach (var otro in conectados)
            {
                if (Mathf.Max(rect.yMin, otro.rect.yMin)+Physics2D.defaultContactOffset >= Mathf.Min(rect.yMax, otro.rect.yMax))
                {
                    //esta uno arriba del otro
                    float alto = Mathf.Min(rect.height, otro.rect.height) / 4f;
                    float y = Mathf.Max(rect.yMin, otro.rect.yMin) - alto / 2f;
                    float x = Mathf.Max(rect.xMin, otro.rect.xMin)+grosorParedes/2f;
                    float ancho = Mathf.Min(rect.xMax, otro.rect.xMax) - x - grosorParedes/2f;
                    if( (y>rect.center.y && abajoChico)||(y<rect.center.y && arribaChico) ){
                        //esta movida es para hacer que no haya dos huecos grandes arriba o abajo (si puede ser arriba Y abajo)
                        x += Random.Range( 0f,ancho-tamPuertas );
                        ancho = tamPuertas;
                    }
                    else{
                        if(y>rect.center.y)abajoChico=true;
                        else arribaChico = true;
                    }
                    salida.Add(new Rect(x, y, ancho, alto));
                }
                else
                {
                    float ancho = grosorParedes * 5f + Random.value;
                    float x = Mathf.Max(rect.xMin, otro.rect.xMin) - ancho / 2f;
                    float alto = tamPuertas;
                    float y = Mathf.Max(rect.yMin, otro.rect.yMin) + grosorParedes/2f;

                    salida.Add(new Rect(x, y, ancho, alto));

                }

            }

            return salida;
        }

        public void GenerarColliderDebug(GameObject portador, IEnumerable<Rect> puertas)
        {
            foreach (var rectGen in puertas)
            {
                var col = portador.AddComponent<BoxCollider2D>();
                col.offset = rectGen.center;
                col.size = rectGen.size;
            }
        }
        public void GenerarColliders(GameObject portador, float grosor, IEnumerable<Rect> puertas = null)
        {
            List<Rect> rectsGenerados = puertas == null ? null : new List<Rect>();
            abreHacia = AbreHacia.NoAbre;
            if (abreHacia != AbreHacia.Derecha) GenerarColliderUnLado(portador, grosor, 0, 1, rectsGenerados);
            if (abreHacia != AbreHacia.Arriba) GenerarColliderUnLado(portador, grosor, 1, 1, rectsGenerados);
            if (abreHacia != AbreHacia.Izquierda) GenerarColliderUnLado(portador, grosor, 0, -1, rectsGenerados);
            if (abreHacia != AbreHacia.Abajo) GenerarColliderUnLado(portador, grosor, 1, -1, rectsGenerados);

            if (rectsGenerados != null && rectsGenerados.Count > 0)
            {
                foreach (var puerta in puertas)
                {
                    var rectsProcesados = new List<Rect>();
                    foreach (var rectGen in rectsGenerados)
                    {
                        rectsProcesados.AddRange(GameUtils.RectSustraccion(rectGen, puerta));
                    }
                    rectsGenerados = rectsProcesados;
                }
                foreach (var rectGen in rectsGenerados)
                {
                    var col = portador.AddComponent<BoxCollider2D>();
                    col.offset = rectGen.center;
                    col.size = rectGen.size;
                }
            }
        }
        public void GenerarColliderUnLado(GameObject portador, float grosor, int eje, int signo, List<Rect> rectsGenerados = null)
        {
            var otroEje = (eje + 1) % 2;
            var offset = rect.center;
            var tam = rect.size;
            // offset[eje] += (tam[eje] - grosor*0f) * signo / 2f;
            offset[eje] += (tam[eje] ) * signo / 2f;
            tam[eje] = grosor;

            if (rectsGenerados == null)
            {
                var col = portador.AddComponent<BoxCollider2D>();
                col.offset = offset;
                col.size = tam;
            }
            else
            {
                rectsGenerados.Add(new Rect(offset - tam / 2f, tam));
            }
        }
    }

    public override void OnDrawGizmosSelected()
    {
        if (cuartoAfectado)
        {
            Gizmos.matrix = cuartoAfectado.transform.localToWorldMatrix;

            for (int i = 0; i < subCuartos.Count; i++)
            {
                var sub = subCuartos[i];
                Gizmos.DrawWireCube(sub.center, sub.size * .97f);

                foreach (var subsub in sub.conectados)
                {
                    Gizmos.DrawLine(sub.center, subsub.center);
                }
#if UNITY_EDITOR
                var estilo = new GUIStyle("Label");
                estilo.normal.textColor = Gizmos.color;
                UnityEditor.Handles.Label(cuartoAfectado.transform.TransformPoint(sub.center), i.ToString() + "_" + sub.abreHacia, estilo);
#endif
            }

            Gizmos.matrix = Matrix4x4.identity;
        }
    }

#if UNITY_EDITOR
    public override void InspectorGUIGenerador()
    {
        EditorGUI.BeginDisabledGroup(!seccionActual);
        if (GUILayout.Button("Re Generar Esto"))
        {
            ReGenerar(seccionActual);
        }
        if (GUILayout.Button("Re procesar Rects"))
        {
            ProcesarRects();
        }
        if (GUILayout.Button("DEBUG puertas"))
        {
            GenerarCollidersBasicos(true);
        }
        if (GUILayout.Button("Re gen colliders"))
        {
            GenerarCollidersBasicos();
        }
        EditorGUI.EndDisabledGroup();
    }
    public override void OnSceneGUIGenerador()
    {
        var worldMous = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition).origin;
        worldMous.z = 0f;
        Handles.color = Color.blue;
        var localMous = seccionActual.transform.InverseTransformPoint(worldMous);
        foreach (var sub in subCuartos)
        {
            if (sub.Contains(localMous))
            {
                Handles.Label(worldMous + umbralTam * Vector3.down, sub.size.ToString() + " $ " + (umbralTam / (1f - porcentajeTamCorte[1])));
                var prevMat = Handles.matrix;
                Handles.matrix = seccionActual.transform.localToWorldMatrix;
                Handles.DrawWireCube(sub.meta.center, sub.meta.size);
                Handles.matrix = prevMat;
            }
        }
    }


    [CustomEditor(typeof(GenSeccionCuartos))]
    public class GenCuartosEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Re procesar Rects"))
            {
                foreach (GenSeccionCuartos gen in targets) gen.ProcesarRects();
            }
            DrawDefaultInspector();
        }
    }
#endif
}
