using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DetailPanelView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private TMP_Text descriptionText;

    [SerializeField] private GameObject useButtonRoot;
    [SerializeField] private UnityEngine.UI.Button useButton;

    [SerializeField] private ItemUsePopupController popupController;

    private ItemStack currentStack;

    public void Show(ItemStack stack)
    {
        currentStack = stack;

        if (stack == null || stack.item == null)
        {
            Clear();
            return;
        }

        // 기존 아이콘/이름/설명 세팅 로직 유지
        if (iconImage != null) iconImage.sprite = stack.item.icon;
        if (nameText != null) nameText.text = stack.item.displayName;
        if (countText != null)
        {
            if (stack.count > 0)
                countText.text = $"보유 : {stack.count}개";
            else
                countText.text = "";
        }
        if (descriptionText != null) descriptionText.text = stack.item.description;

        // 기존 아이콘/이름/설명 세팅 로직 유지

        bool canUse =
            stack.item.itemType == ItemType.Consumable &&
            stack.item.useScope == ItemUseScope.ExplorationOnly;

        if (useButtonRoot != null)
            useButtonRoot.SetActive(canUse);

        if (canUse && useButton != null)
        {
            useButton.onClick.RemoveAllListeners();
            useButton.onClick.AddListener(OnClickUse);
        }
    }

    private void OnClickUse()
    {
        if (currentStack == null || currentStack.item == null)
            return;

        popupController.Open(currentStack.item);

    }

    public void Clear()
    {
        currentStack = null;

        if (useButtonRoot != null)
            useButtonRoot.SetActive(false);

        if (iconImage != null) iconImage.sprite = null;
        if (nameText != null) nameText.text = "";
        if (countText != null) countText.text = "";
        if (descriptionText != null) descriptionText.text = "";
    }
}
