using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneFader : MonoBehaviour
{
    public static SceneFader I { get; private set; }

    [Header("References")]
    public CanvasGroup fadeGroup;

    [Header("Lifecycle")]
    public bool dontDestroyOnLoad = true;

    [Header("Startup Fade In")]
    [Tooltip("게임 시작(첫 씬)에서 자동으로 Fade In 할지")]
    public bool fadeInOnStart = true;
    public float startFadeInTime = 1f; // 네 기존값 유지

    [Header("Scene Transition")]
    public float fadeOutTime = 0.28f;
    public float fadeInTime = 0.25f;
    public float holdBlackTime = 0.05f;

    [Header("Input Block")]
    public bool blockInputDuringFade = true;

    bool _isBusy;
    Coroutine _running;

    void Awake()
    {
        // Singleton
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }
        I = this;

        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);

        if (fadeGroup == null)
        {
            Debug.LogWarning("[SceneFader] fadeGroup이 비었습니다. Inspector에서 연결하세요.");
            return;
        }

        // 항상 켜두고 alpha만으로 제어 (씬 바뀌어도 유지)
        fadeGroup.gameObject.SetActive(true);

        // 시작 상태:
        // - start에서 FadeIn을 할 거면 검정(1)
        // - 안 할 거면 투명(0)
        fadeGroup.alpha = fadeInOnStart ? 1f : 0f;

        // 시작 시점 입력 차단 여부
        fadeGroup.interactable = false;
        fadeGroup.blocksRaycasts = (fadeInOnStart && blockInputDuringFade);
    }

    void Start()
    {
        // 첫 씬 자동 Fade In (네 기존 기능)
        if (fadeInOnStart && fadeGroup != null)
        {
            // 혹시 이미 다른 전환 중이면 건드리지 않음
            if (!_isBusy)
                _running = StartCoroutine(FadeInRealtime(startFadeInTime));
        }
    }

    /// <summary>
    /// 공통 씬 이동: FadeOut → LoadSceneAsync → FadeIn
    /// </summary>
    public void LoadSceneWithFade(string sceneName)
    {
        if (_isBusy) return;
        if (string.IsNullOrEmpty(sceneName)) return;

        _running = StartCoroutine(CoLoadSceneWithFade(sceneName, null));
    }

    /// <summary>
    /// 씬 로드 직후(black 상태에서) 콜백이 필요하면 이 버전 사용
    /// </summary>
    public void LoadSceneWithFade(string sceneName, Action onLoaded)
    {
        if (_isBusy) return;
        if (string.IsNullOrEmpty(sceneName)) return;

        _running = StartCoroutine(CoLoadSceneWithFade(sceneName, onLoaded));
    }

    IEnumerator CoLoadSceneWithFade(string sceneName, Action onLoaded)
    {
        if (fadeGroup == null) yield break;

        _isBusy = true;

        // 1) Fade Out
        yield return FadeOutRealtime(fadeOutTime);

        // 2) Hold black (씬 끊김 숨김)
        if (holdBlackTime > 0f)
            yield return new WaitForSecondsRealtime(holdBlackTime);

        // 3) Load
        var op = SceneManager.LoadSceneAsync(sceneName);
        if (op == null)
        {
            Debug.LogError($"[SceneFader] LoadSceneAsync 실패: {sceneName}");
            _isBusy = false;
            yield break;
        }

        while (!op.isDone)
            yield return null;

        // 로드 직후 블랙 상태에서 콜백 실행 (카메라/스폰/세팅용)
        onLoaded?.Invoke();

        // 이벤트시스템/캔버스 안정화 1프레임
        yield return null;

        // 4) Fade In
        yield return FadeInRealtime(fadeInTime);

        _isBusy = false;
        _running = null;
    }

    IEnumerator FadeOutRealtime(float duration)
    {
        if (fadeGroup == null) yield break;

        if (blockInputDuringFade)
        {
            fadeGroup.interactable = false;
            fadeGroup.blocksRaycasts = true;
        }

        float t = 0f;
        float dur = Mathf.Max(0.01f, duration);

        float start = fadeGroup.alpha;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / dur);
            fadeGroup.alpha = Mathf.Lerp(start, 1f, a);
            yield return null;
        }

        fadeGroup.alpha = 1f;
    }

    IEnumerator FadeInRealtime(float duration)
    {
        if (fadeGroup == null) yield break;

        float t = 0f;
        float dur = Mathf.Max(0.01f, duration);

        float start = fadeGroup.alpha;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / dur);
            fadeGroup.alpha = Mathf.Lerp(start, 0f, a);
            yield return null;
        }

        fadeGroup.alpha = 0f;

        // Fade 끝나고 입력 허용
        fadeGroup.blocksRaycasts = false;
    }

    public void ForceClear()
    {
        if (_running != null) StopCoroutine(_running);
        _running = null;
        _isBusy = false;

        if (fadeGroup != null)
        {
            fadeGroup.alpha = 0f;
            fadeGroup.blocksRaycasts = false;
        }
    }
}