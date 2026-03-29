using System.Collections;
using UnityEngine;
using TMPro;

public class TutorialPanelController : MonoBehaviour
{
    [Header("Root")]
    public GameObject root;

    [Header("Canvas Group")]
    public CanvasGroup canvasGroup;

    [Header("Texts")]
    public TMP_Text titleText;
    public TMP_Text descriptionText;
    public TMP_Text hintText;

    [Header("Fade")]
    [SerializeField] private float fadeInDuration = 0.2f;
    [SerializeField] private float fadeOutDuration = 0.2f;

    private Coroutine _fadeCo;

    void Awake()
    {
        if (root == null)
            root = gameObject;

        if (canvasGroup == null && root != null)
            canvasGroup = root.GetComponent<CanvasGroup>();

        Debug.Log($"[TutorialPanel] Awake | root={(root != null ? root.name : "NULL")}");

        HideImmediate();
    }

    public void Show(string title, string description, string hint)
    {
        Debug.Log($"[TutorialPanel] Show called | root={(root != null ? root.name : "NULL")}");

        if (root == null)
            return;

        if (_fadeCo != null)
        {
            StopCoroutine(_fadeCo);
            _fadeCo = null;
        }

        root.SetActive(true);
        root.transform.SetAsLastSibling();

        if (titleText != null) titleText.text = title;
        if (descriptionText != null) descriptionText.text = description;
        if (hintText != null) hintText.text = hint;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        _fadeCo = StartCoroutine(CoFade(0f, 1f, fadeInDuration, disableOnEnd: false));

        var rt = root.GetComponent<RectTransform>();
        if (rt != null)
        {
            Debug.Log($"[TutorialPanel] Rect | anchored={rt.anchoredPosition} size={rt.rect.size} scale={rt.localScale}");
        }

        Debug.Log(
            $"[TutorialPanel] Show complete | activeSelf={root.activeSelf} | " +
            $"activeInHierarchy={root.activeInHierarchy}"
        );
    }

    public void Hide()
    {
        Debug.Log($"[TutorialPanel] Hide called | root={(root != null ? root.name : "NULL")}");

        if (root == null)
            return;

        // 이미 꺼져 있으면 코루틴 시작하지 않음
        if (!root.activeInHierarchy)
            return;

        if (_fadeCo != null)
        {
            StopCoroutine(_fadeCo);
            _fadeCo = null;
        }

        if (canvasGroup == null)
        {
            root.SetActive(false);
            return;
        }

        _fadeCo = StartCoroutine(CoFade(canvasGroup.alpha, 0f, fadeOutDuration, disableOnEnd: true));
    }

    public void HideImmediate()
    {
        if (root == null)
            return;

        if (_fadeCo != null)
        {
            StopCoroutine(_fadeCo);
            _fadeCo = null;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        root.SetActive(false);
    }

    private IEnumerator CoFade(float from, float to, float duration, bool disableOnEnd)
    {
        if (canvasGroup == null)
        {
            if (disableOnEnd)
                root.SetActive(false);

            _fadeCo = null;
            yield break;
        }

        float t = 0f;
        duration = Mathf.Max(0.01f, duration);

        canvasGroup.alpha = from;

        while (t < duration)
        {
            t += Time.deltaTime;
            float lerp = Mathf.Clamp01(t / duration);
            canvasGroup.alpha = Mathf.Lerp(from, to, lerp);
            yield return null;
        }

        canvasGroup.alpha = to;

        if (disableOnEnd && Mathf.Approximately(to, 0f))
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            root.SetActive(false);
        }
        else
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        _fadeCo = null;
    }
}