using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameGeneralCanvas : MonoBehaviour
{
    WachinJugador _jugadorLocal;
    WachinJugador JugadorLocal => _jugadorLocal?_jugadorLocal:_jugadorLocal=FindObjectOfType<WachinJugador>();

    [SerializeField] Text textUI;
    [SerializeField] string formatoEnemigosRestantes = "Enemigos Restantes: {0}";
    bool jugadorSpawned = false;
    // Update is called once per frame
    void Update()
    {
        textUI.text = string.Format(formatoEnemigosRestantes, WachinEnemigo.Count);
    }
}
