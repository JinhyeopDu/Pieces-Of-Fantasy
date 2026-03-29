using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RewardToastLine : MonoBehaviour
{
    [Header("Refs")]
    public Image background;
    public Image icon;
    public TMP_Text nameText;
    public TMP_Text qtyText;

    private CanvasGroup _cg;
    private Coroutine _fadeOutCo;
    private Coroutine _fadeInCo;
    private bool _isQuittingOrDestroyed;

    void Awake()
    {
        _cg = GetComponent<CanvasGroup>();
        if (_cg == null) _cg = gameObject.AddComponent<CanvasGroup>();
        _cg.alpha = 1f;
    }

    private void OnDisable()
    {
        StopAllSafeCoroutines();
    }

    private void OnDestroy()
    {
        _isQuittingOrDestroyed = true;
        StopAllSafeCoroutines();
    }

    public void Set(ItemData item, int qty)
    {
        if (item == null) return;

        if (icon != null)
        {
            icon.sprite = item.icon;
            icon.enabled = (icon.sprite != null);
            icon.preserveAspect = true;
        }

        if (nameText != null)
            nameText.text = string.IsNullOrEmpty(item.displayName) ? item.name : item.displayName;

        if (qtyText != null)
            qtyText.text = $"x{qty}";

        ApplyBackgroundColor(item.category);
    }

    public void SetSummary(string summaryText)
    {
        if (nameText != null)
            nameText.text = summaryText;

        if (icon != null)
        {
            icon.sprite = null;
            icon.enabled = false;
        }

        if (qtyText != null)
            qtyText.text = "";

        if (background != null)
            background.color = new Color(1f, 1f, 1f, 0.16f);
    }

    private void ApplyBackgroundColor(ItemCategory category)
    {
        if (background == null) return;

        switch (category)
        {
            case ItemCategory.Material:
                background.color = new Color(0.55f, 0.55f, 0.60f, 0.22f);
                break;

            case ItemCategory.Consumable:
                background.color = new Color(0.35f, 0.85f, 0.45f, 0.22f);
                break;

            case ItemCategory.KeyItem:
                background.color = new Color(1f, 0.75f, 0.2f, 0.25f);
                break;

            default:
                background.color = new Color(1f, 1f, 1f, 0.18f);
                break;
        }
    }

    public void PlayFadeOut(float fadeTime)
    {
        if (!isActiveAndEnabled || _isQuittingOrDestroyed) return;

        if (_fadeOutCo != null)
            StopCoroutine(_fadeOutCo);

        _fadeOutCo = StartCoroutine(CoFadeOut(fadeTime));
    }

    public void PlayFadeInPop(float t = 0.18f, float startScale = 0.96f)
    {
        if (!isActiveAndEnabled || _isQuittingOrDestroyed) return;

        if (_fadeInCo != null)
            StopCoroutine(_fadeInCo);

        _fadeInCo = StartCoroutine(CoFadeInPop(t, startScale));
    }

    public IEnumerator CoFadeOut(float fadeTime)
    {
        if (_isQuittingOrDestroyed || !this || !gameObject) yield break;
        if (_cg == null) yield break;

        float t = 0f;
        float start = _cg.alpha;

        while (t < fadeTime)
        {
            if (_isQuittingOrDestroyed || !this || !gameObject || _cg == null)
                yield break;

            t += Time.deltaTime;

            float k = (fadeTime <= 0f) ? 1f : (t / fadeTime);
            _cg.alpha = Mathf.Lerp(start, 0f, k);

            yield return null;
        }

        if (_isQuittingOrDestroyed || !this || !gameObject || _cg == null)
            yield break;

        _cg.alpha = 0f;
        _fadeOutCo = null;
    }

    public IEnumerator CoFadeInPop(float t = 0.18f, float startScale = 0.96f)
    {
        if (_isQuittingOrDestroyed || !this || !gameObject) yield break;
        if (_cg == null) yield break;

        _cg.alpha = 0f;
        transform.localScale = Vector3.one * startScale;

        float time = 0f;
        while (time < t)
        {
            if (_isQuittingOrDestroyed || !this || !gameObject || _cg == null)
                yield break;

            time += Time.deltaTime;
            float k = (t <= 0f) ? 1f : (time / t);

            _cg.alpha = Mathf.Lerp(0f, 1f, k);
            transform.localScale = Vector3.Lerp(Vector3.one * startScale, Vector3.one, k);

            yield return null;
        }

        if (_isQuittingOrDestroyed || !this || !gameObject || _cg == null)
            yield break;

        _cg.alpha = 1f;
        transform.localScale = Vector3.one;
        _fadeInCo = null;
    }

    private void StopAllSafeCoroutines()
    {
        if (_fadeOutCo != null)
        {
            StopCoroutine(_fadeOutCo);
            _fadeOutCo = null;
        }

        if (_fadeInCo != null)
        {
            StopCoroutine(_fadeInCo);
            _fadeInCo = null;
        }
    }
}