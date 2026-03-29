using UnityEngine;

/// <summary>
/// Battle 씬에서만 사용하는 "전투 런타임 스탯".
/// - GameContext(CharacterRuntime) / EnemyData(ComputeFinalStats)에서 값을 주입받아 사용
/// - 전투 중 버프/디버프는 여기서만 누적(전투 종료 시 GameContext에 반영/해제 정책에 따름)
/// </summary>
public class BattleActorRuntime
{
    // ──────────────────────────────
    // Identity / Source
    // ──────────────────────────────
    public CharacterData data;
    public bool isEnemy;

    // ──────────────────────────────
    // Core progression snapshot
    // ──────────────────────────────
    public int level;

    // ──────────────────────────────
    // Resources
    // ──────────────────────────────
    public int hp;
    public int maxHp;
    public int sp;

    // ──────────────────────────────
    // Combat stats (final values used by battle logic)
    // ──────────────────────────────
    public int atk;
    public int def;
    public int spd;

    // ──────────────────────────────
    // Battle-only Buff Runtime (minimal)
    // ──────────────────────────────
    public int defBonus;
    public int defBonusTurns;

    public BattleActorRuntime(CharacterData d, bool enemy, int level = 1, int initialHp = -1, int initialSp = 0)
    {
        data = d;
        isEnemy = enemy;

        this.level = Mathf.Max(1, level);

        // 주의:
        // 이 클래스는 "최종값을 외부에서 주입"받는 구조가 목표.
        // 그래서 생성자에서는 최소한의 안전 기본값만 만든다.
        // (BattleController Allies build / EnemyActorFactory에서 반드시 덮어쓰기)

        // 기본 maxHp/atk/def/spd (fallback)
        if (data != null)
        {
            // 과거 너 코드의 임시 공식(maxHp = baseHP + level*10)을 유지하되,
            // 지금은 EnemyData.ComputeFinalStats / CharacterRuntime.RecalculateStats 결과로 덮어쓰는 것이 정석.
            maxHp = Mathf.Max(1, data.baseHP + this.level * 10);
            atk = data.baseATK;
            def = data.baseDEF;
            spd = data.baseSPD;
        }
        else
        {
            maxHp = 1;
            atk = 1;
            def = 0;
            spd = 100;
        }

        if (initialHp < 0) hp = maxHp;
        else hp = Mathf.Clamp(initialHp, 0, maxHp);

        sp = Mathf.Max(0, initialSp);

        defBonus = 0;
        defBonusTurns = 0;
    }

    public bool IsDead => hp <= 0;

    // ──────────────────────────────
    // Effective Stats
    // ──────────────────────────────
    public int GetEffectiveATK()
    {
        // 공격 버프 시스템이 생기면 여기에 누적해도 됨
        return Mathf.Max(0, atk);
    }

    public int GetEffectiveDEF()
    {
        return Mathf.Max(0, def + Mathf.Max(0, defBonus));
    }

    public int GetEffectiveSPD()
    {
        // 속도 버프 시스템이 생기면 여기에 누적
        return Mathf.Max(1, spd);
    }

    // ──────────────────────────────
    // Buff API (minimal: DEF only)
    // ──────────────────────────────
    public void AddDefBonus(int amount, int turns)
    {
        if (amount <= 0 || turns <= 0) return;
        defBonus += amount;
        defBonusTurns = Mathf.Max(defBonusTurns, turns);
    }

    // 행동자 턴 종료 기준 감소
    public void TickTurnEnd()
    {
        if (defBonusTurns <= 0) return;

        defBonusTurns--;
        if (defBonusTurns <= 0)
        {
            defBonusTurns = 0;
            defBonus = 0;
        }
    }

    // ──────────────────────────────
    // Utilities
    // ──────────────────────────────
    public override string ToString()
    {
        string n = data != null ? data.name : "NULL";
        return $"{n} (lv={level}) HP={hp}/{maxHp} SP={sp} ATK={atk} DEF={def} SPD={spd} enemy={isEnemy}";
    }
}