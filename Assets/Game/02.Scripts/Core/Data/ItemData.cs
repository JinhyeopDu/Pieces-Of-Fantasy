using UnityEngine;

public enum ItemCategory
{
    Consumable = 10,
    Material = 20,
    Equipment = 30,
    Quest = 40,
    KeyItem = 50,
    Etc = 90,
}

public enum ItemType
{
    Consumable = 10,
    Material = 20,
    Equipment = 30,
    Quest = 40,
    KeyItem = 50,
}

public enum GatherMaterialType
{
    None = 0,
    Ore = 1,
    Wood = 2,
    Herb = 3,
    Bone = 4
}

/// <summary>
/// 아이템을 어디서 사용할 수 있는지(사용 시나리오)
/// </summary>
public enum ItemUseScope
{
    Any = 0,
    BattleOnly = 10,
    ExplorationOnly = 20,
}

/// <summary>
/// 효과가 언제까지 유지되는지(효과 단위)
/// </summary>
public enum EffectDuration
{
    Instant = 0,          // 즉시 적용(회복/부활 등)
    BattleUntilEnd = 10,  // 전투 종료 시 해제(버프류)
}

public enum ConsumableEffectType
{
    None = 0,

    // Instant
    HealHP = 10,
    RestoreSecretArt = 20,
    Revive = 30,
    BuffAttack = 35,

    // Buff
    BuffSpeed = 40,
    BuffDefense = 50,
    BuffMaxHP = 60,
}

public enum ItemTargetPolicy
{
    SingleAlly = 0,
    AllParty = 10,
    None = 20,
}

[System.Serializable]
public class ItemEffectSlot
{
    public ConsumableEffectType type = ConsumableEffectType.None;

    [Tooltip("효과 값. HealHP면 회복량, BuffSpeed면 증가량 등")]
    public int value = 0;

    [Tooltip("value가 %인지 여부(예: 최대 HP의 20%)")]
    public bool isPercent = false;

    public EffectDuration duration = EffectDuration.Instant;
}

[CreateAssetMenu(fileName = "IT_NewItem", menuName = "PoF/Item")]
public class ItemData : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("세이브/드랍테이블 안정성을 위한 고유 ID")]
    public string id = "it_";

    public string displayName = "New Item";

    [TextArea(2, 4)]
    public string description;

    [Header("Presentation")]
    public Sprite icon;

    [Header("Classification")]
    public ItemCategory category = ItemCategory.Etc;
    public ItemType itemType = ItemType.Material;

    [Header("Gather Classification")]
    public GatherMaterialType gatherMaterialType = GatherMaterialType.None;

    [Header("Stack Policy")]
    [Min(1)]
    public int maxStack = 99;

    [Header("Economy (Optional)")]
    [Min(0)]
    public int sellPrice = 0;

    [Header("Use Scope (Consumable)")]
    public ItemUseScope useScope = ItemUseScope.Any;

    [Header("Effects (A/B)")]
    public ItemEffectSlot effectA = new ItemEffectSlot();
    public ItemEffectSlot effectB = new ItemEffectSlot();

    public bool IsConsumable => itemType == ItemType.Consumable;

    public ItemTargetPolicy targetPolicy = ItemTargetPolicy.SingleAlly;
}