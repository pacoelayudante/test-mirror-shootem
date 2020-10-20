using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LayoutEnMundo : MonoBehaviour
{
    public static Transform Transform => _inst ? _inst.transform : null;
    public static Vector3 TransformPoint(Vector3 p) => Transform ? Transform.TransformPoint(p) : p;
    public static Vector3 TransformPoint(float x, float y, float z) => Transform ? Transform.TransformPoint(x, y, z) : new Vector3(x, y, z);

    static GeneradorMapaArbol _arbol;
    static GeneradorMapaArbol Arbol => _arbol ? _arbol : _arbol = FindObjectOfType<GeneradorMapaArbol>();

    public static Vector3 PuntoRandomEnLayoutEnMapa()
    {
        var nodo = Arbol.nodos[Random.Range(0, Arbol.nodos.Count)];
        var cuarto = nodo.cuartosPropios[Random.Range(0, nodo.cuartosPropios.Count)];
        return Vector2.Scale(cuarto.Size / 2f, new Vector2(Random.value, Random.value)) + cuarto.Offset + (Vector2)cuarto.transform.position;
    }
    public static Vector3 PuntoRandomEnLayout()
    {
        var pos = PuntoRandomEnLayoutEnMapa();
        return LayoutEnMundo.TransformPoint(pos.x, 0f, pos.y);
    }

    static LayoutEnMundo _inst;
    private void Awake()
    {
        if (_inst) Debug.LogWarning("no deberia suceder");
        _inst = this;
    }
    private void OnDestroy()
    {
        if (_inst == this) _inst = null;
    }
}
