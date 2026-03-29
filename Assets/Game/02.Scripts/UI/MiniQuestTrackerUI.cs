using TMPro;
using UnityEngine;

public class MiniQuestTrackerUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Texts")]
    [SerializeField] private TMP_Text questTitleText;
    [SerializeField] private TMP_Text questDescriptionText;
    [SerializeField] private TMP_Text questProgressText;

    [Header("Options")]
    [SerializeField] private bool refreshOnEnable = true;

    private void OnEnable()
    {
        if (QuestManager.I != null)
            QuestManager.I.OnQuestUpdated += Refresh;

        if (refreshOnEnable)
            Refresh();
    }

    private void OnDisable()
    {
        if (QuestManager.I != null)
            QuestManager.I.OnQuestUpdated -= Refresh;
    }

    private void Start()
    {
        Refresh();
    }

    public void Refresh()
    {
        var g = GameContext.I;
        var qm = QuestManager.I;

        if (root == null)
            root = gameObject;

        if (g == null || qm == null)
        {
            root.SetActive(false);
            return;
        }

        if (g.allQuestsCompleted)
        {
            root.SetActive(false);
            return;
        }

        QuestData quest = qm.GetCurrentQuest();
        QuestRuntimeProgress prog = qm.GetCurrentProgress();

        if (quest == null || prog == null)
        {
            root.SetActive(false);
            return;
        }

        root.SetActive(true);

        if (questTitleText != null)
            questTitleText.text = quest.title;

        if (questDescriptionText != null)
            questDescriptionText.text = quest.description;

        if (questProgressText != null)
            questProgressText.text = qm.GetProgressText();
    }
}