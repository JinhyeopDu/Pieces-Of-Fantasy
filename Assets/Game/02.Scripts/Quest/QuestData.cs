using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "PoF/Quest/QuestData", fileName = "QST_NewQuest")]
public class QuestData : ScriptableObject
{
    [Header("Identity")]
    public string questId;
    public string title;
    [TextArea(2, 5)] public string description;

    [Header("Objective")]
    public QuestObjectiveType objectiveType = QuestObjectiveType.None;

    [Tooltip("KillEnemyById / KillBossById 에서 사용할 적 ID")]
    public string targetEnemyId;

    [Tooltip("카운트형 또는 레벨 목표값")]
    public int targetValue = 1;

    [Tooltip("승급 목표값(1,2,3...)")]
    public int targetPromotion = 0;

    [Header("Reward")]
    public List<QuestRewardEntry> rewards = new();

    [Header("Flow")]
    public QuestData nextQuest;

    private void OnValidate()
    {
        if (questId != null)
            questId = questId.Trim();
    }
}