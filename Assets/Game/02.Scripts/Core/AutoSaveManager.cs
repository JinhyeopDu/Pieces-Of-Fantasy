using UnityEngine;

public class AutoSaveManager : MonoBehaviour
{
    public static AutoSaveManager I { get; private set; }

    [Header("Policy")]
    [Tooltip("자동 저장 최소 간격(초). 너무 자주 저장되는 것 방지")]
    [SerializeField] private float minInterval = 2.0f;

    [Tooltip("자동 저장 로그 출력")]
    [SerializeField] private bool verboseLog = true;

    private float _lastAutoSaveTime = -999f;
    private bool _isSaving;

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool TryAutoSave(string reason)
    {
        if (_isSaving)
        {
            if (verboseLog)
                Debug.Log($"[AutoSave] skipped (already saving). reason={reason}");
            return false;
        }

        if (Time.unscaledTime - _lastAutoSaveTime < minInterval)
        {
            if (verboseLog)
                Debug.Log($"[AutoSave] skipped (cooldown). reason={reason}");
            return false;
        }

        if (GameContext.I == null)
        {
            Debug.LogWarning($"[AutoSave] failed: GameContext.I is null. reason={reason}");
            return false;
        }

        _isSaving = true;

        try
        {
            GameContext.I.SaveGame();
            _lastAutoSaveTime = Time.unscaledTime;

            if (verboseLog)
                Debug.Log($"[AutoSave] success. reason={reason}");
        }
        finally
        {
            _isSaving = false;
        }

        return true;
    }
}