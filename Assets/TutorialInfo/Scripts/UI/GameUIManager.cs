using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance; // 싱글톤

    [Header("패널 (통째로 껐다 킬 것)")]
    public GameObject skillPanelRoot;

    [Header("스킬 슬롯")]
    public SkillSlotUI slotQ;
    public SkillSlotUI slotW;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // 게임 시작 전엔 숨겨두기
        if (skillPanelRoot != null) skillPanelRoot.SetActive(false);
    }

    public void BindPlayerSkills(SkillManager skillManager)
    {
        // 플레이어 접속하면 보여주기
        if (skillPanelRoot != null) skillPanelRoot.SetActive(true);

        slotQ.SetSkill(skillManager.skillQ);
        slotW.SetSkill(skillManager.skillW);

        skillManager.OnSkillUsed -= HandleSkillUsed;
        skillManager.OnSkillUsed += HandleSkillUsed;
    }

    private void HandleSkillUsed(string key, float time)
    {
        if (key == "Q") slotQ.UseSkill(time);
        else if (key == "W") slotW.UseSkill(time);
    }
}
