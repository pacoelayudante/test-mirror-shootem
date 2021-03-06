﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Guazu.DrawersCopados;
using Mirror;

[DisallowMultipleComponent]
public class WachinLogica : NetworkBehaviour
{
    public float maxVel = 10;

    [SerializeField] float acel = 50;
    [SerializeField] float factorCorre = 1.5f;
    [SerializeField] LayerMask wallLayers;
    [AnimatorStringList(AnimatorStringListAttribute.Tipo.Parametros)]
    [SerializeField] string rifleAnimBool;
    [AnimatorStringList(AnimatorStringListAttribute.Tipo.Parametros)]
    [SerializeField] string caminaAnimBool;
    [AnimatorStringList(AnimatorStringListAttribute.Tipo.Parametros)]
    [SerializeField] string rollAnimTrigger;
    [AnimatorStringList(AnimatorStringListAttribute.Tipo.Parametros)]
    [SerializeField] string dirAnimInt;
    [AnimatorStringList(AnimatorStringListAttribute.Tipo.Parametros)]
    [SerializeField] string rollDurAnimFloat;
    [AnimatorStringList(AnimatorStringListAttribute.Tipo.Parametros)]
    [SerializeField] string cabezaMiraAnimBool;
    [AnimatorStringList(AnimatorStringListAttribute.Tipo.Parametros)]
    [SerializeField] string noqueadeAnimBool;
    [SerializeField] float rollDuration = .4f;
    [SerializeField] float rollDistance = 1.5f;
    [SerializeField] AnimationCurve rollCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [Header("Predicciones")]
    public float distPrediccionMaxima = 0.2f;
    public float distPegarSalto = 0.6f;

    public bool Rifle
    {
        get => Animator ? Animator.GetBool(rifleAnimBool) : false;
        set
        {
            // if (hasAuthority) CmdRifleSet(value);
            if (Animator && value != Rifle)
            {
                Agent.speed = value ? MaxVel : MaxVel * factorCorre;
                Animator.SetBool(rifleAnimBool, value);
                Animator.Update(0f);// malisimo, no deberia depender de la animacion la posicion de la salida del tiro, pero bue
                RpcRifleSet(value);
            }
        }
    }

    public float MaxVel
    {
        get => maxVel;
        set
        {
            maxVel = value;
            if (Agent) Agent.speed = maxVel;
        }
    }

    [SerializeField]
    bool _isRolling;
    public bool IsRolling => _isRolling;

    public bool puedeRollar = true;
    bool noqueade = false;
    public bool Noqueade {
        get => noqueade;
        set {
            noqueade = value;
            if(Collider)Collider.enabled = !noqueade;
            RpcNoqueadeSet(noqueade);
        }
    }

    // [Command]
    // void CmdRifleSet(bool value) {
    //     Agent.speed = value ? maxVel : maxVel * factorCorre;
    //     // RpcRifleSet(value);

    //         if (Animator && value != Rifle)
    //         {
    //             Agent.speed = value ? maxVel : maxVel * factorCorre;
    //             // Animator.SetBool(rifleAnimBool, value);
    //             // Animator.Update(0f);
    //         }
    // }
    [ClientRpc]
    void RpcRifleSet(bool value)
    {
        if (Animator && value != Rifle)
        {
            Animator.SetBool(rifleAnimBool, value);
            Animator.Update(0f);
        }
    }
    [ClientRpc]
    void RpcNoqueadeSet(bool value)
    {
        if(Animator)Animator.SetBool(noqueadeAnimBool, value);
    }

    public Vector3 vel;
    Vector3 _posBuscada;
    public Vector3 PosBuscada
    {
        get => _posBuscada;
        set
        {
            if (!isServer) return;
            _posBuscada = value;
            if (Agent) Agent.destination = _posBuscada;
        }
    }

    Vector3 _lastSentMovDir = Vector3.zero;
    public Vector3 MovDir
    {
        get => Agent.velocity.normalized;
        set
        {
            if (hasAuthority && _lastSentMovDir != value) CmdMovDirSet(_lastSentMovDir = value);
            // if (IsRolling) return;
            // if(Agent.hasPath) Agent.ResetPath();
            // Agent.velocity = value*Agent.speed;
        }
    }

    double lastReceivedPosT,prevToLastPosT;
    ulong lastSentPos, lastReceivedPos, prevToLastPos;
    byte lastSentRot;
    Vector3 _movIntent;
    [Command]
    void CmdMovDirSet(Vector3 value)
    {
        _movIntent = value;
        if (IsRolling || noqueade) return;
        if (Agent.hasPath) Agent.ResetPath();
        Agent.velocity = _movIntent * Agent.speed;
    }

    Vector3 _lastSentMirar = Vector3.zero;
    public Vector3 MiraHacia
    {
        get => _mirarHacia;
        set
        {
            if (hasAuthority && _lastSentMirar != value)
            {
                CmdMirarHacia(_lastSentMirar = value);
            }
            else if (isServer) _mirarHacia = value;
        }
    }

    [SerializeField]
    Vector3 _mirarHacia = Vector3.zero;
    [Command]
    void CmdMirarHacia(Vector3 value)
    {
        _mirarHacia = value;
    }

    Coroutine rollCor;
    RaycastHit hit;
    Rigidbody _rigid;
    Rigidbody Rigid => _rigid ? _rigid : _rigid = GetComponent<Rigidbody>();

    Collider _collider;
    Collider Collider => _collider ? _collider : _collider = GetComponent<Collider>();

    NavMeshAgent _agent;
    public NavMeshAgent Agent => _agent ? _agent : _agent = GetComponent<NavMeshAgent>();

    Animator _animator;
    public Animator Animator => _animator ? _animator : _animator = GetComponent<Animator>();

    ItemActivo _itemActivo;
    public ItemActivo ItemActivo => _itemActivo ? _itemActivo : _itemActivo = GetComponentInChildren<ItemActivo>();

    [ServerCallback]
    void Start()
    {
        if (Agent)
        {
            if (isServer)
            {
                Agent.enabled = true;
                Agent.updateRotation = false;
                Agent.speed = MaxVel * factorCorre;
                Agent.acceleration = acel;
            }
        }
    }

    [ClientCallback]
    void Awake()
    {
    }

    // [ServerCallback]
    void Update()
    {
        if (!isServer) {
            PositionPrediction();
            return;
        }

        var mira = Vector3.ProjectOnPlane(_mirarHacia, transform.up);
        mira.y = transform.position.y;
        // transform.LookAt(mira, transform.up);

        if (!IsRolling && !Agent.hasPath && !noqueade) Agent.velocity = _movIntent * Agent.speed;

        // if (Animator)
        // {
        //     if (Rigid) Animator.SetBool(caminaAnimBool, Rigid.velocity.magnitude > 0.2f);
        //     else if (Agent) Animator.SetBool(caminaAnimBool, Agent.velocity.magnitude > .2f);
        // }

        var viewAngle = Vector3.SignedAngle(Vector3.forward, mira - transform.position, Vector3.up) + 180f;
        var currentPosIndex = RondaActual.actual.GetPositionIndex(transform.position);
        var currentAngleByte = (byte)Mathf.RoundToInt(256 * viewAngle / 360f);
        if (currentPosIndex != lastSentPos || currentAngleByte != lastSentRot) RpcUpdatePos(lastSentPos = currentPosIndex, lastSentRot = currentAngleByte);
    }

    Vector3 lastPos;
    [ClientCallback]
    private void LateUpdate()
    {
        if (Animator)
        {
            Animator.SetBool(caminaAnimBool, lastPos != transform.position);
        }
        lastPos = transform.position;
    }

    [Client]
    void PositionPrediction() {
        if (lastReceivedPosT==prevToLastPosT) return;
        var posVieja = RondaActual.actual.GetIndexedPosition(prevToLastPos);
        var posNecesaria = RondaActual.actual.GetIndexedPosition(lastReceivedPos);
        var posDiff = (posNecesaria-posVieja);
        var posPredicta = Vector3.MoveTowards(posNecesaria, posNecesaria+posDiff, distPrediccionMaxima);

        var velEstimada = posDiff.magnitude/(lastReceivedPosT-prevToLastPosT);        
        
        var nuevaPos = Vector3.MoveTowards(posVieja, transform.position, distPegarSalto);
        nuevaPos = Vector3.MoveTowards(nuevaPos, posPredicta, (float)velEstimada*Time.deltaTime);
        transform.position = nuevaPos;
    }

    [ClientRpc(channel = 1)]
    void RpcUpdatePos(ulong posIndex, byte viewDir)
    {
        prevToLastPosT = lastReceivedPosT;
        lastReceivedPosT = Time.realtimeSinceStartup;

        prevToLastPos = lastReceivedPos;        
        lastReceivedPos = posIndex;

        transform.rotation = Quaternion.Euler(0f, 360f * viewDir / 256f - 180f, 0f);
        if (Animator) Animator.SetBool(cabezaMiraAnimBool, transform.forward.z > 0f);

        // if (Animator)
        // {
        //     Animator.SetBool(caminaAnimBool, lastReceivedPos!=posIndex);
        //     lastReceivedPos = posIndex;
        // }
        if (isServer) return;

        // transform.position = RondaActual.actual.GetIndexedPosition(posIndex);
    }

    public void Roll(Vector3 dir)
    {
        if (hasAuthority) CmdRoll(dir);
    }
    [Command]
    public void CmdRoll(Vector3 dir)
    {
        if (IsRolling || !puedeRollar || dir == Vector3.zero) return;
        StartCoroutine(DoRoll(dir));
    }

    IEnumerator DoRoll(Vector3 dir)
    {
        _isRolling = true;
        if (Collider) Collider.enabled = false;
        Agent.isStopped = true;
        Rifle = false;
        dir.Normalize();
        var rollT = 0f;
        var travelledDist = 0f;

        RpcRollAnim((byte)((dir.x < 0 ? 1 : 0) + (dir.z < 0 ? 2 : 0)));
        // if (Animator)
        // {
        //     Animator.SetTrigger(rollAnimTrigger);
        //     Animator.SetInteger(dirAnimInt, (dir.x < 0 ? 1 : 0) + (dir.z < 0 ? 2 : 0));
        //     Animator.SetFloat(rollDurAnimFloat, 1f/rollDuration);
        // }

        while (rollT < rollDuration)
        {
            var newDist = rollCurve.Evaluate(rollT / rollDuration);
            Agent.Move(dir * (newDist - travelledDist) * rollDistance);
            travelledDist = newDist;
            rollT += Time.deltaTime;
            yield return null;
        }
        _isRolling = false;
        Agent.isStopped = false;
        if (Collider) Collider.enabled = true;
        Agent.ResetPath();
    }

    [ClientRpc]
    void RpcRollAnim(byte dir)
    {
        if (Animator)
        {
            Animator.SetTrigger(rollAnimTrigger);
            Animator.SetInteger(dirAnimInt, dir);
            Animator.SetFloat(rollDurAnimFloat, 1f / rollDuration);
        }
    }

}
