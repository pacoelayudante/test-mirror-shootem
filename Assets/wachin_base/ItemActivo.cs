using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class ItemActivo : NetworkBehaviour
{
    public bool Activable => _activable!=null?_activable.Invoke():true;
    System.Func<bool> _activable;
    public event System.Action alActivar;

    public void SetActivableCheck(System.Func<bool> func) {
        _activable = func;
    }

    public void Activar() {
        if (hasAuthority) CmdActivar();
    }

    [Command]
    void CmdActivar(){
        alActivar?.Invoke();
    }
}
