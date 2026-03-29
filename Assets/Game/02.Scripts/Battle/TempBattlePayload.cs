public static class TempBattlePayload
{
    public static EncounterData encounter;
    public static EnemyData[] enemySet;

    // 어떤 필드 몬스터에서 시작된 전투인지
    public static string spawnId;
    public static float respawnDelay; // 추가
}