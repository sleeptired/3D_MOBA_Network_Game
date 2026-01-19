using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillSlotUI : MonoBehaviour
{
    [Header("UI 컴포넌트 연결")]
    public Image iconImage;           // 스킬 아이콘
    public Image cooldownOverlay;     // 어두운 막 (Filled 타입)
    public TextMeshProUGUI timerText; // 남은 시간 숫자

    private float _maxCooldown;
    private float _currentCooldown;

    // 초기화: 아이콘 설정
    public void SetSkill(SkillData data)
    {
        if (data != null && data.icon != null)
        {
            iconImage.sprite = data.icon;
            iconImage.enabled = true;
        }
        else
        {
            // 아이콘 없으면 투명하게 처리하거나 기본 이미지
            iconImage.enabled = false;
        }

        // 시작할 때 쿨타임 UI 숨기기
        cooldownOverlay.fillAmount = 0;
        timerText.text = "";
    }

    // 외부(Player)에서 "스킬 썼다!"고 알려주는 함수
    public void UseSkill(float cooldownTime)
    {
        _maxCooldown = cooldownTime;
        _currentCooldown = cooldownTime;

        // UI 켜기
        cooldownOverlay.fillAmount = 1; // 꽉 채움
    }

    void Update()
    {
        // 쿨타임이 돌고 있을 때만 실행
        if (_currentCooldown > 0)
        {
            _currentCooldown -= Time.deltaTime;

            // 1. 어두운 막 줄어들게 하기 (0~1 사이 값)
            cooldownOverlay.fillAmount = _currentCooldown / _maxCooldown;

            // 2. 남은 시간 텍스트 표시 (소수점 1자리)
            // 0.5초 ... 0.1초 ... 0.0초
            if (_currentCooldown > 0)
                timerText.text = _currentCooldown.ToString("F1");
            else
                timerText.text = ""; // 끝났으면 텍스트 지움
        }
        else
        {
            // 확실하게 0으로 마무리
            cooldownOverlay.fillAmount = 0;
            timerText.text = "";
        }
    }
}
