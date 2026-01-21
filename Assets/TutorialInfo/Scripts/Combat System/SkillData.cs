using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SkillData : ScriptableObject
{
    //기본정보
    public string skillName;
    public Sprite icon;       // UI 표시용 아이콘
    public float cooldown;    // 쿨타임
    public float range;       // 사거리

    //설정
    public bool isAimingSkill; // 조준이 필요한가

    // caster: 스킬 쓴 사람, point: 마우스 위치/방향
    public abstract void OnUse(GameObject caster, Vector3 point);
}
