using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Guazu.DrawersCopados;

[DisallowMultipleComponent]
public class WachinLogica : MonoBehaviour
{
    public float maxVel = 10;
    public float acel = 50;
    public float factorCorre = 1.5f;
    public LayerMask wallLayers;
    [AnimatorStringList(AnimatorStringListAttribute.Tipo.Parametros)]
    public string rifleAnimBool;
    [AnimatorStringList(AnimatorStringListAttribute.Tipo.Parametros)]
    public string caminaAnimBool;

    public bool Rifle
    {
        get => Animator ? Animator.GetBool(rifleAnimBool) : false;
        set
        {
            if (Animator && value!=Rifle)
            {
                Agent.speed = value?maxVel:maxVel*factorCorre;
                Animator.SetBool(rifleAnimBool, value);
                Animator.Update(0f);
            }
        }
    }

    public Vector3 vel;
    Vector3 _posBuscada;
    public Vector3 PosBuscada {
        get => _posBuscada;
        set {
            _posBuscada = value;
            if (Agent) Agent.destination = _posBuscada;
        }
    }

    public Vector3 miraHacia = Vector3.zero;

    RaycastHit hit;
    Rigidbody _rigid;
    Rigidbody Rigid => _rigid ? _rigid : _rigid = GetComponent<Rigidbody>();

    NavMeshAgent _agent;
    public NavMeshAgent Agent => _agent?_agent:_agent=GetComponent<NavMeshAgent>();

    Animator _animator;
    Animator Animator => _animator ? _animator : _animator = GetComponent<Animator>();

    ItemActivo _itemActivo;
    public ItemActivo ItemActivo => _itemActivo ? _itemActivo : _itemActivo = GetComponentInChildren<ItemActivo>();

    void Start() {
        if (Agent) {
            Agent.updateRotation = false;
            Agent.speed = maxVel*factorCorre;
            Agent.acceleration = acel;
        }
    }

    void Update()
    {
        if (Animator)
        {
            if (Rigid) Animator.SetBool(caminaAnimBool, Rigid.velocity.magnitude > 0.1f);
            else if (Agent) Animator.SetBool(caminaAnimBool, Agent.velocity.magnitude > .1f);
        }

        var mira = Vector3.ProjectOnPlane(miraHacia, transform.up);
        transform.LookAt(mira, transform.up);
    }

    void FixedUpdate()
    {
        if (!Rigid) return;
        var dt = Time.inFixedTimeStep ? Time.fixedDeltaTime : Time.deltaTime;

        var velBuscada = Vector3.MoveTowards(Vector3.zero, PosBuscada - transform.position, maxVel);
        if (acel == 0) vel = velBuscada;
        else vel = Vector3.MoveTowards(vel, velBuscada, acel * dt);

        var currentMove = vel * dt;
        var moveDist = currentMove.magnitude;

        // if (Physics.SphereCast(transform.position, radio, currentMove, out hit, currentMove.magnitude, wallLayers))
        // if (Rigid.SweepTest(currentMove,out hit, moveDist))
        // {
        //     var slide = Vector3.MoveTowards(currentMove, Vector3.zero, moveDist - hit.distance);
        //     currentMove -= slide;
        //     currentMove += Vector3.ProjectOnPlane(slide, hit.normal);
        // }
        if (Rigid)
        {
            // Rigid.velocity = currentMove / dt;
            Rigid.velocity = vel;
        }
        else
        {
            transform.position += currentMove;
        }
    }
}
