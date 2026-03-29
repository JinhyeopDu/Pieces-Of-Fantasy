using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MaterialSlotView : MonoBehaviour
{
    [Header("Refs")]
    public Button button;                 // 슬롯 클릭
    public Image iconImage;
    public TMP_Text countText;

    [Header("Selection UI")]
    public GameObject selectedFrame;      // 하얀 테두리
    public Button minusButton;            // '-' 버튼 (포커스 슬롯에만 표시)

    [Header("Color Policy")]
    public Color enoughColor = Color.white;
    public Color insufficientColor = Color.red;

    // 이벤트(컨트롤러가 구독)
    public event Action<MaterialSlotView> OnClick;
    public event Action<MaterialSlotView> OnClickMinus;

    private void Awake()
    {
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnClick?.Invoke(this));
        }

        if (minusButton != null)
        {
            minusButton.onClick.RemoveAllListeners();
            minusButton.onClick.AddListener(() => OnClickMinus?.Invoke(this));
        }
    }

    public void SetIcon(Sprite sprite)
    {
        if (iconImage != null) iconImage.sprite = sprite;
    }

    /// <summary>
    /// 현재 단계 정책(요청 반영):
    /// - 표시는 selected/have 로 갈 예정(너의 다음 목표 3번)
    /// - 다만 “need”도 내부 로직에는 필요하니 파라미터는 유지
    /// </summary>
    public void SetCount(int selected, int need, int have)
    {
        if (countText == null) return;

        // 다음 단계 목표 반영: (selected / have)
        countText.text = $"{selected}/{have}";

        // 색상 정책(현재는 "need 달성 여부" 기준으로 충분/부족)
        bool enough = (selected >= need);
        countText.color = enough ? enoughColor : insufficientColor;
    }

    public void SetSelectedFrame(bool on)
    {
        if (selectedFrame != null) selectedFrame.SetActive(on);
    }

    public void SetMinusVisible(bool on)
    {
        if (minusButton != null) minusButton.gameObject.SetActive(on);
    }
}