using System;

[Serializable]
public class QuestRuntimeProgress
{
    public string questId;
    public int currentValue;
    public bool isCompleted;
    public bool rewardClaimed;

    public QuestRuntimeProgress(string id)
    {
        questId = id;
        currentValue = 0;
        isCompleted = false;
        rewardClaimed = false;
    }
}