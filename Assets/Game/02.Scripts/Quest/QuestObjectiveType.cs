public enum QuestObjectiveType
{
    None = 0,

    GatherAnyOre,               // 어떤 광석이든 카운트
    KillEnemyById,              // 특정 적 누적 처치
    ReachAnyCharacterLevel,     // 파티 중 아무나 특정 레벨 달성
    ReachAnyCharacterPromotion, // 파티 중 아무나 특정 승급 달성
    KillBossById                // 특정 보스 처치
}