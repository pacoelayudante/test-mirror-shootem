using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemActivo : MonoBehaviour
{
    public bool Activable => _activable!=null?_activable.Invoke():true;
    System.Func<bool> _activable;
    public event System.Action alActivar;

    public void SetActivableCheck(System.Func<bool> func) {
        _activable = func;
    }

    public void Activar() {
        alActivar?.Invoke();
    }
}
