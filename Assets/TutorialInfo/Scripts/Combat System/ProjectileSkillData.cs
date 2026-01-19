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
        // (주의: SO는 데이터 덩어리이므로, 실제 생성은 매니저나 RPC를 통해야 함.
        // 여기서는 로직 분리를 위해 '데이터 전달'에 집중하거나,
        // 구조상 Caster의 컴포넌트를 가져와서 실행합니다.)

        var controller = caster.GetComponent<PlayerController>();
        if (controller != null)
        {
            controller.FireProjectileServerRpc(poolName, direction, speed, stunDuration);
        }
    }
}