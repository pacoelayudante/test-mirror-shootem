using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class LiderTacticStats : ScriptableObject
{
    
    static NavMeshHit hit;
    
    public Vector2 reconsiderarPatrullaCada = new Vector2(10f,30f);
    public float chanceDetenerse = .1f;
    public float rangoNuevoPuntoPatrulla = 200f;
    public float samplerRange = 1f;
    public float rangoAutoAsociarPatrulla = 5f;

    public void LiderarPatrulla(Patrulla patrulla) {
        if (Time.time > patrulla.reconsiderarPatrullaCuando) {
            patrulla.reconsiderarPatrullaCuando = Time.time+Random.Range(reconsiderarPatrullaCada[0],reconsiderarPatrullaCada[1]);
            if (Random.value < chanceDetenerse) {
                patrulla.destinoPatrulla = patrulla.PosLider;
            }
            else {
                var pos = LayoutEnMundo.PuntoRandomEnLayoutEnMapa();

                if (NavMesh.SamplePosition(LayoutEnMundo.TransformPoint(pos.x,0f,pos.y),out hit, samplerRange, NavMesh.AllAreas)) {
                    patrulla.destinoPatrulla = hit.position;
                }
                else patrulla.destinoPatrulla = patrulla.PosLider;
            }
        }
    }
}
