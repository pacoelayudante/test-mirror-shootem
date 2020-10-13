using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class SimpleFXs : MonoBehaviour
{
    [SerializeField] bool randomizeRotation = true;

    Animator _animator;
    public Animator Animator => _animator ? _animator : _animator = GetComponent<Animator>();
    
    void Awake() {
        if (randomizeRotation) transform.rotation = Quaternion.Euler(0,0,Random.value*360f);
    }

    void Update() {
        if (Animator) {
            if (Animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 1f) {
                Destroy(gameObject);
            }
        }
    }
}
