using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FxRandomizer : MonoBehaviour
{

    public bool randomizeFlipX,randomizeFlipY;
    public Vector3 rotateRandomAroundAxis = Vector3.zero;

    private void OnEnable() {
        if (randomizeFlipX||randomizeFlipY) {
            foreach(var sr in GetComponentsInChildren<SpriteRenderer>()) {
                if (randomizeFlipX) sr.flipX = Random.value>.5f;
                if (randomizeFlipY) sr.flipY = Random.value>.5f;
            }
        }
        if (rotateRandomAroundAxis != Vector3.zero) {
            transform.Rotate(rotateRandomAroundAxis,Random.value*360f,Space.Self);
        }
    }

}
