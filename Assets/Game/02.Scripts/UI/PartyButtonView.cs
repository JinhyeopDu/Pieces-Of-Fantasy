using UnityEngine;
using UnityEngine.UI;

public class PartyButtonView : MonoBehaviour
{
    public Button button;

    [Header("UI")]
    public Image portraitImage;
    public GameObject selectedFrame; // 선택 강조(있으면)

    public void Set(Sprite portrait)
    {
        if (portraitImage != null) portraitImage.sprite = portrait;
    }

    public void SetSelected(bool selected)
    {
        if (selectedFrame != null) selectedFrame.SetActive(selected);
    }
}