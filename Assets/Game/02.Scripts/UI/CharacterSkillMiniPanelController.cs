using UnityEngine;

public class CharacterSkillMiniPanelController : MonoBehaviour
{
    public enum SkillSlotType
    {
        None = 0,
        BattleSkill = 10,
        SecretArt = 20
    }

    [Header("Info Panel")]
    [SerializeField] private CharacterSkillInfoPanel infoPanel;
    [SerializeField] private RectTransform skillInfoPanelRect;

    [Header("Nodes")]
    [SerializeField] private CharacterSkillNodeView battleSkillNode;
    [SerializeField] private CharacterSkillNodeView secretArtNode;
    [SerializeField] private RectTransform skillNodeRootRect;

    private CharacterData _currentCharacter;
    private SkillSlotType _selectedSlot = SkillSlotType.None;

    private void Update()
    {
        if (_selectedSlot == SkillSlotType.None)
            return;

        if (!Input.GetMouseButtonDown(0))
            return;

        // 패널/노드 영역 안을 클릭한 경우는 유지
        if (IsPointerInsideSkillUI())
            return;

        // 그 외 영역 클릭 시 선택 해제
        ClearSelection();
    }

    private bool IsPointerInsideSkillUI()
    {
        Vector2 mousePos = Input.mousePosition;
        Camera uiCamera = null;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCamera = canvas.worldCamera;

        bool insideInfoPanel = false;
        bool insideNodeRoot = false;

        if (skillInfoPanelRect != null)
        {
            insideInfoPanel = RectTransformUtility.RectangleContainsScreenPoint(
                skillInfoPanelRect,
                mousePos,
                uiCamera
            );
        }

        if (skillNodeRootRect != null)
        {
            insideNodeRoot = RectTransformUtility.RectangleContainsScreenPoint(
                skillNodeRootRect,
                mousePos,
                uiCamera
            );
        }

        return insideInfoPanel || insideNodeRoot;
    }

    public void Bind(CharacterData characterData)
    {
        _currentCharacter = characterData;

        if (_currentCharacter == null)
        {
            Clear();
            return;
        }

        Sprite battleSkillIcon = _currentCharacter.skill != null ? _currentCharacter.skill.icon : null;
        Sprite secretArtIcon = _currentCharacter.secretArtIcon;

        // 기본 선택 없음
        _selectedSlot = SkillSlotType.None;

        if (battleSkillNode != null)
        {
            battleSkillNode.Bind(
                battleSkillIcon,
                this,
                SkillSlotType.BattleSkill,
                false
            );
        }

        if (secretArtNode != null)
        {
            secretArtNode.Bind(
                secretArtIcon,
                this,
                SkillSlotType.SecretArt,
                false
            );
        }

        infoPanel?.Clear();
        RefreshSelectionVisual();
    }

    public void PreviewSlot(SkillSlotType slotType)
    {
        ShowSlot(slotType);
    }

    public void SelectSlot(SkillSlotType slotType)
    {
        // 같은 슬롯을 다시 누르면 선택 해제
        if (_selectedSlot == slotType)
        {
            ClearSelection();
            return;
        }

        _selectedSlot = slotType;
        ShowSlot(_selectedSlot);
        RefreshSelectionVisual();
    }

    private void ClearSelection()
    {
        _selectedSlot = SkillSlotType.None;
        RefreshSelectionVisual();
        infoPanel?.Clear();
    }

    public void RestoreSelectedSlot()
    {
        if (_selectedSlot == SkillSlotType.None)
        {
            infoPanel?.Clear();
            return;
        }

        ShowSlot(_selectedSlot);
    }

    public void Clear()
    {
        _currentCharacter = null;
        _selectedSlot = SkillSlotType.None;

        if (battleSkillNode != null)
            battleSkillNode.gameObject.SetActive(false);

        if (secretArtNode != null)
            secretArtNode.gameObject.SetActive(false);

        infoPanel?.Clear();
    }

    private void RefreshSelectionVisual()
    {
        if (battleSkillNode != null)
            battleSkillNode.SetSelected(_selectedSlot == SkillSlotType.BattleSkill);

        if (secretArtNode != null)
            secretArtNode.SetSelected(_selectedSlot == SkillSlotType.SecretArt);
    }

    private void ShowSlot(SkillSlotType slotType)
    {
        if (_currentCharacter == null)
        {
            infoPanel?.Clear();
            return;
        }

        switch (slotType)
        {
            case SkillSlotType.BattleSkill:
                ShowBattleSkill();
                break;

            case SkillSlotType.SecretArt:
                ShowSecretArt();
                break;

            default:
                infoPanel?.Clear();
                break;
        }
    }

    private void ShowBattleSkill()
    {
        if (_currentCharacter.skill == null)
        {
            infoPanel?.Show("전투 스킬", "", "전투 스킬 데이터가 없습니다.");
            return;
        }

        SkillData skill = _currentCharacter.skill;

        string skillName = skill.displayName;
        string tag = GetBattleSkillTag(skill);
        string desc = GetBattleSkillDescription(skill);

        infoPanel?.Show(skillName, tag, desc);
    }

    private void ShowSecretArt()
    {
        string name = GetSecretArtName(_currentCharacter);
        string tag = GetSecretArtTag(_currentCharacter);
        string desc = GetSecretArtDescription(_currentCharacter);

        infoPanel?.Show(name, tag, desc);
    }

    private string GetBattleSkillTag(SkillData skill)
    {
        if (skill == null) return "";

        if (!string.IsNullOrEmpty(skill.tagText))
            return skill.tagText;

        return "[전투 스킬]";
    }

    private string GetBattleSkillDescription(SkillData skill)
    {
        if (skill == null)
            return "";

        switch (skill.actionType)
        {
            case SkillActionType.HealParty:
                return $"이슬 소리와 함께 치유의 기운을 퍼뜨려 자신 및 파티원 전체를 회복시킨다. 각 대상은 자신의 최대 체력을 기준으로 {(skill.healPercent * 100f):0}%만큼 회복한다.";

            case SkillActionType.SingleStrongHit:
                return $"크고 작은 얼음 기둥을 지면에서 솟아오르게 하여 단일 적을 강하게 타격한다. 대상에게 공격력의 {skill.power}%만큼 피해를 입힌다.";

            case SkillActionType.AoEHitAllEnemies:
                return $"여러 개의 작은 불덩어리를 발사해 전장을 휩쓸며, 필드에 있는 모든 적에게 공격력의 {skill.power}%만큼 피해를 입힌다.";

            case SkillActionType.BasicAttack:
                return $"대상에게 공격력의 {skill.power}%만큼 피해를 입힌다.";

            default:
                return !string.IsNullOrEmpty(skill.description)
                    ? skill.description
                    : "스킬 설명이 설정되지 않았습니다.";
        }
    }

    private string GetSecretArtName(CharacterData cd)
    {
        if (cd == null) return "비술";

        switch (cd.secretArtType)
        {
            case SecretArtType.HealParty:
                return "비술 - 치유의 기운";
            case SecretArtType.DefBuffParty:
                return "비술 - 방어의 결계";
            case SecretArtType.GainBattleSP:
                return "비술 - 전투 준비";
            default:
                return "비술";
        }
    }

    private string GetSecretArtTag(CharacterData cd)
    {
        if (cd == null) return "[비술]";

        switch (cd.secretArtType)
        {
            case SecretArtType.HealParty:
                return "[비술][회복]";
            case SecretArtType.DefBuffParty:
                return "[비술][강화]";
            case SecretArtType.GainBattleSP:
                return "[비술][지원]";
            default:
                return "[비술]";
        }
    }

    private string GetSecretArtDescription(CharacterData cd)
    {
        if (cd == null) return "";

        switch (cd.secretArtType)
        {
            case SecretArtType.HealParty:
                return $"전투 진입 전 사용 시, 아군 전체의 체력을 최대 체력의 {(cd.secretArtHealPercent * 100f):0}%만큼 회복합니다.";

            case SecretArtType.DefBuffParty:
                return $"전투 진입 전 사용 시, 아군 전체의 방어력을 기본 방어력의 {(cd.secretArtDefPercent * 100f):0}%만큼 증가시키며, {cd.secretArtDefTurns}턴 동안 유지됩니다.";

            case SecretArtType.GainBattleSP:
                return $"전투 진입 전 사용 시, 전투 시작 시 전투 스킬 포인트를 {cd.secretArtGainBattleSP}만큼 추가로 획득합니다.";

            default:
                return "이 캐릭터의 비술 설명이 아직 설정되지 않았습니다.";
        }
    }
}