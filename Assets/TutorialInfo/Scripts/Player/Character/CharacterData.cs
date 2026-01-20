using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacterData", menuName = "Data/CharacterData")]
public class CharacterData : ScriptableObject
{
    [Header("전투 능력치")]
    public float attackRange = 7.0f;      // 평타 사거리
    public float attackDamage = 40f;     // 평타 데미지
    public float attackSpeed = 1.0f;      // 공격 속도 (초당 횟수)

    [Header("투사체 설정")]
    public float projectileSpeed = 15f;  // 투사체가 날아가는 속도
    public string bulletPoolName = "BasicBullet"; // 사용할 투사체 프리팹 이름
}
