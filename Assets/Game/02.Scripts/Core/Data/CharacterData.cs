using UnityEngine;

public enum SecretArtType
{
    None = 0,
    HealParty = 10,        // Tribi: 전투 시작 시 파티 회복
    DefBuffParty = 20,     // Kisora: 전투 시작 시 파티 방어 버프
    GainBattleSP = 30,     // StellarWitch: 전투 시작 시 Battle SP +N
}

/// <summary>
/// 캐릭터의 정적 데이터(ScriptableObject).
/// 실제 플레이 중 변경되는 값(레벨, HP, 버프, 상태)은 CharacterRuntime이 보관하고,
/// CharacterData는 기본 스탯/성장/스킬/비술 같은 설계 데이터를 가진다.
/// </summary>
[CreateAssetMenu(fileName = "CH_NewCharacter", menuName = "PoF/Character")]
public class CharacterData : ScriptableObject
{
    // 정책 기본값(원하는 스타레일식)
    public const float DEFAULT_HEAL_PERCENT = 0.30f; // 대상 maxHP의 30%
    public const float DEFAULT_DEF_PERCENT = 0.50f;  // 대상 baseDEF의 50%
    public const int DEFAULT_DEF_TURNS = 3;

    [Header("Identity")]
    [Tooltip("세이브/로드 및 런타임 매칭에 사용하는 캐릭터 고유 ID")]
    public string id;

    [Tooltip("게임 UI에 표시할 캐릭터 이름")]
    public string displayName;

    [Header("Visual")]
    [Tooltip("캐릭터 초상화")]
    public Sprite portrait;

    [Tooltip("탐험 씬에서 사용하는 캐릭터 프리팹")]
    public GameObject explorationPrefab;

    [Header("Secret Art (UI)")]
    [Tooltip("탐험 씬 비술 UI에 표시할 아이콘")]
    public Sprite secretArtIcon;

    [Header("Base Stats")]
    [Tooltip("레벨 1 기준 기본 HP")]
    public int baseHP = 300;

    [Tooltip("레벨 1 기준 기본 공격력")]
    public int baseATK = 20;

    [Tooltip("레벨 1 기준 기본 방어력")]
    public int baseDEF = 10;

    [Tooltip("레벨 1 기준 기본 속도")]
    public int baseSPD = 100;

    [Header("Growth Per Level (V0)")]
    [Tooltip("레벨당 증가하는 HP")]
    public int hpPerLevel = 0;

    [Tooltip("레벨당 증가하는 공격력")]
    public int atkPerLevel = 0;

    [Tooltip("레벨당 증가하는 방어력")]
    public int defPerLevel = 0;

    [Tooltip("레벨당 증가하는 속도")]
    public int spdPerLevel = 0;

    [Header("Promotion Bonus (Per Promotion)")]
    [Tooltip("승급 1회당 적용되는 HP 비율 보너스")]
    [Range(0f, 1f)] public float promoHpPercent = 0f;

    [Tooltip("승급 1회당 적용되는 공격력 비율 보너스")]
    [Range(0f, 1f)] public float promoAtkPercent = 0f;

    [Tooltip("승급 1회당 적용되는 방어력 비율 보너스")]
    [Range(0f, 1f)] public float promoDefPercent = 0f;

    [Tooltip("승급 1회당 적용되는 속도 비율 보너스")]
    [Range(0f, 1f)] public float promoSpdPercent = 0f;

    [Header("Battle Skills")]
    [Tooltip("기본 공격에 사용하는 SkillData")]
    public SkillData basicAtk;

    [Tooltip("일반 스킬에 사용하는 SkillData")]
    public SkillData skill;

    [Header("Secret Art (Pre-Battle Effect)")]
    [Tooltip("탐험에서 준비 후 전투 시작 시 발동되는 비술 타입")]
    public SecretArtType secretArtType = SecretArtType.None;

    [Header("GainBattleSP (StellarWitch)")]
    [Tooltip("비술 효과가 GainBattleSP일 때 전투 시작 시 획득할 SP")]
    public int secretArtGainBattleSP = 2;

    [Header("HealParty (Tribi)")]
    [Tooltip("비술 효과가 HealParty일 때 파티 최대 HP 기준 회복 비율")]
    [Range(0f, 1f)]
    public float secretArtHealPercent = DEFAULT_HEAL_PERCENT;

    [Header("DefBuffParty (Kisora)")]
    [Tooltip("비술 효과가 DefBuffParty일 때 baseDEF 기준 증가 비율")]
    [Range(0f, 2f)]
    public float secretArtDefPercent = DEFAULT_DEF_PERCENT;

    [Tooltip("비술 방어 버프의 지속 턴 수")]
    public int secretArtDefTurns = DEFAULT_DEF_TURNS;

#if UNITY_EDITOR
    /// <summary>
    /// 에셋 인스펙터에서 비술 타입에 맞는 기본값을 자동 보정한다.
    /// 잘못 비워진 값을 정책 기본값으로 되돌리는 용도다.
    /// </summary>
    private void OnValidate()
    {
        if (secretArtType == SecretArtType.HealParty)
        {
            if (secretArtHealPercent <= 0f) secretArtHealPercent = DEFAULT_HEAL_PERCENT;
            secretArtHealPercent = Mathf.Clamp01(secretArtHealPercent);
        }
        else if (secretArtType == SecretArtType.DefBuffParty)
        {
            if (secretArtDefPercent <= 0f) secretArtDefPercent = DEFAULT_DEF_PERCENT;
            if (secretArtDefTurns <= 0) secretArtDefTurns = DEFAULT_DEF_TURNS;
            secretArtDefPercent = Mathf.Max(0f, secretArtDefPercent);
        }
        else if (secretArtType == SecretArtType.GainBattleSP)
        {
            if (secretArtGainBattleSP <= 0) secretArtGainBattleSP = 2;
        }
    }
#endif
}