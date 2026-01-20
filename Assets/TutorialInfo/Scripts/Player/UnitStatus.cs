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
        Stunned = 1 << 3,   // 기절 (이동X, 스킬X)
        Slowed = 1 << 4,    // 둔화 (속도감소)
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
            _originalSpeed = _agent.speed;

            // (디버그용) 확인하고 싶으면 아래 주석을 풀어서 콘솔을 보세요.
            // Debug.Log($"초기 이동 속도 저장됨: {_originalSpeed}");
        }
    }

    public override void OnNetworkSpawn()
    {
        CurrentState.OnValueChanged += (oldVal, newVal) => OnStateChanged?.Invoke(newVal);
    }
    void Update()//상태이상 추가 (핵심 구현 부분)
    {
        if (_agent == null) return;

        // 1. 기절 (이동 정지)
        if (HasState(StateFlags.Stunned) || HasState(StateFlags.Dead))
        {
            _agent.isStopped = true;
            _agent.velocity = Vector3.zero;
        }
        else
        {
            _agent.isStopped = false;
        }

        // 2. 둔화 처리
        if (HasState(StateFlags.Slowed) && !HasState(StateFlags.Stunned))
        {
            // 아까 Start에서 가져온 7의 절반(3.5)으로 설정
            _agent.speed = _originalSpeed * 0.5f;
        }
        else
        {
            // 원래 속도(7)로 복구
            _agent.speed = _originalSpeed;
        }
    }




    // [상태 적용/해제/체크 로직]
    public void AddState(StateFlags state) { if (IsServer) CurrentState.Value |= state; }
    public void RemoveState(StateFlags state) { if (IsServer) CurrentState.Value &= ~state; }
    public bool HasState(StateFlags state) => (CurrentState.Value & state) != 0;

    // [일정 시간 상태 적용 (기절, 둔화 등)]
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
