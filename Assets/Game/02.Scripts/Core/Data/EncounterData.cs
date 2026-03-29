using UnityEngine;

[CreateAssetMenu(menuName = "PoF/Encounter")]
public class EncounterData : ScriptableObject
{
    [Header("Slot 0 (Always Spawn)")]
    public EnemyData guaranteedEnemy;
    public int guaranteedCount = 1;

    [System.Serializable]
    public class Candidate
    {
        public EnemyData enemy;
        [Range(0f, 1f)] public float weight = 1f; // 선택 가중치(1이면 균등)
        public int count = 1;
    }

    [System.Serializable]
    public class OptionalSlot
    {
        [Range(0f, 1f)] public float spawnChance = 0.5f; // 이 슬롯이 생성될 확률
        public Candidate[] candidates;                   // 생성된다면 여기서 랜덤 선택
    }

    [Header("Optional Slots (Slot 1, Slot 2)")]
    public OptionalSlot[] optionalSlots = new OptionalSlot[2]; // 기본 2개(1번,2번)
}
