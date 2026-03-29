using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager I { get; private set; }

    [Header("Quest Flow")]
    [SerializeField] private QuestData firstQuest;

    public System.Action OnQuestUpdated;

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        Debug.Log("[QuestManager] Start called");

        var g = GameContext.I;
        if (g == null)
        {
            Debug.LogWarning("[QuestManager] Start skipped: GameContext is null.");
            return;
        }

        Debug.Log(
            $"[QuestManager] Start state | currentQuest={(g.currentQuest != null ? g.currentQuest.questId : "NULL")} | " +
            $"hasProgress={(g.currentQuestProgress != null)} | " +
            $"allDone={g.allQuestsCompleted}"
        );

        EnsureQuestInitialized();
    }

    public void EnsureQuestInitialized()
    {
        var g = GameContext.I;

        Debug.Log(
            $"[QuestManager] EnsureQuestInitialized called | " +
            $"GC={(g != null)} | " +
            $"currentQuest={(g != null && g.currentQuest != null ? g.currentQuest.questId : "NULL")} | " +
            $"hasProgress={(g != null && g.currentQuestProgress != null)} | " +
            $"allQuestsCompleted={(g != null ? g.allQuestsCompleted : false)} | " +
            $"firstQuest={(firstQuest != null ? firstQuest.questId : "NULL")}"
        );

        if (g == null) return;

        if (g.allQuestsCompleted)
        {
            Debug.Log("[QuestManager] Skip init: all quests already completed.");
            NotifyUpdated();
            return;
        }

        // 이미 둘 다 살아있으면 절대 덮어쓰지 않음
        if (g.currentQuest != null && g.currentQuestProgress != null)
        {
            Debug.Log("[QuestManager] Skip init: restored/current quest already exists.");
            RefreshAutoCompleteConditions();
            return;
        }

        // 한쪽만 있는 경우: 일단 덮어쓰지 말고 로그만 찍고 종료
        if (g.currentQuest != null || g.currentQuestProgress != null)
        {
            Debug.LogWarning(
                $"[QuestManager] Partial quest state detected. Skip auto-init to avoid overwriting save. " +
                $"currentQuest={(g.currentQuest != null ? g.currentQuest.questId : "NULL")} | " +
                $"hasProgress={(g.currentQuestProgress != null)}"
            );
            NotifyUpdated();
            return;
        }

        if (firstQuest == null)
        {
            Debug.LogWarning("[QuestManager] firstQuest is null.");
            return;
        }

        Debug.Log($"[QuestManager] No active quest found. Start firstQuest => {firstQuest.questId}");
        StartQuest(firstQuest);
    }

    public void StartQuest(QuestData quest)
    {
        if (quest == null)
        {
            Debug.LogWarning("[QuestManager] StartQuest failed: quest is null.");
            return;
        }

        var g = GameContext.I;
        if (g == null)
        {
            Debug.LogWarning("[QuestManager] StartQuest failed: GameContext is null.");
            return;
        }

        if (g.currentQuest != null && g.currentQuestProgress != null && g.currentQuest.questId == quest.questId)
        {
            Debug.Log($"[QuestManager] StartQuest skipped: already active quest => {quest.questId}");
            return;
        }

        g.currentQuest = quest;
        g.currentQuestProgress = new QuestRuntimeProgress(quest.questId);

        Debug.Log($"[QuestManager] StartQuest => {quest.questId} / {quest.title}");
        Debug.Log(
            $"[QuestManager] After StartQuest | " +
            $"currentQuest={(g.currentQuest != null ? g.currentQuest.questId : "NULL")} | " +
            $"hasProgress={(g.currentQuestProgress != null)}"
        );

        RefreshAutoCompleteConditions();
        NotifyUpdated();
    }

    public QuestData GetCurrentQuest()
    {
        return GameContext.I != null ? GameContext.I.currentQuest : null;
    }

    public QuestRuntimeProgress GetCurrentProgress()
    {
        return GameContext.I != null ? GameContext.I.currentQuestProgress : null;
    }

    public bool HasActiveQuest()
    {
        var g = GameContext.I;
        return g != null && g.currentQuest != null && g.currentQuestProgress != null;
    }

    public void NotifyGatherOre(int amount)
    {
        if (amount <= 0) return;

        var quest = GetCurrentQuest();
        var prog = GetCurrentProgress();

        if (quest == null || prog == null) return;
        if (prog.isCompleted) return;

        if (quest.objectiveType != QuestObjectiveType.GatherAnyOre)
            return;

        prog.currentValue += amount;
        CheckCompletion();

        Debug.Log($"[QuestManager] GatherAnyOre +{amount} => {prog.currentValue}/{quest.targetValue}");
        NotifyUpdated();
    }

    public void NotifyEnemyKilled(string enemyId, int count = 1, bool isBoss = false)
    {
        if (string.IsNullOrEmpty(enemyId) || count <= 0) return;

        var quest = GetCurrentQuest();
        var prog = GetCurrentProgress();

        Debug.Log(
            $"[QuestKill] incoming enemyId={enemyId}, count={count}, isBoss={isBoss} | " +
            $"currentQuest={(quest != null ? quest.questId : "NULL")} | " +
            $"objectiveType={(quest != null ? quest.objectiveType.ToString() : "NULL")} | " +
            $"targetEnemyId={(quest != null ? quest.targetEnemyId : "NULL")}"
        );

        if (quest == null || prog == null) return;
        if (prog.isCompleted) return;

        switch (quest.objectiveType)
        {
            case QuestObjectiveType.KillEnemyById:
                if (isBoss)
                {
                    Debug.Log("[QuestKill] rejected: quest wants normal enemy, but incoming kill is boss.");
                    return;
                }
                if (quest.targetEnemyId != enemyId)
                {
                    Debug.Log($"[QuestKill] rejected: target mismatch. target={quest.targetEnemyId}, incoming={enemyId}");
                    return;
                }
                prog.currentValue += count;
                break;

            case QuestObjectiveType.KillBossById:
                if (!isBoss)
                {
                    Debug.Log("[QuestKill] rejected: quest wants boss, but isBoss=false.");
                    return;
                }
                if (quest.targetEnemyId != enemyId)
                {
                    Debug.Log($"[QuestKill] rejected: boss target mismatch. target={quest.targetEnemyId}, incoming={enemyId}");
                    return;
                }
                prog.currentValue += count;
                break;

            default:
                Debug.Log("[QuestKill] rejected: current quest is not a kill quest.");
                return;
        }

        CheckCompletion();

        Debug.Log($"[QuestManager] Kill {enemyId} +{count} => {prog.currentValue}/{quest.targetValue}");
        NotifyUpdated();
    }

    public void RefreshAutoCompleteConditions()
    {
        var quest = GetCurrentQuest();
        var prog = GetCurrentProgress();
        var g = GameContext.I;

        if (quest == null || prog == null || g == null || g.party == null)
            return;

        if (prog.isCompleted)
            return;

        switch (quest.objectiveType)
        {
            case QuestObjectiveType.ReachAnyCharacterLevel:
                {
                    int highest = GetHighestLevel(g.party);
                    prog.currentValue = highest;
                    CheckCompletion();
                    break;
                }

            case QuestObjectiveType.ReachAnyCharacterPromotion:
                {
                    int highest = GetHighestPromotion(g.party);
                    prog.currentValue = highest;
                    CheckCompletion();
                    break;
                }
        }

        NotifyUpdated();
    }

    public bool CanClaimReward()
    {
        var prog = GetCurrentProgress();
        return prog != null && prog.isCompleted && !prog.rewardClaimed;
    }

    public void ClaimCurrentQuestReward()
    {
        var g = GameContext.I;
        var quest = GetCurrentQuest();
        var prog = GetCurrentProgress();

        if (g == null || quest == null || prog == null) return;
        if (!prog.isCompleted || prog.rewardClaimed) return;

        if (quest.rewards != null)
        {
            for (int i = 0; i < quest.rewards.Count; i++)
            {
                var r = quest.rewards[i];
                if (r == null || r.item == null || r.amount <= 0) continue;

                g.AddItem(r.item, r.amount);
                g.QueueReward(r.item, r.amount);
            }
        }

        prog.rewardClaimed = true;

        if (!string.IsNullOrEmpty(quest.questId) && !g.completedQuestIds.Contains(quest.questId))
            g.completedQuestIds.Add(quest.questId);

        Debug.Log($"[QuestManager] Reward claimed => {quest.title} | questId={quest.questId} | nextQuest={(quest.nextQuest != null ? quest.nextQuest.questId : "NULL")}");

        QuestData next = quest.nextQuest;

        if (next != null)
        {
            StartQuest(next);
        }
        else
        {
            // 마지막 퀘스트 보상 수령 완료
            g.currentQuest = null;
            g.currentQuestProgress = null;
            g.allQuestsCompleted = true;

            // 엔딩 패널은 "최종 퀘스트 보상 수령 시점"에 띄우기 위한 플래그
            g.endingShown = true;

            Debug.Log("[QuestManager] Final quest reward claimed. endingShown = true");
            NotifyUpdated();
        }
    }

    public string GetProgressText()
    {
        var quest = GetCurrentQuest();
        var prog = GetCurrentProgress();

        if (quest == null || prog == null)
            return string.Empty;

        switch (quest.objectiveType)
        {
            case QuestObjectiveType.GatherAnyOre:
            case QuestObjectiveType.KillEnemyById:
            case QuestObjectiveType.KillBossById:
                return $"({Mathf.Clamp(prog.currentValue, 0, quest.targetValue)}/{quest.targetValue})";

            case QuestObjectiveType.ReachAnyCharacterLevel:
                return $"\n현재 최고 레벨 {prog.currentValue}/{quest.targetValue}";

            case QuestObjectiveType.ReachAnyCharacterPromotion:
                return prog.currentValue >= quest.targetPromotion
                    ? "\n(달성)"
                    : "\n(미달성)";

            default:
                return string.Empty;
        }
    }

    private void CheckCompletion()
    {
        var quest = GetCurrentQuest();
        var prog = GetCurrentProgress();
        if (quest == null || prog == null) return;

        bool completed = false;

        switch (quest.objectiveType)
        {
            case QuestObjectiveType.GatherAnyOre:
            case QuestObjectiveType.KillEnemyById:
            case QuestObjectiveType.KillBossById:
            case QuestObjectiveType.ReachAnyCharacterLevel:
                completed = prog.currentValue >= quest.targetValue;
                break;

            case QuestObjectiveType.ReachAnyCharacterPromotion:
                completed = prog.currentValue >= quest.targetPromotion;
                break;
        }

        if (completed && !prog.isCompleted)
        {
            prog.isCompleted = true;
            Debug.Log($"[QuestManager] Quest completed => {quest.title}");
        }
    }

    private int GetHighestLevel(List<CharacterRuntime> party)
    {
        int best = 0;
        if (party == null) return best;

        for (int i = 0; i < party.Count; i++)
        {
            var c = party[i];
            if (c == null) continue;
            if (c.level > best) best = c.level;
        }

        return best;
    }

    private int GetHighestPromotion(List<CharacterRuntime> party)
    {
        int best = 0;
        if (party == null) return best;

        for (int i = 0; i < party.Count; i++)
        {
            var c = party[i];
            if (c == null) continue;
            if (c.promotionStage > best) best = c.promotionStage;
        }

        return best;
    }

    private void NotifyUpdated()
    {
        OnQuestUpdated?.Invoke();
    }
}