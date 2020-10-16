using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class GameUtils {
    public static Quaternion Random2DRotation => Quaternion.Euler(0,0,Random.value*360f);
    public static Vector3 Vector3XInvertida = new Vector3(-1f,1f,1f);
    public static Vector3 Vector3YInvertida = new Vector3(1f,-1f,1f);

    public static IEnumerator EsperarTrueLuegoHacerCallback(System.Func<bool> trueTester, System.Action callback) {
        while (!trueTester()) yield return null;
        if (callback != null) callback();
    }
    
    public static IEnumerator MientrasTrueHacerEsto(System.Func<bool> trueTester, System.Action callback) {
        while (trueTester()) {
            if (callback != null) callback();
            yield return null;
        }
    }

    public static Color ColorAlfa(Color entra, float alfa) {
        entra.a = alfa;
        return entra;
    }
    public static void ChangeAlfa(ref Color entra, float alfa) => entra.a = alfa;

    public static Mesh GenerateSpriteMesh(Sprite sprite, Mesh mesh = null) {
        if(sprite==null) return mesh;
        if(mesh == null) mesh = new Mesh();
        else mesh.Clear();
        mesh.vertices = sprite.vertices.Select(v=>(Vector3)v).ToArray();
        mesh.uv = sprite.uv;
        mesh.triangles = sprite.triangles.Select(t=>(int)t).ToArray();
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        return mesh;
    }
    
    public static bool LineIntersection(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, ref Vector2 result)
    {
        float bx = p2.x - p1.x;
        float by = p2.y - p1.y;
        float dx = p4.x - p3.x;
        float dy = p4.y - p3.y;
        float bDotDPerp = bx * dy - by * dx;
        if (bDotDPerp == 0)
        {
            return false;
        }
        float cx = p3.x - p1.x;
        float cy = p3.y - p1.y;
        float t = (cx * dy - cy * dx) / bDotDPerp;

        result.Set(p1.x + t * bx, p1.y + t * by);
        return true;
    }

    public static bool PuntoEnTriangulo(Vector2 triA, Vector2 triB, Vector2 triC, Vector2 pt)
    {
        // Compute vectors        
        var v0 = triC - triA;
        var v1 = triB - triA;
        var v2 = pt - triA;

        // Compute dot products
        var dot00 = Vector2.Dot(v0, v0);
        var dot01 = Vector2.Dot(v0, v1);
        var dot02 = Vector2.Dot(v0, v2);
        var dot11 = Vector2.Dot(v1, v1);
        var dot12 = Vector2.Dot(v1, v2);

        // Compute barycentric coordinates
        var invDenom = 1f / (dot00 * dot11 - dot01 * dot01);
        var u = (dot11 * dot02 - dot01 * dot12) * invDenom;
        var v = (dot00 * dot12 - dot01 * dot02) * invDenom;

        // Check if point is in triangle
        return (u >= 0) && (v >= 0) && (u + v < 1);
    }

    public static float QueLadoDeLaLinea(Vector2 lineaA, Vector2 lineaB, Vector2 punto)
    {
        return (lineaB.x - lineaA.x) * (punto.y - lineaA.y) - (lineaB.y - lineaA.y) * (punto.x - lineaA.x);
    }
    public static bool PuntoEnConoInfinito(Vector2 centroCono, Vector2 conoContraReloj, Vector2 conoConReloj, Vector2 punto)
    {
        return QueLadoDeLaLinea(centroCono, conoConReloj, punto) < 0f && QueLadoDeLaLinea(centroCono, conoContraReloj, punto) > 0f;
    }

    public static bool EstaEnLayerMask(int layer, LayerMask layerMask) => ((1<<layer)&layerMask.value)!=0;
    
    public static Rect RectSuperPosicion(Rect a, Rect b, bool amplitudMinimaCero = true)
    {
        var minX = Mathf.Max(a.xMin, b.xMin);
        var maxX = Mathf.Min(a.xMax, b.xMax);
        if (amplitudMinimaCero && minX > maxX) minX = maxX = (minX + maxX) / 2f;

        var minY = Mathf.Max(a.yMin, b.yMin);
        var maxY = Mathf.Min(a.yMax, b.yMax);
        if (amplitudMinimaCero && minY > maxY) minY = maxY = (minX + maxY) / 2f;

        return Rect.MinMaxRect(minX, minY, maxX, maxY);
    }
    public static List<Rect> RectSustraccion(Rect inicial, Rect sustraido)
    {
        var resultado = new List<Rect>();
        if (inicial.Overlaps(sustraido))
        {
            if (sustraido.Contains(inicial.min)&&sustraido.Contains(inicial.max)){
                //en este caso, la sutraccion abarca y contiene a todo el rect inicial
                return resultado;
            }
            if (inicial.xMin < sustraido.xMin)
            {
                resultado.Add( Rect.MinMaxRect(inicial.xMin,inicial.yMin,sustraido.xMin,inicial.yMax) );
            }
            if(inicial.xMax > sustraido.xMax)
            {
                resultado.Add( Rect.MinMaxRect(sustraido.xMax,inicial.yMin,inicial.xMax,inicial.yMax) );
            }
            inicial .xMin = Mathf.Max(inicial.xMin,sustraido.xMin);
            inicial .xMax = Mathf.Min(inicial.xMax,sustraido.xMax);
            if (inicial.yMin < sustraido.yMin)
            {
                resultado.Add( Rect.MinMaxRect(inicial.xMin,inicial.yMin,inicial.xMax,sustraido.yMin) );
            }
            if(inicial.yMax > sustraido.yMax)
            {
                resultado.Add( Rect.MinMaxRect(inicial.xMin,sustraido.yMax,inicial.xMax,inicial.yMax) );
            }
        }
        else
        {// NO HAY OVERLAP
            resultado.Add(inicial);
        }
        return resultado;
    }

#if UNITY_EDITOR
    static List<Rigidbody2D> reactivarRigids = new List<Rigidbody2D>();
    static void IterarFisica(int iteraciones, float deltaT = 0) {
        if (!Physics2D.autoSimulation) return;
        if (deltaT == 0) deltaT = Time.fixedDeltaTime/2f;
        if (deltaT == 0) return;
        Physics2D.autoSimulation = false;
        foreach(var rigid in reactivarRigids) {
            if(rigid)rigid.simulated = false;
        }
        foreach(var joint in GameObject.FindObjectsOfType<AnchoredJoint2D>()) {
            joint.autoConfigureConnectedAnchor = false;
            var jd = joint as DistanceJoint2D;
            if(jd) jd.autoConfigureDistance = false;
            var js = joint as SpringJoint2D;
            if(js) js.autoConfigureDistance = false;
        }
        for (int i=0; i<iteraciones; i++) {
            Physics2D.Simulate(deltaT);
            if(EditorUtility.DisplayCancelableProgressBar("Simulando",$"Iteraciones = {i}/{iteraciones}",(i+0.5f)/iteraciones)) {
                break;
            }
        }
        EditorUtility.ClearProgressBar();
        Physics2D.autoSimulation = true;
        foreach(var rigid in reactivarRigids) {
            if(rigid)rigid.simulated = true;
        }
    }
    public static void IterarFisicaTodo(int iteraciones, float deltaT=0) {
        IterarFisica(iteraciones,deltaT);
    }
    [MenuItem("CONTEXT/Rigidbody2D/Simular Todo (5000)")]
    public static void IterarFisicaTodo100() {
        IterarFisicaTodo(5000);
    }
#endif

}