using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//유저의 상태이상을 표현하는 스크립트
public class UnitVisual : MonoBehaviour
{
    [Header("색상을 바꿀 모델")]
    public Renderer targetRenderer; // 플레이어의 몸통(Mesh)을 여기에 연결

    private UnitStatus _status;
    private Color _originalColor;   // 원래 색깔 저장소

    private void Awake()
    {
        _status = GetComponent<UnitStatus>();

        // 게임 시작 시 원래 색깔을 기억해둡니다. (나중에 복구해야 하니까)
        if (targetRenderer != null)
        {
            _originalColor = targetRenderer.material.color;
        }
    }

    private void Start()
    {
        // UnitStatus가 "나 상태 변했어!"라고 외칠 때(Event)를 듣기 위해 구독 신청
        if (_status != null)
        {
            _status.OnStateChanged += UpdateColor;
        }
    }

    private void OnDestroy()
    {
        // 오브젝트가 사라질 때 구독 취소 (메모리 누수 방지)
        if (_status != null)
        {
            _status.OnStateChanged -= UpdateColor;
        }
    }

    // 상태가 변할 때마다 호출되는 함수
    private void UpdateColor(UnitStatus.StateFlags newState)
    {
        if (targetRenderer == null) return;

        // 1. 기절 상태인지 확인 (비트 연산)
        bool isStunned = (newState & UnitStatus.StateFlags.Stunned) != 0;

        if (isStunned)
        {
            // 기절함 -> 빨간색으로 변경
            targetRenderer.material.color = Color.red;
        }
        else
        {
            // 기절 풀림 -> 둔화인지 확인
            bool isSlowed = (newState & UnitStatus.StateFlags.Slowed) != 0;

            if (isSlowed)
            {
                // (선택사항) 둔화면 파란색
                targetRenderer.material.color = Color.blue;
            }
            else
            {
                // 아무것도 아니면 원래 색 복구
                targetRenderer.material.color = _originalColor;
            }
        }
    }
}
