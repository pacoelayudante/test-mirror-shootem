using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameGeneralCanvas : MonoBehaviour
{
    WachinJugador _jugadorLocal;
    WachinJugador JugadorLocal => _jugadorLocal?_jugadorLocal:_jugadorLocal=FindObjectOfType<WachinJugador>();

    [SerializeField] KeyCode resetButton = KeyCode.R;
    [SerializeField] KeyCode escapeButton = KeyCode.Escape;
    [SerializeField] Text textUI;
    [SerializeField] string formatoEnemigosRestantes = "Enemigos Restantes: {0}";
    [SerializeField] string resetearCon = "Reiniciar Escenario Con '{0}'";
    [SerializeField] string abortarCon = "Abortar Escenario Con '{0}'";
    bool jugadorSpawned = false;
    // Update is called once per frame
    void Update()
    {
        textUI.text = string.Format(abortarCon, escapeButton);
        if (jugadorSpawned) {
            textUI.text += $"\n{string.Format(formatoEnemigosRestantes, WachinEnemigo.Count)}";
            if (!JugadorLocal || WachinEnemigo.Count==0) {
                textUI.text += $"\n{string.Format(resetearCon, resetButton)}";

                if (Input.GetKeyUp(resetButton)) {
                    UnityEngine.SceneManagement.SceneManager.LoadScene(0);
                }
            }
        }
        else {
            if(JugadorLocal) jugadorSpawned = true;
        }
        
        if (Input.GetKeyUp(escapeButton)) {
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
        }
    }
}
