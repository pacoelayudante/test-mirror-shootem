using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[DisallowMultipleComponent]
public class PosibleObjetivo : MonoBehaviour
{
    static List<PosibleObjetivo> posibles = new List<PosibleObjetivo>();

    [SerializeField] float radio = 1f;

    Collider2D _collider;
    public Collider2D Collider => _collider?_collider:_collider = GetComponent<Collider2D>();
    public static bool ObjetivoCerca(Vector2 pos) => posibles.Any(obj => Vector2.Distance(obj.transform.position, pos) < obj.radio);

    private void OnEnable()
    {
        posibles.Add(this);
    }
    void OnDisable()
    {
        posibles.Remove(this);
    }

    public void Gizmo()
    {
        Gizmos.DrawWireSphere(transform.position, radio);
    }
}
