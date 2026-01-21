using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class SkillManager : MonoBehaviour
{
    [Header("장착된 스킬 (데이터)")]
    public SkillData skillQ;
    public SkillData skillW;

    // 런타임 쿨타임 타이머
    private float _currentCoolQ = 0f;
    private float _currentCoolW = 0f;

    // UI에게 알림을 줄 이벤트
    public event Action<string, float> OnSkillUsed;

    void Update()
    {
        // 쿨타임 회복
        if (_currentCoolQ > 0) _currentCoolQ -= Time.deltaTime;
        if (_currentCoolW > 0) _currentCoolW -= Time.deltaTime;
    }

    // 쿨타임 확인 함수
    public bool IsReady(string skillType)
    {
        if (skillType == "Q") return _currentCoolQ <= 0;
        if (skillType == "W") return _currentCoolW <= 0;
        return false;
    }

    // 스킬 사용 처리 
    public void ApplyCooldown(string skillType)
    {
        if (skillType == "Q")
        {
            _currentCoolQ = skillQ.cooldown;
            OnSkillUsed?.Invoke("Q", skillQ.cooldown); // UI 업데이트 요청
        }
        else if (skillType == "W")
        {
            _currentCoolW = skillW.cooldown;
            OnSkillUsed?.Invoke("W", skillW.cooldown);
        }
    }

    // 데이터 접근용
    public SkillData GetSkill(string key) => key == "Q" ? skillQ : skillW;
}
