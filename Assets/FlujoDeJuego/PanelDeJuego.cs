using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PanelDeJuego : MonoBehaviour
{
    public WachinJugador gorritosPrefab;
    public Transform posSpawnGorritos;

    public Button sigGorrito, antGorrito;
    public Button botPreparado;
    public InputField cambiarNombre;
    GameObject gorroSpawneado;
    int gorroMostradoIndex = -1;

    public Image[] imgModsActivos;
    public Button[] botModsActivos;

    public Image imgPreparade;

    void Start()
    {
        if (sigGorrito) sigGorrito.onClick.AddListener(SigGorrito);
        if (antGorrito) antGorrito.onClick.AddListener(AntGorrito);
        if (sigGorrito) sigGorrito.interactable = false;
        if (antGorrito) antGorrito.interactable = false;
        if (cambiarNombre) cambiarNombre.onValueChanged.AddListener(val =>
        {
            if (JugadorMirror.local) JugadorMirror.local.CmdCambiarNombre(val);
        });
        if (botPreparado) {
            botPreparado.interactable = false;
            botPreparado.onClick.AddListener(()=>{
                if (JugadorMirror.local) JugadorMirror.local.CmdTogglePreparade();
            });
        }

        StartCoroutine(GameUtils.EsperarTrueLuegoHacerCallback(
            () => JugadorMirror.local,
            () =>
            {
                if (sigGorrito) sigGorrito.interactable = true;
                if (antGorrito) antGorrito.interactable = true;
                if (botPreparado) botPreparado.interactable = true;

                for(int i=0; i<botModsActivos.Length; i++) {
                    var indice = i;
                    botModsActivos[i].interactable = true;
                    botModsActivos[i].onClick.AddListener(()=>{
                        if (JugadorMirror.local) JugadorMirror.local.CmdToggleMod(indice);
                    });
                }
            }
        ));
    }

    void SigGorrito()
    {
        if (JugadorMirror.local)
        {
            JugadorMirror.local.CmdCambiarGorrito((JugadorMirror.local.gorritoDeseado + 1) % gorritosPrefab.posiblesGorros.Length);
        }
    }
    void AntGorrito()
    {
        if (JugadorMirror.local)
        {
            JugadorMirror.local.CmdCambiarGorrito((JugadorMirror.local.gorritoDeseado + gorritosPrefab.posiblesGorros.Length - 1) % gorritosPrefab.posiblesGorros.Length);
        }
    }

    void Update()
    {
        if (!JugadorMirror.local) return;

        if (imgPreparade) imgPreparade.color = JugadorMirror.local.preparade?Color.white:Color.black;

        for(int i=0; i<imgModsActivos.Length; i++) {
            imgModsActivos[i].color = JugadorMirror.local.EstaModActivo(i)?Color.white:Color.black;
        }

        if (posSpawnGorritos && JugadorMirror.local.gorritoDeseado % gorritosPrefab.posiblesGorros.Length != gorroMostradoIndex)
        {
            if (gorroSpawneado) Destroy(gorroSpawneado);
            gorroMostradoIndex = JugadorMirror.local.gorritoDeseado % gorritosPrefab.posiblesGorros.Length;
            gorroSpawneado = Instantiate(gorritosPrefab.posiblesGorros[gorroMostradoIndex], posSpawnGorritos);
        }

        if (WachinJugador.local) gameObject.SetActive(false);
    }
}
