using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class ControlMirror : MonoBehaviour
{
    [SerializeField] string _ipServidor = "localhost";
    public string IPServidor {
        get => _ipServidor;
        set => _ipServidor = value;
    }
    
    public void StartHost() {
        if (NetworkManager.singleton) {
            NetworkManager.singleton.StartHost();
        }
    }

    public void JoinAsClient() => JoinAsClient(_ipServidor);
    public void JoinAsClient(string ip) {
        if (NetworkManager.singleton) {
            NetworkManager.singleton.StartClient( new System.Uri(ip) );
        }
    }

}
