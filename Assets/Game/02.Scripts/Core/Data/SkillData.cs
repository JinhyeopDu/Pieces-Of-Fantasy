using UnityEngine;

public enum SkillActionType
{
    BasicAttack = 0,

    HealParty = 10,          // Tribi: 파티 전체 회복(컷 + VFX)
    SingleStrongHit = 20,    // Kisora: 제자리 단일 강공(VFX)
    AoEHitAllEnemies = 30,   // StellarWitch: 제자리 광역(VFX)
}

[CreateAssetMenu(menuName = "PoF/Skill")]
public class SkillData : ScriptableObject
{
    [Header("Identity")]
    public string id;
    public string displayName;

    [Header("UI")]
    public Sprite icon;
    [Tooltip("예: [서포트], [공격], [광역]")]
    public string tagText;
    [TextArea(3, 8)]
    public string description;

    [Header("Cost")]
    [Tooltip("스킬 포인트 비용. (스킬=1 권장, 기본공격=0, 궁극=0/별도게이지)")]
    public int spCost;

    [Header("Action Type")]
    public SkillActionType actionType = SkillActionType.BasicAttack;

    [Header("Numbers")]
    [Tooltip("공격 스킬 배율(%). 100=기본, 200=2배")]

    public int power = 100;

    [Tooltip("힐 스킬: 대상 maxHP의 퍼센트(0~1). 예: 0.30 = 30%")]
    [Range(0f, 1f)] public float healPercent = 0.30f;

    [Header("VFX")]
    [Tooltip("스킬 전용 VFX 프리팹(힐/단일강공/광역용)")]
    public GameObject vfxPrefab;

    [Tooltip("VFX 유지 시간(초). 0이면 자동 파괴 안 함(권장X)")]
    public float vfxLifeTime = 1.2f;

    [Header("Animation")]
    [Tooltip("캐스터 애니메이터에서 스킬을 실행할 Trigger 이름")]
    public string animTrigger = "Skill";
}
