using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

/// <summary>
/// 플레이어 턴에서 행동(기본 공격 / 스킬)을 선택하는 UI.
/// 현재 프로젝트 범위에서는 궁극기 시스템을 사용하지 않으므로
/// 기본 공격과 일반 스킬 2가지만 선택할 수 있다.
/// </summary>
public class SkillSelectUI : MonoBehaviour
{
    [Header("Root Panel")]
    [Tooltip("행동 선택 UI 전체 루트 오브젝트")]
    public GameObject panel;

    [Header("Texts")]
    [Tooltip("현재 턴 캐릭터 이름 표시 텍스트")]
    public TMP_Text actorNameText;

    [Tooltip("행동 선택 안내 문구")]
    public TMP_Text instructionText;

    [Header("Buttons")]
    [Tooltip("기본 공격 버튼")]
    public Button basicAttackButton;

    [Tooltip("기본 공격 이름 표시 텍스트")]
    public TMP_Text basicAttackLabel;

    [Tooltip("일반 스킬 버튼")]
    public Button skillButton;

    [Tooltip("일반 스킬 이름 표시 텍스트")]
    public TMP_Text skillLabel;

    [Header("Skill Lock UI")]
    [Tooltip("스킬 포인트 부족 시 표시할 잠금 아이콘")]
    public GameObject skillLockIcon;

    [Header("References")]
    [Tooltip("스킬 포인트 부족 시 깜빡임 연출에 사용하는 UI")]
    public SkillPointPipUI skillPointPipUI;

    [Tooltip("중앙 메시지 출력 UI")]
    public BattleCenterMessageUI centerMessageUI;

    [Header("Skill Point Policy (UI)")]
    [Tooltip("일반 스킬 선택에 필요한 SP 비용")]
    public int skillCostSP = 1;

    [Header("Visual State")]
    [Tooltip("스킬 사용 가능 상태의 텍스트 색상")]
    public Color skillNormalTextColor = Color.white;

    [Tooltip("스킬 사용 불가 상태의 텍스트 색상")]
    public Color skillDisabledTextColor = new Color(0.75f, 0.75f, 0.75f, 1f);

    [Tooltip("스킬 사용 가능 상태의 버튼 색상")]
    public Color skillNormalButtonColor = Color.white;

    [Tooltip("스킬 사용 불가 상태의 버튼 색상")]
    public Color skillDisabledButtonColor = new Color(0.45f, 0.45f, 0.45f, 1f);

    [Header("Messages")]
    [Tooltip("스킬 포인트 부족 시 중앙 메시지로 보여줄 문구")]
    public string spLackMessage = "스킬 포인트가 부족합니다.";

    /// <summary>
    /// 행동 선택 결과를 BattleController 쪽으로 넘기는 콜백.
    /// 0 = 기본 공격, 1 = 스킬
    /// </summary>
    private Action<int> onSkillSelected;

    /// <summary>
    /// 현재 UI가 열려 있는지 여부.
    /// SP 변경 이벤트가 왔을 때, 이 값이 true면 즉시 상태를 갱신한다.
    /// </summary>
    private bool isShown;

    /// <summary>
    /// 현재 턴을 진행 중인 캐릭터 런타임 데이터.
    /// </summary>
    private BattleActorRuntime currentActor;

    private void Awake()
    {
        if (panel == null)
            panel = gameObject;

        panel.SetActive(false);

        if (skillLockIcon != null)
            skillLockIcon.SetActive(false);

        if (basicAttackButton != null)
            basicAttackButton.onClick.AddListener(() => SelectSkill(0));

        if (skillButton != null)
            skillButton.onClick.AddListener(() => SelectSkill(1));
    }

    private void OnEnable()
    {
        if (GameContext.I != null)
            GameContext.I.OnBattleSkillPointsChanged += HandleSPChanged;
    }

    private void OnDisable()
    {
        if (GameContext.I != null)
            GameContext.I.OnBattleSkillPointsChanged -= HandleSPChanged;
    }

    /// <summary>
    /// Battle SP 값이 바뀌면 현재 열려 있는 선택 UI의 사용 가능 상태를 다시 계산한다.
    /// </summary>
    private void HandleSPChanged(int cur, int max)
    {
        if (isShown)
            RefreshLabelsAndInteractable();
    }

    /// <summary>
    /// 특정 캐릭터 턴에서 행동 선택 UI를 열고 콜백을 등록한다.
    /// </summary>
    public void ShowFor(BattleActorRuntime actor, Action<int> callback)
    {
        currentActor = actor;
        onSkillSelected = callback;
        isShown = true;

        if (panel != null)
            panel.SetActive(true);

        if (skillLockIcon != null)
            skillLockIcon.SetActive(false);

        if (actorNameText != null)
            actorNameText.text = actor.data.displayName;

        if (instructionText != null)
            instructionText.text = "행동을 선택하세요";

        if (basicAttackLabel != null)
        {
            if (actor.data.basicAtk != null && !string.IsNullOrEmpty(actor.data.basicAtk.displayName))
                basicAttackLabel.text = actor.data.basicAtk.displayName;
            else
                basicAttackLabel.text = "기본 공격";
        }

        RefreshLabelsAndInteractable();
    }

    /// <summary>
    /// 행동 선택 UI를 닫고 내부 참조를 정리한다.
    /// </summary>
    public void Hide()
    {
        isShown = false;

        if (skillLockIcon != null)
            skillLockIcon.SetActive(false);

        if (panel != null)
            panel.SetActive(false);

        onSkillSelected = null;
        currentActor = null;
    }

    /// <summary>
    /// 현재 캐릭터 기준으로 버튼 활성화 상태와 라벨 텍스트를 갱신한다.
    /// </summary>
    private void RefreshLabelsAndInteractable()
    {
        if (currentActor == null || currentActor.data == null)
            return;

        // 기본 공격은 항상 선택 가능
        if (basicAttackButton != null)
            basicAttackButton.interactable = true;

        // ── Skill ─────────────────────────
        if (currentActor.data.skill != null)
        {
            // 클릭 자체는 허용하고, 부족할 경우 SelectSkill()에서 메시지를 보여준다.
            if (skillButton != null)
                skillButton.interactable = true;

            bool canUse = (GameContext.I == null)
                ? true
                : GameContext.I.CanSpendBattleSkillPoint(Mathf.Max(0, skillCostSP));

            if (skillLabel != null)
            {
                string name = string.IsNullOrEmpty(currentActor.data.skill.displayName)
                    ? "스킬"
                    : currentActor.data.skill.displayName;

                skillLabel.text = name;
            }

            SetSkillVisualState(canUse);
        }
        else
        {
            if (skillLabel != null)
            {
                skillLabel.text = "-";
                skillLabel.color = skillDisabledTextColor;
            }

            if (skillButton != null && skillButton.image != null)
                skillButton.image.color = skillDisabledButtonColor;

            if (skillLockIcon != null)
                skillLockIcon.SetActive(false);

            if (skillButton != null)
                skillButton.interactable = false;
        }
    }

    /// <summary>
    /// 스킬 사용 가능 여부에 따라 텍스트, 버튼 색상, 잠금 아이콘 상태를 변경한다.
    /// </summary>
    private void SetSkillVisualState(bool canUse)
    {
        if (skillLabel != null)
            skillLabel.color = canUse ? skillNormalTextColor : skillDisabledTextColor;

        if (skillButton != null && skillButton.image != null)
            skillButton.image.color = canUse ? skillNormalButtonColor : skillDisabledButtonColor;

        if (skillLockIcon != null)
            skillLockIcon.SetActive(!canUse);
    }

    /// <summary>
    /// 행동을 확정한다.
    /// 스킬 선택 시 SP가 부족하면 행동을 확정하지 않고 경고 UI만 보여준다.
    /// </summary>
    private void SelectSkill(int skillIndex)
    {
        if (!isShown)
            return;

        // 스킬(1): SP 부족이면 행동 확정 대신 별 UI + 중앙 메시지 표시
        if (skillIndex == 1)
        {
            if (currentActor == null || currentActor.data == null || currentActor.data.skill == null)
                return;

            int cost = Mathf.Max(0, skillCostSP);

            if (GameContext.I != null && cost > 0 && !GameContext.I.CanSpendBattleSkillPoint(cost))
            {
                if (skillPointPipUI != null)
                    skillPointPipUI.Flash();

                if (centerMessageUI != null)
                    centerMessageUI.ShowMessage(spLackMessage);

                RefreshLabelsAndInteractable();
                return;
            }
        }

        isShown = false;

        if (panel != null)
            panel.SetActive(false);

        var cb = onSkillSelected;
        onSkillSelected = null;
        cb?.Invoke(skillIndex);
    }
}