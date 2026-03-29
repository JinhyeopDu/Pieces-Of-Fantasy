using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CharacterSkillNodeView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Image selectedRing;
    [SerializeField] private Button button;

    private CharacterSkillMiniPanelController _owner;
    private CharacterSkillMiniPanelController.SkillSlotType _slotType;

    public CharacterSkillMiniPanelController.SkillSlotType SlotType => _slotType;

    public void Bind(
        Sprite icon,
        CharacterSkillMiniPanelController owner,
        CharacterSkillMiniPanelController.SkillSlotType slotType,
        bool selected)
    {
        _owner = owner;
        _slotType = slotType;

        if (iconImage != null)
            iconImage.sprite = icon;

        SetSelected(selected);

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClickNode);
        }

        gameObject.SetActive(icon != null);
    }

    public void SetSelected(bool selected)
    {
        if (selectedRing != null)
            selectedRing.enabled = selected;
    }

    private void OnClickNode()
    {
        if (_owner == null) return;
        _owner.SelectSlot(_slotType);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_owner == null) return;
        _owner.PreviewSlot(_slotType);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_owner == null) return;
        _owner.RestoreSelectedSlot();
    }
}