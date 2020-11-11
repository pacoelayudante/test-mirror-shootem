using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Steamworks;
using Steamworks.Data;
using System.Linq;
using Mirror;

public class SteamLobbyControl : MonoBehaviour
{
    public Button abrirServer;
    public Button refrescarLista;
    public Dropdown unirseManual;
    public Button templateBotonAmigo;
    public GameObject panelDeConexion, panelDeJuego;

    Friend[] friendsList;
    Dictionary<Friend, Button> botonAutoJoin = new Dictionary<Friend, Button>();
    Dictionary<Friend, Lobby> lobbies = new Dictionary<Friend, Lobby>();

    [Guazu.DrawersCopados.SceneAssetPathAsString]
    public string gameScene;

    void SetearInteractivo(bool interactivo) {
        if (abrirServer) abrirServer.interactable = interactivo;
        if (unirseManual) unirseManual.interactable = interactivo;
        foreach (var bot in botonAutoJoin.Values) bot.interactable = interactivo;
    }

    void Start()
    {
        if (refrescarLista) refrescarLista.interactable = false;
        SetearInteractivo(false);

        StartCoroutine(GameUtils.EsperarTrueLuegoHacerCallback(
                () => SteamClient.IsLoggedOn,
                StartCuandoSteam
            ));

        NetworkManager.singleton.GetComponent<Transport>().OnClientDisconnected.AddListener( ()=>{
            SetearInteractivo(true);    
            panelDeConexion.SetActive(true);
            panelDeJuego.SetActive(false);        
        });
        NetworkManager.singleton.GetComponent<Transport>().OnClientConnected.AddListener( ()=>{
            panelDeConexion.SetActive(false);
            panelDeJuego.SetActive(true);
        });
        NetworkManager.singleton.GetComponent<Transport>().OnServerDisconnected.AddListener( (valor)=>{
            SetearInteractivo(true);       
            panelDeConexion.SetActive(true);
            panelDeJuego.SetActive(false);       
        });
    }
    void StartCuandoSteam()
    {
        if (refrescarLista) refrescarLista.interactable = true;

        if (abrirServer) abrirServer.onClick.AddListener(AbrirServidor);
        if (refrescarLista) refrescarLista.onClick.AddListener(RefrescarLista);

        friendsList = SteamFriends.GetFriends().ToArray();

        templateBotonAmigo.gameObject.SetActive(false);
        foreach (var friend in friendsList)
        {
            var bot = Instantiate(templateBotonAmigo, templateBotonAmigo.transform.parent);
            var txt = bot.GetComponentInChildren<Text>();
            if (txt) txt.text = friend.Name;
            bot.onClick.AddListener(() => UnirseAlServidorAmigo(friend));
            botonAutoJoin.Add(friend, bot);
        }
        
        if (unirseManual)
        {
            unirseManual.AddOptions(
                friendsList.Select(f => new Dropdown.OptionData(f.Name))
                .ToList()
            );
            unirseManual.onValueChanged.AddListener((val) =>
           {
               if (friendsList != null && val > 0)
               {
                   var selected = friendsList.FirstOrDefault(f=>f.Name==unirseManual.options[val].text);
                   UnirseAlServidorAmigo(selected);
               }
           });
        }

        RefrescarLista();
        
        SetearInteractivo(true);

        // SteamMatchmaking.LobbyList.RequestAsync().ContinueWith((lobbis)=>{
        //     Debug.Log("list returned");
        //     foreach(var lobbi in lobbies) {
        //         Debug.Log($"lobbi {lobbi.Key} - {lobbi.Value.Data} - {lobbi.Value.Id} - {lobbi.Value.Owner}");
        //     }
        // });
    }

    public void RefrescarLista()
    {
        if (refrescarLista) refrescarLista.interactable = false;
        foreach(var bot in botonAutoJoin.Values.Where(b=>b)) bot.gameObject.SetActive(false);
        
        SteamMatchmaking.LobbyList.FilterDistanceWorldwide().RequestAsync().ContinueWith(resultado=>{
            foreach(var lobbi in resultado.Result) {
                Debug.Log($"lobbi - {lobbi.Owner}");
                if (botonAutoJoin != null && botonAutoJoin.ContainsKey(lobbi.Owner) && botonAutoJoin[lobbi.Owner]) {
                    botonAutoJoin[lobbi.Owner].gameObject.SetActive(true);
                }
            }
            if (refrescarLista) refrescarLista.interactable = true;
        });

        

        // foreach (var f in friendsList)
        // {
        //     if (botonAutoJoin == null || !botonAutoJoin[f]) continue;//por ahi se destruyo todo en el medio de los awaits?
        //     // if ( ! f.IsPlayingThisGame ) {
        //     //     botonAutoJoin[f].gameObject.SetActive(false);
        //     //     continue;
        //     // }
        //     await f.RequestInfoAsync();
        //     lobbies.Remove(f);
        //     if (f.GameInfo?.Lobby == null)
        //     {
        //         botonAutoJoin[f].gameObject.SetActive(false);
        //     }
        //     else
        //     {
        //         lobbies.Add(f, f.GameInfo?.Lobby ?? default(Lobby));
        //         botonAutoJoin[f].gameObject.SetActive(true);
        //     }
        // }
        // if (refrescarLista) refrescarLista.interactable = true;
    }

    public void UnirseAlServidorAmigo(Friend f)
    {
        SetearInteractivo(false);
        NetworkManager.singleton.networkAddress = f.Id.Value.ToString();
        NetworkManager.singleton.StartClient();

        UnirseALobbyAmigoAsync(f);
    }

    public async void UnirseALobbyAmigoAsync(Friend f)
    {
        await f.RequestInfoAsync();
        var lobbi = f.GameInfo?.Lobby ?? default(Lobby);
        await SteamMatchmaking.JoinLobbyAsync(lobbi.Id);
    }

    public void AbrirServidor()
    {
        if (abrirServer) abrirServer.interactable = false;
        if (unirseManual) unirseManual.interactable = false;
        foreach (var bot in botonAutoJoin.Values) bot.interactable = false;

            panelDeConexion.SetActive(false);
            panelDeJuego.SetActive(true);
            NetworkManager.singleton.StartHost();
            if(!string.IsNullOrEmpty(gameScene)) NetworkManager.singleton.ServerChangeScene(gameScene);

        var lobbiTalVez = SteamMatchmaking.CreateLobbyAsync().ContinueWith(resultado=>{
            if (resultado.Result.HasValue) {
                var lobbi = resultado.Result.Value;
                lobbi.SetPublic();
            }
        });
    }
}
