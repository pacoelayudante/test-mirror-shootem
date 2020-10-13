using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class MiraCam : MonoBehaviour
{
    [Range(0f, 1f)]
    public float verticalRotation = 0.5f;

    void Start() { }//solo parar que se vea el control de "enabled"

    private void OnWillRenderObject()
    {
        if (!enabled) return;
        var cam = Camera.current;
        if (!cam) cam = Camera.main;
        if (cam)
        {
            var posdiff = cam.transform.position - transform.position;
            var rotFull = Quaternion.LookRotation(posdiff, Vector3.up);
            posdiff.y = 0f;
            var rotZero = Quaternion.LookRotation(posdiff, Vector3.up);
            transform.rotation = Quaternion.Lerp(rotZero, rotFull, verticalRotation);
        }
    }
}
