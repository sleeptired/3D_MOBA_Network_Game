using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Qos.V2.Models;
using UnityEngine;
using UnityEngine.AI;

public class UnitStatus : NetworkBehaviour
{
    public enum StateFlags //각 자리수가 상태를 의미하는 방법, 비트 플래그 방식
    {
        None = 0,
        Idle = 1 << 0,
        Moving = 1 << 1,
        Stunned = 1 << 3,   // 기절 
        Slowed = 1 << 4,    // 둔화 
        Dead = 1 << 5
    }

    public NetworkVariable<StateFlags> CurrentState = new NetworkVariable<StateFlags>(StateFlags.Idle);

    // 상태 변경 시 UI나 로직에 알리기 위한 이벤트
    public event Action<StateFlags> OnStateChanged;
    private NavMeshAgent _agent;
    private float _originalSpeed = 0f;

    private void Awake()//상태이상 추가
    {
        _agent = GetComponent<NavMeshAgent>();
    }

    private void Start()//상태이상 추가
    {
        if (_agent != null)
        {
            // NavMeshAgent의 Speed 값 여기에 저장.
            _originalSpeed = _agent.speed;;
        }
    }

    public override void OnNetworkSpawn()
    {
        CurrentState.OnValueChanged += (oldVal, newVal) => OnStateChanged?.Invoke(newVal);
    }
    void Update()//상태이상 추가 
    {
        if (_agent == null) return;

        // 기절
        if (HasState(StateFlags.Stunned) || HasState(StateFlags.Dead))
        {
            _agent.isStopped = true;
            _agent.velocity = Vector3.zero;
        }
        else
        {
            _agent.isStopped = false;
        }

        // 둔화 처리
        if (HasState(StateFlags.Slowed) && !HasState(StateFlags.Stunned))
        {
            // 속도 감소
            _agent.speed = _originalSpeed * 0.5f;
        }
        else
        {
            // 속도 복구
            _agent.speed = _originalSpeed;
        }
    }




    // 상태 적용
    public void AddState(StateFlags state) { if (IsServer) CurrentState.Value |= state; }
    public void RemoveState(StateFlags state) { if (IsServer) CurrentState.Value &= ~state; }
    public bool HasState(StateFlags state) => (CurrentState.Value & state) != 0;

    // 일정 시간 상태 적용
    [ServerRpc(RequireOwnership = false)]
    public void ApplyTemporaryStateServerRpc(StateFlags state, float duration)
    {
        StartCoroutine(ProcessTemporaryState(state, duration));
    }

    private IEnumerator ProcessTemporaryState(StateFlags state, float duration)
    {
        AddState(state);
        yield return new WaitForSeconds(duration);
        RemoveState(state);
    }

    public bool CanMove() => !HasState(StateFlags.Stunned | StateFlags.Dead);
    public bool CanSkill() => !HasState(StateFlags.Stunned | StateFlags.Dead);

}
