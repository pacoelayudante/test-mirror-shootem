﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LibTessDotNet;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GeneradorColliderGlobal : MonoBehaviour
{
    [SerializeField] GeneradorMapaArbol arbol;
    [SerializeField] float bordeExternoChico = .1f;
    [SerializeField] float bordeInternoChico = 0f;
    [SerializeField] float bordeExternoGrande = 0f;
    [SerializeField] float bordeInternoGrande = .1f;
    [SerializeField] PolygonCollider2D colliderGlobal;

    [SerializeField] bool generateMesh;
    [SerializeField] bool generarCollider2d = true;

    [SerializeField] Mesh generatedMesh;
    [SerializeField] MeshFilter genMeshFilter;
    [SerializeField] bool genPiso = true, genTechos = true, genInterior = true, genExterior = true;

    Tess tessExterno;
    Tess tessInterno;

    public void Generar()
    {
        Generar(arbol.nodos, arbol.vinculos, arbol.OverlapMinimoParaConectar);
    }
    public void Generar(List<SeccionDeLayout> secciones, List<VinculoEntreSecciones> vinculos, float diametroPuerta)
    {
        UnityEngine.Profiling.Profiler.BeginSample("Generando Collider Global");
        if (generateMesh) {
            if (!genMeshFilter) {
                var go = new GameObject("generated mesh");
                go.transform.SetParent(transform,false);
                genMeshFilter = go.AddComponent<MeshFilter>();
                go.AddComponent<MeshRenderer>();
            }
            if (!generatedMesh) {
                generatedMesh = new Mesh();
                genMeshFilter.sharedMesh = generatedMesh;
            }
            generatedMesh.Clear();
        }

        tessExterno = new Tess();
        tessInterno = new Tess();
        var tessPiso = new Tess();
        var tessTechos = new Tess();
        tessPiso.NoEmptyPolygons = tessTechos.NoEmptyPolygons = 
        tessExterno.NoEmptyPolygons = tessInterno.NoEmptyPolygons = true;

        foreach (var seccion in secciones)
        {
            var externo = 0f;
            var interno = 0f;
            if (seccion.categoria == LayoutCuarto.Categoria.Grande)
            {
                externo = bordeExternoGrande;
                interno = bordeInternoGrande;
            }
            else if (seccion.categoria == LayoutCuarto.Categoria.Peque)
            {
                externo = bordeExternoChico;
                interno = bordeInternoChico;
            }
            foreach (var cuarto in seccion.cuartosPropios)
            {
                tessExterno.AddContour(BoxColliderToContour(cuarto.BoxCol, externo), ContourOrientation.Clockwise);
                tessInterno.AddContour(BoxColliderToContour(cuarto.BoxCol, interno), ContourOrientation.Clockwise);

                tessPiso.AddContour(BoxColliderToContour(cuarto.BoxCol, interno), ContourOrientation.Clockwise);
                // tessTechos.AddContour(BoxColliderToContour(cuarto.BoxCol, externo), ContourOrientation.Clockwise);
                // tessTechos.AddContour(BoxColliderToContour(cuarto.BoxCol, interno), ContourOrientation.CounterClockwise);
            }
        }
        foreach(var puerta in vinculos.SelectMany(vinc=>vinc.puertas)) {
            var puertaPath = CajaDiam(puerta,diametroPuerta*Vector2.one,0);
            tessInterno.AddContour(Vector3ToContour(puertaPath), ContourOrientation.Clockwise);

            tessPiso.AddContour(Vector3ToContour(puertaPath), ContourOrientation.Clockwise);
            // tessTechos.AddContour(Vector3ToContour(puertaPath), ContourOrientation.CounterClockwise);
        }

        UnityEngine.Profiling.Profiler.BeginSample("Generando Tesselacion principal");
        tessInterno.Tessellate(WindingRule.Positive, ElementType.BoundaryContours, 8, null);
        tessExterno.Tessellate(WindingRule.Positive, ElementType.BoundaryContours, 8, null);
        UnityEngine.Profiling.Profiler.EndSample();

        if (colliderGlobal)
        {
            if (Application.isPlaying) Destroy(colliderGlobal.gameObject);
#if UNITY_EDITOR
            else DestroyImmediate(colliderGlobal.gameObject);
#endif
        }

        if (generarCollider2d) {
            colliderGlobal = new GameObject("collider global").AddComponent<PolygonCollider2D>();
            colliderGlobal.transform.parent = transform;

            colliderGlobal.pathCount = tessExterno.ElementCount+tessInterno.ElementCount;
        }

        var vertices2 = tessExterno.Vertices.Select(cont => new Vector2(cont.Position.X, cont.Position.Y));

        var meshVerts = vertices2.Select(vert=>new Vector3(vert.x,0f,vert.y)).Concat(vertices2.Select(vert=>new Vector3(vert.x,1f,vert.y)));
        meshVerts = meshVerts.Concat(meshVerts);

        List<int> tris = new List<int>();
        
        for (int i = 0; i < tessExterno.ElementCount; i++)
        {
            var baseIndex = tessExterno.Elements[i * 2];
            var count = tessExterno.Elements[i * 2 + 1];
            if (generarCollider2d && colliderGlobal) colliderGlobal.SetPath(i, vertices2.Skip(baseIndex).Take(count).ToArray());

            for (int v=0; genExterior && v<count; v++) {
                int v2 = (v+1)%count;
                tris.Add(v+baseIndex);
                tris.Add(v2+baseIndex+tessExterno.VertexCount*3);
                tris.Add(v2+baseIndex+tessExterno.VertexCount*2);
                tris.Add(v+baseIndex+tessExterno.VertexCount);
                tris.Add(v2+baseIndex+tessExterno.VertexCount*3);
                tris.Add(v+baseIndex);
            }
        }

        vertices2 = tessInterno.Vertices.Select(cont => new Vector2(cont.Position.X, cont.Position.Y));
        var meshVerts2 = vertices2.Select(vert=>new Vector3(vert.x,0f,vert.y)).Concat(vertices2.Select(vert=>new Vector3(vert.x,1f,vert.y)));
        meshVerts = meshVerts.Concat( meshVerts2.Concat(meshVerts2) );
        for (int i = 0; i < tessInterno.ElementCount; i++)
        {
            var baseIndex = tessInterno.Elements[i * 2];
            var count = tessInterno.Elements[i * 2 + 1];
            if (generarCollider2d && colliderGlobal) colliderGlobal.SetPath(i+tessExterno.ElementCount, vertices2.Skip(baseIndex).Take(count).ToArray());
            
            baseIndex += tessExterno.VertexCount*4;
            for (int v=0; genInterior && v<count; v++) {
                int v2 = (v+1)%count;
                tris.Add(v+baseIndex);
                tris.Add(v2+baseIndex+tessInterno.VertexCount*2);
                tris.Add(v2+baseIndex+tessInterno.VertexCount*3);
                tris.Add(v2+baseIndex+tessInterno.VertexCount*3);
                tris.Add(v+baseIndex+tessInterno.VertexCount);
                tris.Add(v+baseIndex);
            }
        }
        
        if (generatedMesh && generateMesh) {
            UnityEngine.Profiling.Profiler.BeginSample("Produciendo Mesh");

            tessPiso.Tessellate(WindingRule.Positive, ElementType.Polygons);
            var meshVerts3 = tessPiso.Vertices.Select(cont=>new Vector3(cont.Position.X, 0f, cont.Position.Y));
            int baseIndex = meshVerts.Count();
            if (genPiso) tris.AddRange( tessPiso.Elements.Select(index=>index+baseIndex).Reverse() );
            meshVerts = meshVerts.Concat(meshVerts3);

        // lo que se hizo para el techo seguro que se podria hacer para el piso y seria mas facil creooo
            var contornos = Enumerable.Repeat(new ContourVertex[0],tessInterno.ElementCount)
                .Select( (_,index)=>tessInterno.Vertices.Skip(tessInterno.Elements[index*2]).Take(tessInterno.Elements[index*2+1]) );
            foreach (var cont in contornos)tessTechos.AddContour(cont.ToArray(), ContourOrientation.CounterClockwise);
            
            contornos = Enumerable.Repeat(new ContourVertex[0],tessExterno.ElementCount)
                .Select( (_,index)=>tessExterno.Vertices.Skip(tessExterno.Elements[index*2]).Take(tessExterno.Elements[index*2+1]) );
            foreach (var cont in contornos)tessTechos.AddContour(cont.ToArray(), ContourOrientation.Clockwise);
            // tessTechos.Tessellate(WindingRule.Positive, ElementType.Polygons);

            tessTechos.Tessellate(WindingRule.EvenOdd, ElementType.Polygons);
            var meshVerts4 = tessTechos.Vertices.Select(cont=>new Vector3(cont.Position.X, 1f, cont.Position.Y));
            baseIndex = meshVerts.Count();
            if (genTechos) tris.AddRange( tessTechos.Elements.Select(index=>index+baseIndex).Reverse() );
            meshVerts = meshVerts.Concat(meshVerts4);

            generatedMesh.vertices = meshVerts.ToArray();
            generatedMesh.triangles = tris.ToArray();
            generatedMesh.RecalculateNormals();
            generatedMesh.RecalculateBounds();
            
            UnityEngine.Profiling.Profiler.EndSample();
        }
        UnityEngine.Profiling.Profiler.EndSample();
    }

    public void GenerarAMano(Vector2[] posGrandes, Vector2[] tamGrandes, Vector2[] posPeques, Vector2[] tamPeques, Vector2[] puertas, float diametroPuerta=-1f) {
        if (diametroPuerta < 0f) diametroPuerta = arbol.OverlapMinimoParaConectar;
        UnityEngine.Profiling.Profiler.BeginSample("Generando Collider Global");
        if (generateMesh) {
            if (!genMeshFilter) {
                var go = new GameObject("generated mesh");
                go.transform.SetParent(transform,false);
                genMeshFilter = go.AddComponent<MeshFilter>();
                go.AddComponent<MeshRenderer>();
            }
            if (!generatedMesh) {
                generatedMesh = new Mesh();
                genMeshFilter.sharedMesh = generatedMesh;
            }
            generatedMesh.Clear();
        }

        tessExterno = new Tess();
        tessInterno = new Tess();
        var tessPiso = new Tess();
        var tessTechos = new Tess();
        tessPiso.NoEmptyPolygons = tessTechos.NoEmptyPolygons = 
        tessExterno.NoEmptyPolygons = tessInterno.NoEmptyPolygons = true;

        for (int i=0; i<posGrandes.Length+posPeques.Length; i++)
        {
            var externo = 0f;
            var interno = 0f;
            if (i<posGrandes.Length)
            {
                externo = bordeExternoGrande;
                interno = bordeInternoGrande;
            }
            else
            {
                externo = bordeExternoChico;
                interno = bordeInternoChico;
            }
            // foreach (var cuarto in seccion.cuartosPropios)
            {
                var pos = i<posGrandes.Length?posGrandes[i]:posPeques[i-posGrandes.Length];
                var tam = i<tamGrandes.Length?tamGrandes[i]:tamPeques[i-tamGrandes.Length];

                tessExterno.AddContour(RectToContour(pos, tam, externo), ContourOrientation.Clockwise);
                tessInterno.AddContour(RectToContour(pos, tam, interno), ContourOrientation.Clockwise);

                tessPiso.AddContour(RectToContour(pos, tam, interno), ContourOrientation.Clockwise);
                // tessTechos.AddContour(BoxColliderToContour(cuarto.BoxCol, externo), ContourOrientation.Clockwise);
                // tessTechos.AddContour(BoxColliderToContour(cuarto.BoxCol, interno), ContourOrientation.CounterClockwise);
            }
        }
        // foreach(var puerta in vinculos.SelectMany(vinc=>vinc.puertas))
        foreach(var puerta in puertas)
        {
            var puertaPath = CajaDiam(puerta,diametroPuerta*Vector2.one,0);
            tessInterno.AddContour(Vector3ToContour(puertaPath), ContourOrientation.Clockwise);

            tessPiso.AddContour(Vector3ToContour(puertaPath), ContourOrientation.Clockwise);
            // tessTechos.AddContour(Vector3ToContour(puertaPath), ContourOrientation.CounterClockwise);
        }

        UnityEngine.Profiling.Profiler.BeginSample("Generando Tesselacion principal");
        tessInterno.Tessellate(WindingRule.Positive, ElementType.BoundaryContours, 8, null);
        tessExterno.Tessellate(WindingRule.Positive, ElementType.BoundaryContours, 8, null);
        UnityEngine.Profiling.Profiler.EndSample();

        if (colliderGlobal)
        {
            if (Application.isPlaying) Destroy(colliderGlobal.gameObject);
#if UNITY_EDITOR
            else DestroyImmediate(colliderGlobal.gameObject);
#endif
        }

        if (generarCollider2d) {
            colliderGlobal = new GameObject("collider global").AddComponent<PolygonCollider2D>();
            colliderGlobal.transform.parent = transform;

            colliderGlobal.pathCount = tessExterno.ElementCount+tessInterno.ElementCount;
        }

        var vertices2 = tessExterno.Vertices.Select(cont => new Vector2(cont.Position.X, cont.Position.Y));

        var meshVerts = vertices2.Select(vert=>new Vector3(vert.x,0f,vert.y)).Concat(vertices2.Select(vert=>new Vector3(vert.x,1f,vert.y)));
        meshVerts = meshVerts.Concat(meshVerts);

        List<int> tris = new List<int>();
        
        for (int i = 0; i < tessExterno.ElementCount; i++)
        {
            var baseIndex = tessExterno.Elements[i * 2];
            var count = tessExterno.Elements[i * 2 + 1];
            if (generarCollider2d && colliderGlobal) colliderGlobal.SetPath(i, vertices2.Skip(baseIndex).Take(count).ToArray());

            for (int v=0; genExterior && v<count; v++) {
                int v2 = (v+1)%count;
                tris.Add(v+baseIndex);
                tris.Add(v2+baseIndex+tessExterno.VertexCount*3);
                tris.Add(v2+baseIndex+tessExterno.VertexCount*2);
                tris.Add(v+baseIndex+tessExterno.VertexCount);
                tris.Add(v2+baseIndex+tessExterno.VertexCount*3);
                tris.Add(v+baseIndex);
            }
        }

        vertices2 = tessInterno.Vertices.Select(cont => new Vector2(cont.Position.X, cont.Position.Y));
        var meshVerts2 = vertices2.Select(vert=>new Vector3(vert.x,0f,vert.y)).Concat(vertices2.Select(vert=>new Vector3(vert.x,1f,vert.y)));
        meshVerts = meshVerts.Concat( meshVerts2.Concat(meshVerts2) );
        for (int i = 0; i < tessInterno.ElementCount; i++)
        {
            var baseIndex = tessInterno.Elements[i * 2];
            var count = tessInterno.Elements[i * 2 + 1];
            if (generarCollider2d && colliderGlobal) colliderGlobal.SetPath(i+tessExterno.ElementCount, vertices2.Skip(baseIndex).Take(count).ToArray());
            
            baseIndex += tessExterno.VertexCount*4;
            for (int v=0; genInterior && v<count; v++) {
                int v2 = (v+1)%count;
                tris.Add(v+baseIndex);
                tris.Add(v2+baseIndex+tessInterno.VertexCount*2);
                tris.Add(v2+baseIndex+tessInterno.VertexCount*3);
                tris.Add(v2+baseIndex+tessInterno.VertexCount*3);
                tris.Add(v+baseIndex+tessInterno.VertexCount);
                tris.Add(v+baseIndex);
            }
        }
        
        if (generatedMesh && generateMesh) {
            UnityEngine.Profiling.Profiler.BeginSample("Produciendo Mesh");

            tessPiso.Tessellate(WindingRule.Positive, ElementType.Polygons);
            var meshVerts3 = tessPiso.Vertices.Select(cont=>new Vector3(cont.Position.X, 0f, cont.Position.Y));
            int baseIndex = meshVerts.Count();
            if (genPiso) tris.AddRange( tessPiso.Elements.Select(index=>index+baseIndex).Reverse() );
            meshVerts = meshVerts.Concat(meshVerts3);

        // lo que se hizo para el techo seguro que se podria hacer para el piso y seria mas facil creooo
            var contornos = Enumerable.Repeat(new ContourVertex[0],tessInterno.ElementCount)
                .Select( (_,index)=>tessInterno.Vertices.Skip(tessInterno.Elements[index*2]).Take(tessInterno.Elements[index*2+1]) );
            foreach (var cont in contornos)tessTechos.AddContour(cont.ToArray(), ContourOrientation.CounterClockwise);
            
            contornos = Enumerable.Repeat(new ContourVertex[0],tessExterno.ElementCount)
                .Select( (_,index)=>tessExterno.Vertices.Skip(tessExterno.Elements[index*2]).Take(tessExterno.Elements[index*2+1]) );
            foreach (var cont in contornos)tessTechos.AddContour(cont.ToArray(), ContourOrientation.Clockwise);
            // tessTechos.Tessellate(WindingRule.Positive, ElementType.Polygons);

            tessTechos.Tessellate(WindingRule.EvenOdd, ElementType.Polygons);
            var meshVerts4 = tessTechos.Vertices.Select(cont=>new Vector3(cont.Position.X, 1f, cont.Position.Y));
            baseIndex = meshVerts.Count();
            if (genTechos) tris.AddRange( tessTechos.Elements.Select(index=>index+baseIndex).Reverse() );
            meshVerts = meshVerts.Concat(meshVerts4);

            generatedMesh.vertices = meshVerts.ToArray();
            generatedMesh.triangles = tris.ToArray();
            generatedMesh.RecalculateNormals();
            generatedMesh.RecalculateBounds();
            
            UnityEngine.Profiling.Profiler.EndSample();
        }
        UnityEngine.Profiling.Profiler.EndSample();
    }

    public ContourVertex[] Vector3ToContour(Vector3[] vec3list) => vec3list.Select(v => new Vec3(v.x, v.y, v.z)).Select(v => new ContourVertex(v)).ToArray();
    public Vector3[] CajaDiam(Vector2 offset, Vector2 tam, float margen) {
        var deltaX = tam.x/2f + margen;
        var deltaY = tam.y/2f + margen;
        return new Vector3[]{
            new Vector3(offset.x+deltaX,offset.y+deltaY,0f),
            new Vector3(offset.x-deltaX,offset.y+deltaY,0f),
            new Vector3(offset.x-deltaX,offset.y-deltaY,0f),
            new Vector3(offset.x+deltaX,offset.y-deltaY,0f),
        };
    }
    public ContourVertex[] BoxColliderToContour(BoxCollider2D box, float margen)
    {
        var salida = CajaDiam(box.offset,box.size, margen);

        return salida.Select(v => box.transform.TransformPoint(v))
                .Select(v => new Vec3(v.x, v.y, v.z))
                .Select(v => new ContourVertex(v)).ToArray();
    }
    
    public ContourVertex[] RectToContour(Vector2 offset, Vector2 tam, float margen)
    {
        var salida = CajaDiam(offset, tam, margen);

        return salida//.Select(v => box.transform.TransformPoint(v))
                .Select(v => new Vec3(v.x, v.y, v.z))
                .Select(v => new ContourVertex(v)).ToArray();
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(GeneradorColliderGlobal))]
    public class GeneradorColliderGlobalEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var gen = target as GeneradorColliderGlobal;
            if (GUILayout.Button("Generar"))
            {
                gen.Generar();
            }
            if (gen.tessExterno != null) {
                GUILayout.Label("Externo");
                EditorGUILayout.IntField("- ElementCount",gen.tessExterno.ElementCount);
                EditorGUILayout.IntField("- VertexCount",gen.tessExterno.VertexCount);
                GUILayout.Label("Interno");
                EditorGUILayout.IntField("- ElementCount",gen.tessInterno.ElementCount);
                EditorGUILayout.IntField("- VertexCount",gen.tessInterno.VertexCount);
            }
        }
    }
#endif
}
