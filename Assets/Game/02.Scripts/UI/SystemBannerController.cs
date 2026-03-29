// Assets/Game/02.Scripts/UI/SystemBannerController.cs
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SystemBannerController : MonoBehaviour
{
    [Header("Root")]
    [Tooltip("배너 전체 루트. 비워두면 자기 자신(GameObject)을 사용합니다.")]
    public GameObject root;

    [Header("Message UI")]
    [Tooltip("TMP 텍스트를 쓰는 경우 연결")]
    public TMP_Text messageTextTMP;

    [Tooltip("기본 Text를 쓰는 경우 연결 (TMP 미사용 시)")]
    public Text messageTextLegacy;

    [Header("Timing")]
    [Tooltip("배너가 화면에 유지되는 시간")]
    public float defaultDuration = 1.6f;

    [Tooltip("표시/숨김에 Time.timeScale 영향을 받지 않게 할지 여부")]
    public bool useUnscaledTime = true;

    [Header("Optional")]
    [Tooltip("시작 시 자동으로 숨김")]
    public bool hideOnStart = true;

    private Coroutine _showRoutine;

    private void Awake()
    {
        if (root == null)
            root = gameObject;
    }

    private void Start()
    {
        if (hideOnStart)
            HideImmediate();
    }

    /// <summary>
    /// 기본 시간으로 메시지 표시
    /// </summary>
    public void ShowMessage(string message)
    {
        ShowMessage(message, defaultDuration);
    }

    /// <summary>
    /// 지정한 시간 동안 메시지 표시 후 자동 숨김
    /// </summary>
    public void ShowMessage(string message, float duration)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        SetMessageText(message);

        if (root != null && !root.activeSelf)
            root.SetActive(true);

        if (_showRoutine != null)
            StopCoroutine(_showRoutine);

        _showRoutine = StartCoroutine(Co_ShowThenHide(duration));
    }

    /// <summary>
    /// 메시지를 표시하되 자동으로 숨기지 않음
    /// </summary>
    public void ShowPersistent(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        SetMessageText(message);

        if (_showRoutine != null)
        {
            StopCoroutine(_showRoutine);
            _showRoutine = null;
        }

        if (root != null && !root.activeSelf)
            root.SetActive(true);
    }

    /// <summary>
    /// 즉시 숨김
    /// </summary>
    public void HideImmediate()
    {
        if (_showRoutine != null)
        {
            StopCoroutine(_showRoutine);
            _showRoutine = null;
        }

        if (root != null)
            root.SetActive(false);
    }

    private IEnumerator Co_ShowThenHide(float duration)
    {
        if (duration < 0f)
            duration = 0f;

        if (useUnscaledTime)
            yield return new WaitForSecondsRealtime(duration);
        else
            yield return new WaitForSeconds(duration);

        if (root != null)
            root.SetActive(false);

        _showRoutine = null;
    }

    private void SetMessageText(string message)
    {
        if (messageTextTMP != null)
            messageTextTMP.text = message;

        if (messageTextLegacy != null)
            messageTextLegacy.text = message;
    }
}