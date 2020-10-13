using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[DefaultExecutionOrder(100)]
public class RenderFar : MonoBehaviour
{
    static Dictionary<Sprite,Mesh> spriteToMesh = new Dictionary<Sprite, Mesh>();
    static Mesh GetMesh(Sprite sprite) {
        if (!sprite) return null;
        if (!spriteToMesh.ContainsKey(sprite)) {
            var mesh = new Mesh();
            mesh.vertices = sprite.vertices.Select(v=>(Vector3)v).ToArray();
            mesh.uv = sprite.uv;
            mesh.triangles = sprite.triangles.Select(uv=>(int)uv).ToArray();
            mesh.colors = Enumerable.Repeat(Color.gray, mesh.vertices.Length).ToArray();
            spriteToMesh.Add(sprite,mesh);
        }
        return spriteToMesh[sprite];
    }

    public float distScale = 0.1f;//deberia ser mas una "resta" a la distancia
    public float distResta = 10f;
    public float pow = 2f;
    public float zScale = 0.25f;

    SpriteRenderer _sr;
    SpriteRenderer SR => _sr?_sr:_sr=GetComponent<SpriteRenderer>();
    MaterialPropertyBlock block;
    
    // Update is called once per frame
    void LateUpdate()
    {
        if (SR && !SR.isVisible) {
            var camPos = Camera.main.transform.position;
            var dist = Vector2.Distance(camPos,transform.position)*distScale-distResta;

            var matriz = transform.localToWorldMatrix;
            matriz[2,3] = Mathf.Pow(dist,pow)*zScale;

            if (block == null) {
                SR.GetPropertyBlock(block = new MaterialPropertyBlock());
            }

            Graphics.DrawMesh(GetMesh(SR.sprite),matriz,SR.sharedMaterial,gameObject.layer,null,0,block);

        }
    }
}
