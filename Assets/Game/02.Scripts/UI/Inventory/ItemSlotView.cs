using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemSlotView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text countText;

    [Header("Selection")]
    [SerializeField] private GameObject selectionFrame;

    private ItemStack boundStack;
    private InventoryView owner;

    public ItemStack BoundStack => boundStack;

    public void Bind(ItemStack stack, InventoryView ownerView)
    {
        boundStack = stack;
        owner = ownerView;

        if (iconImage != null)
            iconImage.sprite = (stack.item != null) ? stack.item.icon : null;

        if (countText != null)
            countText.text = $"×{stack.count}";   // 인코딩 정상

        SetSelected(false);
    }

    public void OnClick()
    {
        AudioManager.I?.PlaySFX2D(SFXKey.UI_Click);
        owner?.SelectSlot(this);
    }

    public void SetSelected(bool selected)
    {
        if (selectionFrame != null)
            selectionFrame.SetActive(selected);
    }
}
