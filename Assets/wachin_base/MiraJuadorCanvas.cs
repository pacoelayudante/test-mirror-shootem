using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MiraJuadorCanvas : MonoBehaviour
{
    ControlFlechitas _jugadorLocal;
    ControlFlechitas JugadorLocal => _jugadorLocal?_jugadorLocal:_jugadorLocal=FindObjectOfType<ControlFlechitas>();

    RectTransform RectTransform => (RectTransform)transform;
    


        // Update is called once per frame
    private void OnEnable() {
        Cursor.visible = false;
    }
    private void OnDisable() {
        Cursor.visible = true;
    }

    void LateUpdate()
    {
        transform.position = Input.mousePosition;
    }
}
