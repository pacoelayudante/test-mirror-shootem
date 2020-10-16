using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class LayoutCuarto : MonoBehaviour
{
    public enum Categoria
    {
        Grande, Peque
    }
    public Categoria categoria;

    Rigidbody2D rigid;
    Rigidbody2D Rigid
    {
        get
        {
            if (rigid == null)
            {
                rigid = GetComponent<Rigidbody2D>();
            }
            return rigid;
        }
    }
    BoxCollider2D boxCol;
    public BoxCollider2D BoxCol { get { return boxCol ? boxCol : boxCol = GetComponent<BoxCollider2D>(); } }
    public Vector2 Size { get { return BoxCol ? BoxCol.size : Vector2.zero; } }
    public Vector2 Offset { get { return BoxCol ? BoxCol.offset : Vector2.zero; } }

    [System.Serializable]
    public struct Muro
    {
        public bool solito;
        public bool discriminable;
        public Vector2 a, b;
        public bool EsOrtogonal
        {
            get { return a.x == b.x || a.y == b.y; }
        }
        public float Distance { get { return Vector2.Distance(a, b); } }
        public Vector2 PuntoMedio { get { return (a + b) * 0.5f; } }
        public Muro(Vector2 a, Vector2 b, Matrix4x4 worldLocalMatrix) : this(a, b, worldLocalMatrix, false) { }
        public Muro(Vector2 a, Vector2 b, Matrix4x4 worldLocalMatrix, bool solito)
        {
            this.a = worldLocalMatrix.MultiplyPoint(a);
            this.b = worldLocalMatrix.MultiplyPoint(b);
            this.solito = solito;
            this.discriminable = false;
        }

        public void OnDrawGizmosSelected()
        {
            if (discriminable)
            {
                Gizmos.color = Color.cyan;
                var pm = PuntoMedio;
                var tamCruz = Physics2D.defaultContactOffset * 12f;
                Gizmos.DrawWireCube(pm, Vector2.one * tamCruz);
                // var vecOne = Vector2.up * tamCruz;
                // var vecOneCroz = Vector2.right * tamCruz;
                // Gizmos.DrawLine(pm + vecOne, pm - vecOne);
                // Gizmos.DrawLine(pm + vecOneCroz, pm - vecOneCroz);
            }
            Gizmos.color = solito ? Color.red : Color.yellow;
            if (Distance <= Physics2D.defaultContactOffset * 3f && !discriminable)
            {
                var pm = PuntoMedio;
                var tamCruz = Physics2D.defaultContactOffset * 20f;
                Gizmos.DrawWireCube(pm, Vector2.one * tamCruz);
                // var vecOne = Vector2.one * tamCruz;
                // var vecOneCroz = new Vector2(tamCruz, -tamCruz);
                // Gizmos.DrawLine(pm + vecOne, pm - vecOne);
                // Gizmos.DrawLine(pm + vecOneCroz, pm - vecOneCroz);
            }
            Gizmos.DrawLine(a, b);
        }
    }

    public List<LayoutCuarto> pegados = new List<LayoutCuarto>();
    public List<Rigidbody2D> pegadosRigid = new List<Rigidbody2D>();
    public List<BoxCollider2D> pegadosColliders = new List<BoxCollider2D>(),
    arriba = new List<BoxCollider2D>(), derecha = new List<BoxCollider2D>(), abajo = new List<BoxCollider2D>(), izquierda = new List<BoxCollider2D>();

    public List<Muro> muros = new List<Muro>();
    public List<Collider2D> murosColliders = new List<Collider2D>();
    public GameObject colliderObj;

    public List<LayoutCuarto> pegadosFiltrados = new List<LayoutCuarto>(),pegadosTocandoPoco=new List<LayoutCuarto>();

    public int DiffPegadosFiltrados
    {
        get
        {
            UnityEngine.Assertions.Assert.IsTrue(pegados.Count >= pegadosFiltrados.Count);
            return pegados.Count - pegadosFiltrados.Count;
        }
    }

    public bool Simulated
    {
        get { return Rigid ? Rigid.simulated : false; }
        set { if (Rigid) Rigid.simulated = value; }
    }

    public void RevisarPegados(float filtroOverlap = 0f)
    {
        pegados.Clear();
        pegadosRigid.Clear();
        pegadosColliders.Clear();
        pegadosFiltrados.Clear();
        pegadosTocandoPoco.Clear();
        arriba.Clear();
        abajo.Clear();
        derecha.Clear();
        izquierda.Clear();
        if (Rigid)
        {
            var contactos = new ContactPoint2D[50];
            int cant = Rigid.GetContacts(contactos);
            for (int i = 0; i < cant; i++)
            {
                var contacto = contactos[i];
                var tuBoxCol = contacto.collider as BoxCollider2D;
                var miBox = BoxCol ? BoxCol : contacto.otherCollider as BoxCollider2D;
                if (tuBoxCol && !pegadosColliders.Contains(tuBoxCol))
                {
                    pegadosColliders.Add(tuBoxCol);
                    var tuBoundsMax = tuBoxCol.bounds.max;
                    var tuBoundsMin = tuBoxCol.bounds.min;
                    var miBoundsMax = miBox.bounds.max;
                    var miBoundsMin = miBox.bounds.min;

                    // Nueva tecnica, se tomaran los bloques incluyendo superposiciones, un mismo bloque que esta arriba y abajo entra en ambos grupos
                    if (tuBoundsMax.y > miBoundsMax.y) arriba.Add(tuBoxCol);
                    if (tuBoundsMin.y < miBoundsMin.y) abajo.Add(tuBoxCol);

                    if (tuBoundsMax.x > miBoundsMax.x) derecha.Add(tuBoxCol);
                    if (tuBoundsMin.x < miBoundsMin.x) izquierda.Add(tuBoxCol);

                    if (contacto.rigidbody && !pegadosRigid.Contains(contacto.rigidbody))
                    {
                        pegadosRigid.Add(contacto.rigidbody);
                        var tuLaycuarto = contacto.rigidbody.GetComponent<LayoutCuarto>();
                        if (tuLaycuarto)
                        {
                            pegados.Add(tuLaycuarto);

                            var overLapX = Mathf.Min(tuBoundsMax.x, miBoundsMax.x) - Mathf.Max(tuBoundsMin.x, miBoundsMin.x);
                            var overLapY = Mathf.Min(tuBoundsMax.y, miBoundsMax.y) - Mathf.Max(tuBoundsMin.y, miBoundsMin.y);
                            if (overLapX > filtroOverlap || overLapY > filtroOverlap) pegadosFiltrados.Add(tuLaycuarto);
                            else pegadosTocandoPoco.Add(tuLaycuarto);
                        }
                    }
                }
            }
        }

        RevisarMuros();
    }

    public GameObject RenderEdgeColliders(float tamMinMuro = 0f, float tamCajaMuroMin = 1f, float grosorMuro = 0f, BoxCollider2D prefabCaja = null)
    {
        if (colliderObj)
        {
            if (Application.isPlaying) Destroy(colliderObj);
#if UNITY_EDITOR
            else DestroyImmediate(colliderObj);
#endif
        }

        murosColliders.Clear();
        colliderObj = new GameObject("collider obj");
        colliderObj.transform.parent = transform;
        colliderObj.transform.localPosition = Vector3.zero;
#if UNITY_EDITOR
        //ArgumentException: JsonUtility.ToJson does not support engine types.
        // Por esto es que hay que utilizar la version del Editor
        var sr = GetComponent<SpriteRenderer>();
        if (sr) UnityEditor.EditorJsonUtility.FromJsonOverwrite(UnityEditor.EditorJsonUtility.ToJson(sr), colliderObj.AddComponent<SpriteRenderer>());
#endif
        foreach (var muro in muros)
        {
            if (muro.discriminable) continue;
            if (muro.Distance < tamMinMuro)
            {
                if (prefabCaja != null)
                {
                    var niuCajaInst = Instantiate(prefabCaja, colliderObj.transform);
                    niuCajaInst.transform.localPosition = muro.PuntoMedio;
                    murosColliders.Add(niuCajaInst);
                    if (!niuCajaInst.gameObject.activeSelf) niuCajaInst.gameObject.SetActive(true);
                }
                else if (tamCajaMuroMin > 0f)
                {
                    var niuMuroCaja = colliderObj.AddComponent<BoxCollider2D>();
                    niuMuroCaja.offset = muro.PuntoMedio;
                    niuMuroCaja.size = Vector2.one * tamCajaMuroMin;
                    murosColliders.Add(niuMuroCaja);
                }
            }
            else
            {
                if (grosorMuro <= 0f)
                {
                    var niuEge = colliderObj.AddComponent<EdgeCollider2D>();
                    niuEge.points = new Vector2[] { muro.a, muro.b };
                    murosColliders.Add(niuEge);
                }
                else
                {
                    if (!muro.EsOrtogonal)
                    {
                        Debug.LogError("Muro no ortogonal, deberia haber una implementacion al respecto");
                        // Ya sea una implmentacion general o unica para los muros no ortogonales,
                        // (seguramente es mejor hacer general) donde se crea un sub objeto , y se lo posiciona y rota
                        // en base a donde debe estar (en vez de poner todos los muros asi como del mismo objeto)
                    }
                    var niuMuro = colliderObj.AddComponent<BoxCollider2D>();
                    niuMuro.offset = muro.PuntoMedio;
                    int ejeDominante = (muro.a.x == muro.b.x ? 1 : 0);
                    //Es decir, si tienen mismo X (asumiendo que es ortonogal) el eje dominante es el Y osea Vec2[1]
                    // else el eje dominante es X osea Vec2[0]
                    var tam = Vector2.one * grosorMuro;
                    tam[ejeDominante] = Mathf.Abs(muro.a[ejeDominante] - muro.b[ejeDominante]);// esto es todo ORTOGANAL ONLY
                    niuMuro.size = tam;
                }
            }
        }
        return colliderObj;
    }

    [ContextMenu("Revisar Muros")]
    void RevisarMuros()
    {
        muros.Clear();
        if (!BoxCol) return;

        RevisaMuros(new Vector2(BoxCol.bounds.min.x, BoxCol.bounds.max.y), arriba, Vector2.right, 0, 1,
            (col) => col.bounds.min.x, (col) => col.bounds.max.x);
        RevisaMuros(new Vector2(BoxCol.bounds.max.x, BoxCol.bounds.max.y), derecha, Vector2.down, 1, -1,
            (col) => col.bounds.max.y, (col) => col.bounds.min.y);
        RevisaMuros(new Vector2(BoxCol.bounds.max.x, BoxCol.bounds.min.y), abajo, Vector2.left, 0, -1,
            (col) => col.bounds.max.x, (col) => col.bounds.min.x);
        RevisaMuros(new Vector2(BoxCol.bounds.min.x, BoxCol.bounds.min.y), izquierda, Vector2.up, 1, 1,
            (col) => col.bounds.min.y, (col) => col.bounds.max.y);
    }
    void RevisaMuros(Vector2 puntoInicio, List<BoxCollider2D> grupo, Vector2 dirAvanzaMuro, int ejeMov, int signoEje, System.Func<BoxCollider2D, float> fBoundMin, System.Func<BoxCollider2D, float> fBoundMax)
    {
        if (grupo.Count == 0)
        {
            muros.Add(new Muro(puntoInicio, puntoInicio + dirAvanzaMuro * BoxCol.size[ejeMov], transform.worldToLocalMatrix, true));
        }
        else
        {
            grupo.Sort((a, b) => signoEje * fBoundMin(a).CompareTo(fBoundMin(b)));
            // var pri = grupo[0];
            // if (fBoundMin(pri) * signoEje <= puntoInicio[ejeMov] * signoEje) puntoInicio[ejeMov] = fBoundMax(pri);
            foreach (var col in grupo)
            {
                if (puntoInicio[ejeMov] * signoEje < fBoundMin(col) * signoEje)
                {
                    float off = fBoundMin(col) * signoEje - puntoInicio[ejeMov] * signoEje;
                    var niuMuro = new Muro(puntoInicio, puntoInicio + dirAvanzaMuro * off, transform.worldToLocalMatrix);
                    niuMuro.discriminable = off < Physics2D.defaultContactOffset * 2f;
                    muros.Add(niuMuro);
                    puntoInicio[ejeMov] = fBoundMax(col);
                }
                else
                {//significa que el punto de inicio esta "adentro" del collider, y lo tengo que mover hasta afuera de este al menos
                    var niuPos = fBoundMax(col);
                    //pero nos tenemos que asegurar de no volver hacia atras
                    if (puntoInicio[ejeMov] * signoEje < niuPos * signoEje) puntoInicio[ejeMov] = niuPos;
                }
            }
            if (puntoInicio[ejeMov] * signoEje < fBoundMax(BoxCol) * signoEje)
            {//por ahi nos quedamos sin colliders, pero aun nos falta poner muro del otro lado
                float off = fBoundMax(BoxCol) * signoEje - puntoInicio[ejeMov] * signoEje;
                muros.Add(new Muro(puntoInicio, puntoInicio + dirAvanzaMuro * off, transform.worldToLocalMatrix));
                puntoInicio[ejeMov] += off;
            }
        }
    }

    /* public void OnDrawGizmos()
    {
#if UNITY_EDITOR
        if (UnityEditor.Selection.GetFiltered<LayoutCuarto>(UnityEditor.SelectionMode.Deep).Length > 0) return;
#endif
        Gizmos.matrix = transform.localToWorldMatrix;
        foreach (var muro in muros)
        {
            muro.OnDrawGizmosSelected();
        }
    }*/
    public void OnDrawGizmosSelected()
    {
#if UNITY_EDITOR
        if (UnityEditor.Selection.GetFiltered<LayoutCuarto>(UnityEditor.SelectionMode.Deep).Length <= 1)
#endif
        {
            Gizmos.color = Color.blue;
            foreach (BoxCollider2D bcollider in pegadosColliders)
            {
                if (bcollider)
                {
                    Gizmos.matrix = bcollider.transform.localToWorldMatrix;
                    Gizmos.DrawWireCube(bcollider.offset, bcollider.size);
                }
            }
            foreach (var tocandoPoco in pegadosTocandoPoco)
            {
                if (tocandoPoco)
                {
                    Gizmos.matrix = tocandoPoco.transform.localToWorldMatrix;
                    Gizmos.DrawSphere(Vector3.zero, Mathf.Min(tocandoPoco.Size.x,tocandoPoco.Size.y) / 8f);
                }
            }

#if UNITY_EDITOR
            var estilo = new GUIStyle("Label");
            estilo.normal.textColor = Color.yellow;
            foreach (BoxCollider2D bcollider in arriba) if (bcollider) UnityEditor.Handles.Label(bcollider.transform.position, "ARRIBA", estilo);
            foreach (BoxCollider2D bcollider in abajo) if (bcollider) UnityEditor.Handles.Label(bcollider.transform.position, "\nABAJO", estilo);
            foreach (BoxCollider2D bcollider in izquierda) if (bcollider) UnityEditor.Handles.Label(bcollider.transform.position, "\n\nIZQUIERDA", estilo);
            foreach (BoxCollider2D bcollider in derecha) if (bcollider) UnityEditor.Handles.Label(bcollider.transform.position, "\n\n\nDERECHA", estilo);
#endif
        }

        Gizmos.matrix = transform.localToWorldMatrix;
        foreach (var muro in muros)
        {
            muro.OnDrawGizmosSelected();
        }
    }
}
