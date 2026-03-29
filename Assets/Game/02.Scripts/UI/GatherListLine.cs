using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GatherListLine : MonoBehaviour
{
    public Image bg;
    public Image icon;
    public TMP_Text label;

    public IInteractable Target { get; private set; }

    public void Bind(IInteractable target, Sprite sprite, string text, bool selected)
    {
        Target = target;

        if (icon) icon.sprite = sprite;
        if (label) label.text = text;

        SetSelected(selected);
    }

    public void SetSelected(bool selected)
    {
        if (!bg) return;
        var c = bg.color;
        c.a = selected ? 0.75f : 0.35f;
        bg.color = c;
    }
}
