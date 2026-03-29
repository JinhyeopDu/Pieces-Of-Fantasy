using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestRewardSlotView : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text countText;

    public void Bind(ItemData item, int amount)
    {
        if (iconImage != null)
        {
            iconImage.sprite = item != null ? item.icon : null;
            iconImage.enabled = iconImage.sprite != null;
            iconImage.preserveAspect = true;
        }

        if (countText != null)
        {
            countText.text = $"x{Mathf.Max(0, amount)}";
        }
    }
}