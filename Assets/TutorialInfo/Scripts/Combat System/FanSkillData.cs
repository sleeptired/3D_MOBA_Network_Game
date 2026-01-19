using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Skills/Fan Skill")]
public class FanSkillData : SkillData
{
    [Header("범위 설정")]
    public float angle = 90f;       // 부채꼴 각도
    public float slowDuration = 3f; // 둔화 시간
    public LayerMask targetLayer;

    public Color skillColor = new Color(0, 0, 1, 0.3f);//기본색

    public override void OnUse(GameObject caster, Vector3 point)
    {
        var controller = caster.GetComponent<PlayerController>();
        if (controller != null)
        {
            controller.FireFanSkillServerRpc(angle, range, slowDuration, skillColor);
        }
    }
}
