using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LanzaderaDeMoscas : MonoBehaviour
{
    [SerializeField] Mosca moscaPrefab = null;

    [SerializeField] Vector2 salidaMosca = Vector2.zero;
    [SerializeField] float anguloSalida = 0f;
    [SerializeField] int cantMoscas = 12;
    [SerializeField] Vector2 intervaloEntreMoscas = new Vector2(0.1f,.3f);

    [SerializeField] float cooldown = 20f;
    [SerializeField] int minMoscas = 6;

    List<Mosca> moscasCreadas = new List<Mosca>();
    float siguienteLanzamiento = 0f;

    void Update() {
        if (moscasCreadas.Count > 0 && !moscasCreadas[Time.frameCount%moscasCreadas.Count]) moscasCreadas.RemoveAt(Time.frameCount%moscasCreadas.Count);
        if (moscasCreadas.Count < minMoscas && Time.time >= siguienteLanzamiento) {
            LanzarMoscas();
        }
    }

    void LanzarMoscas() => StartCoroutine(LanzarMoscasCorut());
    IEnumerator LanzarMoscasCorut() {
        siguienteLanzamiento = Time.time+cooldown;
        for (int i=0; i<cantMoscas; i++) {

            var nuevaMosca = Instantiate(moscaPrefab, transform.TransformPoint(salidaMosca), Quaternion.identity);
            nuevaMosca.Angulo = anguloSalida;
            moscasCreadas.Add(nuevaMosca);

            yield return new WaitForSeconds(Random.Range(intervaloEntreMoscas.x,intervaloEntreMoscas.y));
        }
    }
}
