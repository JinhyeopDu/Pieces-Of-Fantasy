using UnityEngine;

/// <summary>
/// 레벨업/승급(Ascension) 정책 단일 소스.
/// - 레벨 캡: 10/20/30/40/50
/// - 승급 단계: 0~4 (0=초기, 4=최종)
/// - exp는 "현재 레벨 구간 내 exp" (레벨업하면 exp 차감 후 다음 레벨로)
/// </summary>
public static class LevelingPolicy
{
    public const int MaxPromotionStage = 4;
    public const int MaxLevel = 50;

    // stage 0..4 => cap 10,20,30,40,50
    public static int GetLevelCap(int promotionStage)
    {
        promotionStage = Mathf.Clamp(promotionStage, 0, MaxPromotionStage);
        return (promotionStage + 1) * 10;
    }

    public static bool IsAtLevelCap(int level, int promotionStage)
    {
        return level >= GetLevelCap(promotionStage);
    }

    public static bool CanGainExp(int level, int promotionStage)
    {
        return !IsAtLevelCap(level, promotionStage);
    }

    /// <summary>
    /// 레벨업에 필요한 경험치(현재 레벨 -> 다음 레벨).
    /// MVP 정책: stage에 따라 커브/스케일만 다르게.
    /// </summary>
    public static int GetNeedExpForNextLevel(int level, int promotionStage)
    {
        level = Mathf.Clamp(level, 1, MaxLevel);
        promotionStage = Mathf.Clamp(promotionStage, 0, MaxPromotionStage);

        // 캡 도달이면 "다음 레벨 필요 exp"는 의미가 없으므로 0으로 반환
        // -> UI가 need==0을 보고 "MAX/승급 필요"로 자연스럽게 분기 가능
        if (IsAtLevelCap(level, promotionStage))
            return 0;

        // ---- 간단 커브 (나중에 테이블로 대체 가능) ----
        int baseNeed = 1200 + (level - 1) * 220;         // 선형 증가
        float stageBonus = 1f + promotionStage * 0.15f;
        int need = Mathf.RoundToInt(baseNeed * stageBonus);

        return Mathf.Max(300, need);
    }

    /// <summary>
    /// 레벨업 적용: exp를 더하고 가능한 만큼 레벨업.
    /// - 남는 exp는 다음 레벨 exp로 carry
    /// - 레벨캡 도달 시 즉시 stop, exp는 cap 내에서 clamp
    /// </summary>
    public static void ApplyExpAndLevelUp(ref int level, ref int exp, int addExp, int promotionStage)
    {
        if (addExp <= 0) return;

        level = Mathf.Clamp(level, 1, MaxLevel);
        exp = Mathf.Max(0, exp);

        if (IsAtLevelCap(level, promotionStage))
        {
            // 캡이면 exp를 0으로 고정하거나 유지할지 정책 선택 가능.
            // HSR 느낌이면 캡 도달 시 exp는 "가득 찬 상태"로 보여줄 수도 있지만
            // 현재 UI 로직은 "needExp 분모 기준"이라 0 유지가 안전.
            exp = 0;
            return;
        }

        exp += addExp;

        // 여러 레벨 오를 수 있게 while
        while (true)
        { 
            if (IsAtLevelCap(level, promotionStage))
            {
                exp = 0;
                break;
            }

            int need = GetNeedExpForNextLevel(level, promotionStage);
            if (need <= 0) need = 1;

            if (exp < need) break;

            exp -= need;
            level += 1;

            if (level >= MaxLevel)
            {
                level = MaxLevel;
                exp = 0;
                break;
            }
        }
    }

    /// <summary>
    /// 승급 가능 여부: 레벨캡에 도달했고, stage가 최대가 아니며, 승급 재료를 충족하면 가능.
    /// "재료" 체크는 컨트롤러에서 하므로 여기선 논리만 제공.
    /// </summary>
    public static bool IsPromotionRequired(int level, int promotionStage)
    {
        if (promotionStage >= MaxPromotionStage) return false;
        return IsAtLevelCap(level, promotionStage);
    }

    public static float GetExpProgress01(int level, int exp, int promotionStage)
    {
        level = Mathf.Clamp(level, 1, MaxLevel);
        promotionStage = Mathf.Clamp(promotionStage, 0, MaxPromotionStage);

        int need = GetNeedExpForNextLevel(level, promotionStage);

        // 캡(need==0)인 경우: 바는 항상 꽉 찬 상태로 표현
        if (need == 0)
            return 1f;

        // 정상 구간: exp/need
        exp = Mathf.Max(0, exp);
        return Mathf.Clamp01(exp / (float)need);
    }
}