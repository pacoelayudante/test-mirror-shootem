using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoscaStats : ScriptableObject
{
    public float maxHP = 5;
    public bool puedenDesagruparse = false;

    public int tamDeGrupo = 7;
    public float radioDeInfluencia = 4f;
    public float demasiadoCerca = .5f;

    [Range(0f, 1f)]
    public float alineacion = 1f, cohesion = 1f, separacion = 1f;

    public float velMaximia = 5f;
    public float rotMaxima = 90f;

    public ContactFilter2D pilotoContact;
    public float stunStrength = 1f;
}
