using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoscaTestManager : MonoBehaviour
{
    [SerializeField] Mosca _moscaPrefab = null;
    [SerializeField] int cantToSpawn = 100;
    [SerializeField] float radio = 10f;

    // Start is called before the first frame update
    void Start()
    {
        for (int i=0; i<cantToSpawn; i++) {
            Instantiate(_moscaPrefab, Quaternion.Euler(0,0,Random.value*360f)*Vector2.right*Random.Range(0,radio), Quaternion.identity);
        }
    }

}
