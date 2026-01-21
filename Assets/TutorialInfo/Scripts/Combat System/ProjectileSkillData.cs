using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

[CreateAssetMenu(menuName = "Skills/Projectile Skill")]
public class ProjectileSkillData : SkillData
{
    //투사체 설정
    public GameObject projectilePrefab; // 총알 프리팹
    public string poolName = "Bullet";
    public float speed = 20f;
    public float stunDuration = 2f;     // 기절 시간

    public override void OnUse(GameObject caster, Vector3 direction)
    {
        // 총알 생성 및 발사 로직
        var controller = caster.GetComponent<PlayerController>();
        if (controller != null)
        {
            controller.FireProjectileServerRpc(poolName, direction, speed, stunDuration);
        }
    }
}