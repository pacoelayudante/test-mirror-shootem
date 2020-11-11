using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Guazu.DrawersCopados;
using UnityEngine.SceneManagement;

public class ControlMirror : MonoBehaviour
{
    [SceneAssetPathAsString]
    public string gameScene = string.Empty;

    [SerializeField] string _ipServidor = "localhost";
    public string IPServidor {
        get => NetworkManager.singleton.networkAddress;
        set => NetworkManager.singleton.networkAddress = value;
        // get => _ipServidor;
        // set => _ipServidor = value;
    }

    public GameObject serverMenu, lobbyMenu;
    
    void Start() {        
        NetworkManager.singleton.GetComponent<Transport>().OnClientDisconnected.AddListener( ()=>{   
            serverMenu.SetActive(true);
            lobbyMenu.SetActive(false);
        });
        NetworkManager.singleton.GetComponent<Transport>().OnClientConnected.AddListener( ()=>{
            serverMenu.SetActive(false);
            lobbyMenu.SetActive(true);
        });

        NetworkManager.singleton.GetComponent<Transport>().OnServerDisconnected.AddListener( (valor)=>{   
            serverMenu.SetActive(true);
            lobbyMenu.SetActive(false);
        });
    }

    public void StartHost() {
        if (NetworkManager.singleton) {
            NetworkManager.singleton.StartHost();
        // serverMenu.SetActive(false);
        // lobbyMenu.SetActive(true);
        if (!string.IsNullOrEmpty(gameScene)) NetworkManager.singleton.ServerChangeScene(gameScene);
        }
    }

    public void JoinAsClient() => JoinAsClient(_ipServidor);
    public void JoinAsClient(string ip) {
        if (NetworkManager.singleton) {
            // NetworkManager.singleton.StartClient( new System.Uri(ip) );
            NetworkManager.singleton.StartClient();
        // serverMenu.SetActive(false);
        // lobbyMenu.SetActive(true);
        }
    }


}
