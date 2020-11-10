using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class MenuPermanente : MonoBehaviour
{
    [Guazu.DrawersCopados.SceneAssetPathAsString]
    public string escenaInicial;
    public Button botCerrar, botDesconectar;

    void Start()
    {
        if (botCerrar) botCerrar.onClick.AddListener(() => Application.Quit());
        if (botDesconectar) botDesconectar.onClick.AddListener(() =>
        {
            NetworkManager.singleton.StopClient();
            NetworkManager.singleton.StopServer();
            Destroy(NetworkManager.singleton.transform.root.gameObject);
            UnityEngine.SceneManagement.SceneManager.LoadScene(escenaInicial);
        });
    }

    void Update()
    {
        botDesconectar.gameObject.SetActive(NetworkManager.singleton.isNetworkActive);
    }
}
