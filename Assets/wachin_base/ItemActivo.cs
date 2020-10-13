using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemActivo : MonoBehaviour
{
    public event System.Action alActivar;

    public void Activar() {
        alActivar?.Invoke();
    }
}
