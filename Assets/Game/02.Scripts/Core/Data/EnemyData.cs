using UnityEngine;

public enum ElementType { None, Physical, Fire, Ice, Lightning, Wind, Quantum, Imaginary }

public enum EnemyRank
{
    Normal = 0,
    Elite = 10,   // 준보스
    Boss = 20     // 보스
}

[CreateAssetMenu(menuName = "PoF/Enemy")]
public class EnemyData : ScriptableObject
{
    [Header("Identity")]
    public string id;
    public string displayName;
    public Sprite portrait;

    [Header("Base (stats/skills via CharacterData)")]
    public CharacterData baseStats;

    [Header("Leveling")]
    public int defaultLevel = 1;
    public int minLevel = 1;
    public int maxLevel = 1;

    [Header("Weakness/Toughness")]
    public int toughness = 100;
    public ElementType[] weaknesses;

    [Header("Prefabs (optional visuals)")]
    public GameObject battlePrefab;
    public GameObject explorationPrefab;

    [Header("Rewards")]
    public int expReward = 10;
    public int creditReward = 20;

    [Header("Rank / Respawn Policy")]
    public EnemyRank rank = EnemyRank.Normal;
    
    // 드롭율 관련
    public DropTable dropTable;

    // 보스라면 보통 true
    public bool uniqueDefeat = false;

    // -1이면 BattleStarter의 respawnDelay 사용
    // 0 이상이면 이 값으로 강제
    public float respawnDelayOverride = -1f;

    [System.Serializable]
    public class LootEntry { public ItemData item;[Range(0, 1)] public float dropRate = 0.2f; public int min = 1; public int max = 1; }
    public LootEntry[] lootTable;

    [Header("AI")]
    public EnemyAIProfile aiProfile = EnemyAIProfile.BasicFocusWeak;

    public int ResolveSpawnLevel()
    {
        if (minLevel > maxLevel) return defaultLevel;
        if (minLevel == maxLevel) return minLevel;
        return Random.Range(minLevel, maxLevel + 1);
    }

    public void ComputeFinalStats(int level, out int maxHp, out int atk, out int def, out int spd)
    {
        level = Mathf.Max(1, level);

        // baseStats가 없으면 안전 기본값
        if (baseStats == null)
        {
            maxHp = 1;
            atk = 1;
            def = 0;
            spd = 100;
            return;
        }

        // 1) CharacterData 기반 "레벨 성장"
        int hpBase = baseStats.baseHP + baseStats.hpPerLevel * (level - 1);
        int atkBase = baseStats.baseATK + baseStats.atkPerLevel * (level - 1);
        int defBase = baseStats.baseDEF + baseStats.defPerLevel * (level - 1);
        int spdBase = baseStats.baseSPD + baseStats.spdPerLevel * (level - 1);

        // 2) EnemyRank 보정(정책값: 여기서 너가 밸런스 조절)
        //    - 일단 "곱연산"으로 정리: 엘리트/보스가 더 탱키하고 세게
        float hpMul = 1f, atkMul = 1f, defMul = 1f, spdMul = 1f;

        switch (rank)
        {
            case EnemyRank.Elite:
                hpMul = 1.35f;
                atkMul = 1.20f;
                defMul = 1.15f;
                spdMul = 1.00f;
                break;

            case EnemyRank.Boss:
                hpMul = 1.80f;
                atkMul = 1.35f;
                defMul = 1.25f;
                spdMul = 1.00f;
                break;

            default: // Normal
                break;
        }

        maxHp = Mathf.Max(1, Mathf.RoundToInt(hpBase * hpMul));
        atk = Mathf.Max(0, Mathf.RoundToInt(atkBase * atkMul));
        def = Mathf.Max(0, Mathf.RoundToInt(defBase * defMul));
        spd = Mathf.Max(1, Mathf.RoundToInt(spdBase * spdMul));
    }
}

public enum EnemyAIProfile { BasicRandom, BasicFocusWeak }
