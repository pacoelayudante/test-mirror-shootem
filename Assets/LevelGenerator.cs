using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class LevelGenerator : MonoBehaviour
{
    [SerializeField]GeneradorLayouts generadorLayouts;
    [SerializeField]GeneradorMapaArbol generadorMapaArbol;
    [SerializeField]GeneradorColliderGlobal generadorColliderGlobal;
    [SerializeField]NavMeshSurface surface;
    [SerializeField]MeshCollider meshCol;

    public WachinLogica prefabJugador;
    public WachinLogica prefabGuardia;
    public Vector2Int cantGuardiasPorPatrulla = new Vector2Int(3,6);
    public Vector2Int cantPatrullas = new Vector2Int(9,15);

    void Start() {
        generadorLayouts.Generar(true, false, true);
        generadorLayouts.Simular(noprogbar: true);
        generadorLayouts.Generar(false, true, false);
        generadorLayouts.Simular(noprogbar: true);

        generadorMapaArbol.Generar();

        generadorColliderGlobal.Generar();

        var mesh = meshCol.sharedMesh;
        meshCol.sharedMesh = null;
        meshCol.sharedMesh = mesh;

        surface.BuildNavMesh();

        var numPatrullas = Random.Range(cantPatrullas[0],cantPatrullas[1]);
        for (int i=0; i<numPatrullas; i++) {
            var guardias = Random.Range(cantGuardiasPorPatrulla[0],cantGuardiasPorPatrulla[1]);
            var pos = LayoutEnMundo.PuntoRandomEnLayout();
            for (int g=0; g<guardias; g++) {
                Instantiate(prefabGuardia, pos, Quaternion.identity);
            }
        }

        if (prefabJugador.gameObject.scene.rootCount > 0) {
            prefabJugador.Agent.Warp(LayoutEnMundo.PuntoRandomEnLayout());  
        }
    }
}
