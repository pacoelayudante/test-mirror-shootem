using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class CamSortAxis : MonoBehaviour
{
    [SerializeField] TransparencySortMode sortMode;
    // [Guazu.DrawersCopados.Ocultador("sortMode", (int)TransparencySortMode.CustomAxis)]    
    // [SerializeField] Space axisSpace = Space.Self;
    // [Guazu.DrawersCopados.Ocultador("sortMode", (int)TransparencySortMode.CustomAxis)]
    // [SerializeField] Vector3 sortAxis = Vector3.forward;

    Camera _cam;
    Camera Cam => _cam?_cam:_cam=GetComponent<Camera>();

    void Update() {
        if (Cam) {
            if (Cam.transparencySortMode != sortMode) Cam.transparencySortMode = sortMode;
            if (sortMode == TransparencySortMode.CustomAxis) {
                // Cam.transparencySortAxis = axisSpace==Space.World?sortAxis:transform.TransformVector(sortAxis);
                Cam.transparencySortAxis = Vector3.Scale(Vector3.forward+Vector3.right,transform.forward).normalized;
            }
        }
    }
}
