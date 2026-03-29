using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestPanelController : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Texts")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText; // 진행도 포함
    [SerializeField] private TMP_Text statusText;

    [Header("Reward List")]
    [SerializeField] private Transform rewardContentRoot;
    [SerializeField] private QuestRewardSlotView rewardSlotPrefab;

    [Header("Buttons")]
    [SerializeField] private Button claimButton;
    [SerializeField] private TMP_Text claimButtonText;
    [SerializeField] private Button closeButton;

    [Header("Button Labels")]
    [SerializeField] private string claimLabelAvailable = "보상 받기";
    [SerializeField] private string claimLabelDone = "수령 완료";
    [SerializeField] private string claimLabelUnavailable = "진행 중";

    [Header("Status Labels")]
    [SerializeField] private string statusLabelNoQuest = "모든 퀘스트 완료";
    [SerializeField] private string statusLabelInProgress = "진행 중";
    [SerializeField] private string statusLabelClaimable = "보상 수령 가능";
    [SerializeField] private string statusLabelClaimed = "수령 완료";

    [Header("Optional Disable While Open")]
    [SerializeField] private MonoBehaviour[] disableWhileOpen;

    [Header("Text Style")]
    [SerializeField] private bool colorProgressInDescription = true;
    [SerializeField] private string progressColorHex = "#00FF66";

    private readonly List<QuestRewardSlotView> _rewardSlots = new();

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        if (panelRoot == null)
            panelRoot = gameObject;

        if (claimButton != null)
        {
            claimButton.onClick.RemoveListener(OnClickClaim);
            claimButton.onClick.AddListener(OnClickClaim);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Close);
            closeButton.onClick.AddListener(Close);
        }

        SetOpen(false, true);
    }

    private void OnEnable()
    {
        if (QuestManager.I != null)
            QuestManager.I.OnQuestUpdated += Refresh;
    }

    private void OnDisable()
    {
        if (QuestManager.I != null)
            QuestManager.I.OnQuestUpdated -= Refresh;
    }

    public void Toggle()
    {
        SetOpen(!IsOpen);
    }

    public void Open()
    {
        SetOpen(true);
    }

    public void Close()
    {
        SetOpen(false);
    }

    public void SetOpen(bool open, bool instant = false)
    {
        if (IsOpen == open)
            return;

        if (open)
        {
            if (GameContext.I != null)
            {
                bool entered = GameContext.I.TryEnterOverlay(UIOverlayKind.QuestPanel);
                if (!entered)
                    return;
            }
        }

        IsOpen = open;

        if (panelRoot != null)
            panelRoot.SetActive(true);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = open ? 1f : 0f;
            canvasGroup.interactable = open;
            canvasGroup.blocksRaycasts = open;
        }

        ApplyDisableTargets(open);
        ApplyCursorPolicy(open);
        GameContext.I?.SetUIBlockingLook(open);

        if (open)
        {
            Debug.Log("[QuestPanel] Open -> Refresh()");
            Refresh();

            if (!instant)
                AudioManager.I?.PlaySFX2D(SFXKey.UI_Open);

            // 튜토리얼: 퀘스트창 열기 완료
            TutorialManager.I?.CompleteQuestTutorial();
        }
        else
        {
            GameContext.I?.ExitOverlay(UIOverlayKind.QuestPanel);

            if (!instant)
                AudioManager.I?.PlaySFX2D(SFXKey.UI_Close);

            if (panelRoot != null)
                panelRoot.SetActive(false);
        }
    }

    public void Refresh()
    {
        QuestData quest = QuestManager.I != null ? QuestManager.I.GetCurrentQuest() : null;
        QuestRuntimeProgress prog = QuestManager.I != null ? QuestManager.I.GetCurrentProgress() : null;
        if (QuestManager.I != null && quest == null && prog != null && !GameContext.I.allQuestsCompleted)
        {
            Debug.LogWarning("[QuestPanel] Inconsistent quest state detected. Reinitializing quest.");
            QuestManager.I.EnsureQuestInitialized();

            quest = QuestManager.I.GetCurrentQuest();
            prog = QuestManager.I.GetCurrentProgress();
        }
        Debug.Log($"[QuestPanel] Refresh called. quest={(quest != null ? quest.title : "NULL")}, prog={(prog != null ? "EXISTS" : "NULL")}, allDone={(GameContext.I != null ? GameContext.I.allQuestsCompleted : false)}");


        ClearRewardSlots();

        if (quest == null || prog == null)
        {
            bool allDone = GameContext.I != null && GameContext.I.allQuestsCompleted;

            if (allDone)
            {
                if (titleText != null)
                    titleText.text = "모든 퀘스트를 완료하셨습니다.";

                if (descriptionText != null)
                    descriptionText.text = "";

                if (statusText != null)
                    statusText.text = "";

                ClearRewardSlots();
                RefreshClaimButtonNoQuest();
                return;
            }

            if (titleText != null)
                titleText.text = "퀘스트 없음";

            if (descriptionText != null)
                descriptionText.text = "현재 진행 중인 퀘스트가 없습니다.";

            if (statusText != null)
                statusText.text = statusLabelNoQuest;

            RefreshClaimButtonNoQuest();
            return;
        }

        if (titleText != null)
            titleText.text = quest.title;

        if (descriptionText != null)
            descriptionText.text = BuildDescriptionWithProgress(quest);

        if (statusText != null)
            statusText.text = BuildStatusText(prog);

        BuildRewardSlots(quest);
        RefreshClaimButton(quest, prog);
    }

    private string BuildDescriptionWithProgress(QuestData quest)
    {
        if (quest == null)
            return string.Empty;

        string desc = string.IsNullOrEmpty(quest.description)
            ? quest.title
            : quest.description;

        string progress = QuestManager.I != null ? QuestManager.I.GetProgressText() : string.Empty;

        if (string.IsNullOrEmpty(progress))
            return desc;

        if (colorProgressInDescription)
            return $"{desc} <color={progressColorHex}>{progress}</color>";

        return $"{desc} {progress}";
    }

    private string BuildStatusText(QuestRuntimeProgress prog)
    {
        if (prog == null)
            return statusLabelNoQuest;

        if (prog.rewardClaimed)
            return statusLabelClaimed;

        if (prog.isCompleted)
            return statusLabelClaimable;

        return statusLabelInProgress;
    }

    private void RefreshClaimButtonNoQuest()
    {
        if (claimButton != null)
            claimButton.interactable = false;

        if (claimButtonText != null)
            claimButtonText.text = claimLabelDone;
    }

    private void RefreshClaimButton(QuestData quest, QuestRuntimeProgress prog)
    {
        if (claimButton == null)
            return;

        if (quest == null || prog == null)
        {
            claimButton.interactable = false;

            if (claimButtonText != null)
                claimButtonText.text = claimLabelUnavailable;
            return;
        }

        if (prog.rewardClaimed)
        {
            claimButton.interactable = false;

            if (claimButtonText != null)
                claimButtonText.text = claimLabelDone;
            return;
        }

        if (prog.isCompleted)
        {
            claimButton.interactable = true;

            if (claimButtonText != null)
                claimButtonText.text = claimLabelAvailable;
            return;
        }

        claimButton.interactable = false;

        if (claimButtonText != null)
            claimButtonText.text = claimLabelUnavailable;
    }

    private void BuildRewardSlots(QuestData quest)
    {
        if (quest == null || quest.rewards == null)
            return;

        if (rewardContentRoot == null || rewardSlotPrefab == null)
            return;

        for (int i = 0; i < quest.rewards.Count; i++)
        {
            var r = quest.rewards[i];
            if (r == null || r.item == null || r.amount <= 0)
                continue;

            var slot = Instantiate(rewardSlotPrefab, rewardContentRoot);
            slot.Bind(r.item, r.amount);
            _rewardSlots.Add(slot);
        }
    }

    private void ClearRewardSlots()
    {
        for (int i = 0; i < _rewardSlots.Count; i++)
        {
            if (_rewardSlots[i] != null)
                Destroy(_rewardSlots[i].gameObject);
        }

        _rewardSlots.Clear();
    }

    private void OnClickClaim()
    {
        AudioManager.I?.PlaySFX2D(SFXKey.UI_Click);

        if (QuestManager.I == null)
            return;

        if (!QuestManager.I.CanClaimReward())
            return;

        QuestManager.I.ClaimCurrentQuestReward();

        bool shouldShowEnding = (GameContext.I != null && GameContext.I.endingShown);

        if (shouldShowEnding)
        {
            // 마지막 퀘스트 보상 수령이면 퀘스트창을 먼저 닫는다.
            Close();

            var ending = FindFirstObjectByType<EndingPanelController>(FindObjectsInactive.Include);
            if (ending != null)
            {
                ending.ShowEnding();
            }
            else
            {
                Debug.LogWarning("[QuestPanel] EndingPanelController not found.");
            }
        }
        else
        {
            Refresh();
        }

        TutorialManager.I?.CheckAndShowNextTutorial();
    }

    private void ApplyDisableTargets(bool open)
    {
        if (disableWhileOpen == null || disableWhileOpen.Length == 0)
            return;

        for (int i = 0; i < disableWhileOpen.Length; i++)
        {
            if (disableWhileOpen[i] == null)
                continue;

            disableWhileOpen[i].enabled = !open;
        }
    }

    private void ApplyCursorPolicy(bool open)
    {
        if (open)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}