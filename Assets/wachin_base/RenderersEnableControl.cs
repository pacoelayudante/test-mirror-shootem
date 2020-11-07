using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RenderersEnableControl : MonoBehaviour {
    Renderer[] _rends;
    Renderer[] Rends => _rends!=null?_rends:_rends=GetComponentsInChildren<Renderer>();

    void OnEnable() {
        foreach(var r in Rends) r.enabled = true;
    }
    void OnDisable() {
        foreach(var r in Rends) r.enabled = false;
    }
}