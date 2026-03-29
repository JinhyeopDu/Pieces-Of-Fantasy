// Assets/Game/02.Scripts/UI/MapIconView.cs
using UnityEngine;
using UnityEngine.UI;

public class MapIconView : MonoBehaviour
{
    [Header("UI")]
    public RectTransform rect;
    public Image image;

    public void EnsureRefs()
    {
        if (rect == null) rect = GetComponent<RectTransform>();
        if (image == null) image = GetComponent<Image>();
    }

    public void SetStyle(Sprite sprite, Color color, Vector2 size)
    {
        EnsureRefs();
        if (image != null)
        {
            image.sprite = sprite;
            image.color = color;
        }
        if (rect != null)
        {
            rect.sizeDelta = size;
        }
    }

    public void SetViewportPosition(Vector3 viewport01)
    {
        // viewport (0~1) 를 부모 RectTransform 내 좌표로 변환
        if (rect == null) return;

        var parent = rect.parent as RectTransform;
        if (parent == null) return;

        // (0,0) = 좌하단, (1,1) = 우상단
        float x = (viewport01.x - 0.5f) * parent.rect.width;
        float y = (viewport01.y - 0.5f) * parent.rect.height;

        rect.anchoredPosition = new Vector2(x, y);
    }
}