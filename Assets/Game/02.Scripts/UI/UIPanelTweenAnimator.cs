using System.Collections;
using UnityEngine;

public class UIPanelTweenAnimator : MonoBehaviour
{
    public enum AnimType
    {
        SizeExpand,
        ScalePop,
        FadeOnly,
        ScaleAndFade
    }

    public enum PlayMode
    {
        ManualOnly,
        PlayOnEnable
    }

    [Header("Refs")]
    [SerializeField] private RectTransform targetPanel;
    [SerializeField] private GameObject contentRoot;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Mode")]
    [SerializeField] private AnimType animType = AnimType.SizeExpand;
    [SerializeField] private PlayMode playMode = PlayMode.PlayOnEnable;

    [Header("SizeExpand")]
    [SerializeField] private Vector2 targetSize = new Vector2(1000f, 500f);
    [SerializeField] private float startDotSize = 8f;
    [SerializeField] private float expandXTime = 0.12f;
    [SerializeField] private float expandYTime = 0.10f;
    [SerializeField] private float collapseYTime = 0.10f;
    [SerializeField] private float collapseXTime = 0.10f;

    [Header("Scale")]
    [SerializeField] private Vector3 closedScale = new Vector3(0.85f, 0.85f, 1f);
    [SerializeField] private Vector3 openScale = Vector3.one;
    [SerializeField] private float scaleOpenTime = 0.18f;
    [SerializeField] private float scaleCloseTime = 0.14f;

    [Header("Fade")]
    [SerializeField] private float fadeOpenTime = 0.15f;
    [SerializeField] private float fadeCloseTime = 0.12f;

    [Header("Common")]
    [SerializeField] private bool hideContentWhileOpening = true;
    [SerializeField] private bool hideContentBeforeClosing = true;
    [SerializeField] private bool deactivateOnClose = true;
    [SerializeField] private bool useUnscaledTime = true;

    private Coroutine _co;
    private bool _isOpen;

    public bool IsOpen => _isOpen;

    private void Reset()
    {
        if (targetPanel == null)
            targetPanel = GetComponent<RectTransform>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Awake()
    {
        PrepareInitialState();
    }

    private void OnEnable()
    {
        if (playMode == PlayMode.PlayOnEnable)
            PlayOpen();
    }

    private void OnDisable()
    {
        StopCurrent();
    }

    public void PlayOpen()
    {
        if (targetPanel == null)
        {
            Debug.LogWarning($"[UIPanelTweenAnimator] targetPanel is null on {name}");
            return;
        }

        gameObject.SetActive(true);

        StopCurrent();
        _co = StartCoroutine(Co_Open());
    }

    public void PlayClose()
    {
        if (targetPanel == null)
        {
            Debug.LogWarning($"[UIPanelTweenAnimator] targetPanel is null on {name}");
            return;
        }

        StopCurrent();
        _co = StartCoroutine(Co_Close());
    }

    public void Toggle()
    {
        if (_isOpen) PlayClose();
        else PlayOpen();
    }

    public void ShowImmediate()
    {
        StopCurrent();

        targetPanel.gameObject.SetActive(true);

        switch (animType)
        {
            case AnimType.SizeExpand:
                targetPanel.sizeDelta = targetSize;
                break;

            case AnimType.ScalePop:
                targetPanel.localScale = openScale;
                break;

            case AnimType.FadeOnly:
                EnsureCanvasGroup();
                if (canvasGroup != null) canvasGroup.alpha = 1f;
                break;

            case AnimType.ScaleAndFade:
                targetPanel.localScale = openScale;
                EnsureCanvasGroup();
                if (canvasGroup != null) canvasGroup.alpha = 1f;
                break;
        }

        SetContentVisible(true);
        _isOpen = true;
    }

    public void HideImmediate()
    {
        StopCurrent();

        SetContentVisible(false);

        switch (animType)
        {
            case AnimType.SizeExpand:
                targetPanel.sizeDelta = new Vector2(startDotSize, startDotSize);
                break;

            case AnimType.ScalePop:
                targetPanel.localScale = closedScale;
                break;

            case AnimType.FadeOnly:
                EnsureCanvasGroup();
                if (canvasGroup != null) canvasGroup.alpha = 0f;
                break;

            case AnimType.ScaleAndFade:
                targetPanel.localScale = closedScale;
                EnsureCanvasGroup();
                if (canvasGroup != null) canvasGroup.alpha = 0f;
                break;
        }

        _isOpen = false;

        if (deactivateOnClose)
            gameObject.SetActive(false);
    }

    private void PrepareInitialState()
    {
        if (targetPanel == null) return;

        switch (animType)
        {
            case AnimType.SizeExpand:
                targetPanel.sizeDelta = new Vector2(startDotSize, startDotSize);
                break;

            case AnimType.ScalePop:
                targetPanel.localScale = closedScale;
                break;

            case AnimType.FadeOnly:
                EnsureCanvasGroup();
                if (canvasGroup != null) canvasGroup.alpha = 0f;
                break;

            case AnimType.ScaleAndFade:
                targetPanel.localScale = closedScale;
                EnsureCanvasGroup();
                if (canvasGroup != null) canvasGroup.alpha = 0f;
                break;
        }

        SetContentVisible(false);
        _isOpen = false;
    }

    private IEnumerator Co_Open()
    {
        targetPanel.gameObject.SetActive(true);

        if (hideContentWhileOpening)
            SetContentVisible(false);

        switch (animType)
        {
            case AnimType.SizeExpand:
                targetPanel.sizeDelta = new Vector2(startDotSize, startDotSize);

                yield return LerpSize(
                    new Vector2(startDotSize, startDotSize),
                    new Vector2(targetSize.x, startDotSize),
                    expandXTime);

                yield return LerpSize(
                    new Vector2(targetSize.x, startDotSize),
                    targetSize,
                    expandYTime);
                break;

            case AnimType.ScalePop:
                targetPanel.localScale = closedScale;
                yield return LerpScale(closedScale, openScale, scaleOpenTime);
                break;

            case AnimType.FadeOnly:
                EnsureCanvasGroup();
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 0f;
                    yield return LerpAlpha(0f, 1f, fadeOpenTime);
                }
                break;

            case AnimType.ScaleAndFade:
                EnsureCanvasGroup();
                targetPanel.localScale = closedScale;
                if (canvasGroup != null) canvasGroup.alpha = 0f;

                IEnumerator scaleCo = LerpScale(closedScale, openScale, scaleOpenTime);
                IEnumerator fadeCo = LerpAlpha(0f, 1f, fadeOpenTime);

                while (scaleCo.MoveNext() | fadeCo.MoveNext())
                    yield return null;
                break;
        }

        SetContentVisible(true);
        _isOpen = true;
        _co = null;
    }

    private IEnumerator Co_Close()
    {
        if (hideContentBeforeClosing)
            SetContentVisible(false);

        switch (animType)
        {
            case AnimType.SizeExpand:
                yield return LerpSize(
                    targetSize,
                    new Vector2(targetSize.x, startDotSize),
                    collapseYTime);

                yield return LerpSize(
                    new Vector2(targetSize.x, startDotSize),
                    new Vector2(startDotSize, startDotSize),
                    collapseXTime);
                break;

            case AnimType.ScalePop:
                yield return LerpScale(openScale, closedScale, scaleCloseTime);
                break;

            case AnimType.FadeOnly:
                EnsureCanvasGroup();
                if (canvasGroup != null)
                    yield return LerpAlpha(1f, 0f, fadeCloseTime);
                break;

            case AnimType.ScaleAndFade:
                EnsureCanvasGroup();

                IEnumerator scaleCo = LerpScale(openScale, closedScale, scaleCloseTime);
                IEnumerator fadeCo = LerpAlpha(1f, 0f, fadeCloseTime);

                while (scaleCo.MoveNext() | fadeCo.MoveNext())
                    yield return null;
                break;
        }

        _isOpen = false;
        _co = null;

        if (deactivateOnClose)
            gameObject.SetActive(false);
    }

    private IEnumerator LerpSize(Vector2 from, Vector2 to, float duration)
    {
        if (duration <= 0f)
        {
            targetPanel.sizeDelta = to;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += DeltaTime();
            float u = Mathf.Clamp01(t / duration);
            targetPanel.sizeDelta = Vector2.LerpUnclamped(from, to, u);
            yield return null;
        }

        targetPanel.sizeDelta = to;
    }

    private IEnumerator LerpScale(Vector3 from, Vector3 to, float duration)
    {
        if (duration <= 0f)
        {
            targetPanel.localScale = to;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += DeltaTime();
            float u = Mathf.Clamp01(t / duration);
            targetPanel.localScale = Vector3.LerpUnclamped(from, to, u);
            yield return null;
        }

        targetPanel.localScale = to;
    }

    private IEnumerator LerpAlpha(float from, float to, float duration)
    {
        EnsureCanvasGroup();
        if (canvasGroup == null)
            yield break;

        if (duration <= 0f)
        {
            canvasGroup.alpha = to;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += DeltaTime();
            float u = Mathf.Clamp01(t / duration);
            canvasGroup.alpha = Mathf.LerpUnclamped(from, to, u);
            yield return null;
        }

        canvasGroup.alpha = to;
    }

    private void StopCurrent()
    {
        if (_co != null)
        {
            StopCoroutine(_co);
            _co = null;
        }
    }

    private void SetContentVisible(bool visible)
    {
        if (contentRoot != null)
            contentRoot.SetActive(visible);
    }

    private void EnsureCanvasGroup()
    {
        if (canvasGroup == null)
            canvasGroup = targetPanel != null ? targetPanel.GetComponent<CanvasGroup>() : null;
    }

    private float DeltaTime()
    {
        return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }
}