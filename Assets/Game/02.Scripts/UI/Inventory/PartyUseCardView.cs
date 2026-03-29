using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PartyUseCardView : MonoBehaviour
{
    [Header("UI Refs")]
    public Button button;
    public Image portraitImage;
    public TMP_Text nameText;

    [Header("HP Bar")]
    public RectTransform hpBarBack;
    public Image hpFill;
    public RectTransform hpGain;
    public Image hpGainFill;

    [Header("Selection")]
    public GameObject selectedFrame;

    [Header("Buff UX")]
    public GameObject alreadyAppliedBadge; // "이미 적용됨" 배지
    public GameObject disabledOverlay;     // 회색 처리용 오버레이(선택)

    [SerializeField] private GameObject hpBarRoot;

    private CanvasGroup _cg;

    int _index = -1;
    Action<int> _onClick;

    void Awake()
    {
        if (button == null) button = GetComponent<Button>();

        if (button != null)
        {
            button.onClick.RemoveListener(OnClick);
            button.onClick.AddListener(OnClick);
        }

        if (hpGainFill == null && hpGain != null)
            hpGainFill = hpGain.GetComponentInChildren<Image>(true);

        _cg = GetComponent<CanvasGroup>();
        if (_cg == null) _cg = gameObject.AddComponent<CanvasGroup>();
    }

    public void Bind(
     int partyIndex,
     string displayName,
     Sprite portrait,
     int hpBefore,
     int hpAfter,
     int maxHp,
     bool selected,
     Action<int> onClick,
     bool canClick,
     bool showDisabledVisual,
     bool alreadyApplied)
    {
        _index = partyIndex;
        _onClick = onClick;

        if (portraitImage != null)
        {
            portraitImage.sprite = portrait;
            portraitImage.enabled = (portrait != null);
            portraitImage.preserveAspect = true;
        }

        if (nameText != null)
            nameText.text = string.IsNullOrEmpty(displayName) ? "?" : displayName;

        SetSelected(selected);
        ApplyHpPreview_AddOnly(hpBefore, hpAfter, maxHp);
        ApplyBuffUx(canClick, showDisabledVisual, alreadyApplied);
    }

    private void ApplyBuffUx(bool canClick, bool showDisabledVisual, bool alreadyApplied)
    {
        if (alreadyAppliedBadge != null)
            alreadyAppliedBadge.SetActive(alreadyApplied);

        // 클릭 가능 여부만 제어
        if (button != null)
            button.interactable = canClick;

        // 어둡게 처리(시각적 비활성)는 별도 플래그로 제어
        if (disabledOverlay != null)
            disabledOverlay.SetActive(showDisabledVisual);

        if (_cg != null)
            _cg.alpha = showDisabledVisual ? 0.55f : 1f;
    }

    public void SetHpPreviewVisible(bool visible)
    {
        if (hpBarRoot != null) hpBarRoot.SetActive(visible);
    }

    public void SetSelected(bool on)
    {
        if (selectedFrame != null)
            selectedFrame.SetActive(on);
    }

    void ApplyHpPreview_AddOnly(int before, int after, int maxHp)
    {
        if (maxHp <= 0) maxHp = 1;

        before = Mathf.Clamp(before, 0, maxHp);
        after = Mathf.Clamp(after, 0, maxHp);

        int delta = Mathf.Max(0, after - before);

        float before01 = (float)before / maxHp;
        float delta01 = (float)delta / maxHp;

        if (hpFill != null)
            hpFill.fillAmount = before01;

        if (hpBarBack == null || hpGain == null)
            return;

        float barW = hpBarBack.rect.width;

        if (delta <= 0 || barW <= 0.01f)
        {
            hpGain.gameObject.SetActive(false);
            return;
        }

        hpGain.gameObject.SetActive(true);

        float startX = barW * before01;
        float widthW = barW * delta01;

        var pos = hpGain.anchoredPosition;
        pos.x = startX;
        hpGain.anchoredPosition = pos;

        var size = hpGain.sizeDelta;
        size.x = widthW;
        hpGain.sizeDelta = size;

        if (hpGainFill != null)
        {
            hpGainFill.enabled = true;
            hpGainFill.preserveAspect = false;
        }
    }

    void OnClick()
    {
        if (_index < 0) return;
        _onClick?.Invoke(_index);
    }
}