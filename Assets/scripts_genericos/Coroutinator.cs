using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("")]
class Coroutinator : MonoBehaviour
{
    static Coroutinator _instance;
    static Coroutinator Instance {
        get {
            if (!Application.isPlaying) return null;
            if(!_instance) {
                var go = new GameObject(">>> Coroutinator");
                DontDestroyOnLoad(go);
                go.hideFlags |= HideFlags.DontSaveInBuild|HideFlags.NotEditable|HideFlags.DontSaveInEditor;
                _instance = go.AddComponent<Coroutinator>();
                _instance.hideFlags |= HideFlags.DontSaveInBuild|HideFlags.NotEditable|HideFlags.DontSaveInEditor;
            }
            return _instance;
        }
    }
    public static Coroutine Start(IEnumerator routine) => Application.isPlaying?Instance.StartCoroutine(routine):null;
    public static void Stop(Coroutine routine) => Instance.StopCoroutine(routine);
    public static void Stop(IEnumerator routine) => Instance.StopCoroutine(routine);

    private void OnValidate() {
        this.hideFlags |= HideFlags.DontSaveInBuild|HideFlags.NotEditable|HideFlags.DontSaveInEditor;
    }
}