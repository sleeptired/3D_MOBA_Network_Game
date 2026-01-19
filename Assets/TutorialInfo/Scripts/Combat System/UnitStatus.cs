using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Qos.V2.Models;
using UnityEngine;

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

    public override void OnNetworkSpawn()
    {
        CurrentState.OnValueChanged += (oldVal, newVal) => OnStateChanged?.Invoke(newVal);
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
