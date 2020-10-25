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
    bool onGame;
    
    public void StartHost() {
        if (NetworkManager.singleton) {
            NetworkManager.singleton.StartHost();
        }
    }

    public void JoinAsClient() => JoinAsClient(_ipServidor);
    public void JoinAsClient(string ip) {
        if (NetworkManager.singleton) {
            // NetworkManager.singleton.StartClient( new System.Uri(ip) );
            NetworkManager.singleton.StartClient();
        }
    }

    private void Update() {
        serverMenu.SetActive(!NetworkManager.singleton.isNetworkActive);
        lobbyMenu.SetActive(!onGame && NetworkManager.singleton.isNetworkActive && !WachinJugador.local);
    }

    public void Jugar() {
        onGame = true;
        serverMenu.SetActive(false);
        lobbyMenu.SetActive(false);

        if (!string.IsNullOrEmpty(gameScene)) NetworkManager.singleton.ServerChangeScene(gameScene);
    }

}
