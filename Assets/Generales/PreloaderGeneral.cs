using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PreloaderGeneral : ScriptableObject
{
    static PreloaderGeneral _instance;

    [RuntimeInitializeOnLoadMethod]
    static void Init() {
        if (_instance) {
            
        }
    }

    void OnEnable() {
        _instance = this;
    }
}
