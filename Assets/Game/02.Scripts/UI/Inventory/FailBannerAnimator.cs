using System.Collections;
using TMPro;
using UnityEngine;

public class FailBannerAnimator : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform panel;
    [SerializeField] private TMP_Text messageText;

    [Header("Size")]
    [SerializeField] private Vector2 targetSize = new Vector2(1000f, 200f);
    [SerializeField] private float startDotSize = 8f;

    [Header("Timing")]
    [SerializeField] private float expandXTime = 0.12f;
    [SerializeField] private float expandYTime = 0.10f;
    [SerializeField] private float holdTime = 3.0f;
    [SerializeField] private float collapseYTime = 0.10f;
    [SerializeField] private float collapseXTime = 0.10f;

    private Coroutine _co;

    private void Awake()
    {
        HideImmediate();
    }

    private void OnDisable()
    {
        if (_co != null)
        {
            StopCoroutine(_co);
            _co = null;
        }

        if (panel != null)
            panel.sizeDelta = new Vector2(startDotSize, startDotSize);
    }

    public void Play(string msg)
    {
        if (panel == null)
        {
            Debug.LogWarning("[FailBannerAnimator] panel is null.");
            return;
        }

        gameObject.SetActive(true);
        panel.gameObject.SetActive(true);

        if (messageText != null)
        {
            messageText.text = msg;
            messageText.gameObject.SetActive(false);
        }

        if (_co != null)
            StopCoroutine(_co);

        _co = StartCoroutine(Co_Play());
    }

    public void HideImmediate()
    {
        if (_co != null)
        {
            StopCoroutine(_co);
            _co = null;
        }

        if (messageText != null)
            messageText.gameObject.SetActive(false);

        if (panel != null)
        {
            panel.sizeDelta = new Vector2(startDotSize, startDotSize);
            panel.gameObject.SetActive(false);
        }
    }

    private IEnumerator Co_Play()
    {
        panel.gameObject.SetActive(true);
        panel.sizeDelta = new Vector2(startDotSize, startDotSize);

        if (messageText != null)
            messageText.gameObject.SetActive(false);

        yield return LerpSize(
            new Vector2(startDotSize, startDotSize),
            new Vector2(targetSize.x, startDotSize),
            expandXTime);

        yield return LerpSize(
            new Vector2(targetSize.x, startDotSize),
            targetSize,
            expandYTime);

        // 3) 텍스트 ON + 유지
        if (messageText != null)
            messageText.gameObject.SetActive(true);

        yield return new WaitForSecondsRealtime(holdTime);

        // 4) 세로가 줄어들기 전에 텍스트 먼저 OFF
        if (messageText != null)
            messageText.gameObject.SetActive(false);

        // 5) 세로 접기
        yield return LerpSize(
            from: targetSize,
            to: new Vector2(targetSize.x, startDotSize),
            duration: collapseYTime);

        yield return LerpSize(
            new Vector2(targetSize.x, startDotSize),
            new Vector2(startDotSize, startDotSize),
            collapseXTime);

        HideImmediate();
    }

    private IEnumerator LerpSize(Vector2 from, Vector2 to, float duration)
    {
        if (duration <= 0f)
        {
            panel.sizeDelta = to;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / duration);
            panel.sizeDelta = Vector2.LerpUnclamped(from, to, u);
            yield return null;
        }

        panel.sizeDelta = to;
    }
}