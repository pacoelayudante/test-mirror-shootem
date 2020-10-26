using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Cinemachine;

public class LevelGenerator : MonoBehaviour
{
    [SerializeField] CinemachineVirtualCamera camInicial;
    [SerializeField] public GeneradorLayouts generadorLayouts;
    [SerializeField] public GeneradorMapaArbol generadorMapaArbol;
    [SerializeField] public GeneradorColliderGlobal generadorColliderGlobal;
    [SerializeField] NavMeshSurface surface;
    [SerializeField] MeshCollider meshCol;

    public WachinLogica prefabJugador;
    public WachinLogica prefabGuardia;
    public Vector2Int cantGuardiasPorPatrulla = new Vector2Int(3, 6);
    public Vector2Int cantPatrullas = new Vector2Int(9, 15);

    public Coroutine GenerarLayout(bool renderIntermedio = true) => StartCoroutine(GenerarLayoutsRutina(renderIntermedio));
    IEnumerator GenerarLayoutsRutina(bool renderIntermedio = true)
    {
        generadorLayouts.Generar(true, true, true);
        for (int i = 0; i < generadorLayouts.pasosSimular / 10; i += 50)
        {
            generadorLayouts.Simular(50, noprogbar: true);
            if (renderIntermedio)
            {
                RenderNivel();
                yield return null;
            }
        }
        generadorLayouts.Generar(false, true, true);
        for (int i = 0; i < generadorLayouts.pasosSimular / 10; i += 100)
        {
            generadorLayouts.Simular(100, noprogbar: true);
            if (renderIntermedio)
            {
                RenderNivel();
                yield return null;
            }
        }
        generadorLayouts.Generar(false, true, false);
        for (int i = 0; i < generadorLayouts.pasosSimular; i += 1000)
        {
            generadorLayouts.Simular(1000, noprogbar: true);
            if (renderIntermedio)
            {
                RenderNivel();
                yield return null;
            }
        }
    }
    public void RenderNivel(bool actualizarMapaArbol = true)
    {
        if (actualizarMapaArbol) generadorMapaArbol.Generar();
        generadorColliderGlobal.Generar();
    }

    public void ActualizarColliderYNavSurface() {
        var mesh = meshCol.sharedMesh;
        meshCol.sharedMesh = null;
        meshCol.sharedMesh = mesh;

        surface.BuildNavMesh();
    }

    IEnumerator Start()
    {
        yield return StartCoroutine(GenerarLayoutsRutina(true));
        RenderNivel();

        ActualizarColliderYNavSurface();

        if (prefabJugador.gameObject.scene.rootCount > 0)
        {
            prefabJugador.gameObject.SetActive(true);
            prefabJugador.Agent.Warp(LayoutEnMundo.PuntoRandomEnLayout());
        }

        camInicial.enabled = false;

        // ¿tal vez un delay esperando que la camara termine la transicion?
        yield return new WaitForSeconds(3f);
        var numPatrullas = Random.Range(cantPatrullas[0], cantPatrullas[1]);
        for (int i = 0; i < numPatrullas; i++)
        {
            var guardias = Random.Range(cantGuardiasPorPatrulla[0], cantGuardiasPorPatrulla[1]);
            var pos = LayoutEnMundo.PuntoRandomEnLayout();
            for (int g = 0; g < guardias; g++)
            {
                Instantiate(prefabGuardia, pos, Quaternion.identity);
            }
        }
    }
}
