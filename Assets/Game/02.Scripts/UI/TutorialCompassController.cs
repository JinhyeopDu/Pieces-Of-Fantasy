using UnityEngine;
using UnityEngine.UI;

public class TutorialCompassController : MonoBehaviour
{
    public static TutorialCompassController I { get; private set; }

    [Header("UI")]
    [SerializeField] private RectTransform rootRect;
    [SerializeField] private RectTransform arrowRect;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("References")]
    [SerializeField] private Camera targetCamera;

    private Transform _player;
    private Transform _target;

    void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;

        if (rootRect == null)
            rootRect = GetComponent<RectTransform>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (targetCamera == null)
            targetCamera = Camera.main;

        Hide();
    }

    void Update()
    {
        if (_player == null || _target == null)
        {
            Hide();
            return;
        }

        if (targetCamera == null)
            targetCamera = Camera.main;

        UpdateCompass();
    }

    public void SetTargets(Transform player, Transform target)
    {
        _player = player;
        _target = target;

        if (_player != null && _target != null)
            Show();
        else
            Hide();
    }

    public void ClearTargets()
    {
        _player = null;
        _target = null;
        Hide();
    }

    private void UpdateCompass()
    {
        if (arrowRect == null || _player == null || _target == null)
            return;

        Vector3 toTarget = _target.position - _player.position;
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude < 0.0001f)
            return;

        toTarget.Normalize();

        Vector3 camForward = targetCamera != null ? targetCamera.transform.forward : Vector3.forward;
        Vector3 camRight = targetCamera != null ? targetCamera.transform.right : Vector3.right;

        camForward.y = 0f;
        camRight.y = 0f;

        camForward.Normalize();
        camRight.Normalize();

        float x = Vector3.Dot(toTarget, camRight);
        float y = Vector3.Dot(toTarget, camForward);

        float angle = Mathf.Atan2(x, y) * Mathf.Rad2Deg;
        arrowRect.localRotation = Quaternion.Euler(0f, 0f, -angle);
    }

    private void Show()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
    }

    private void Hide()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
    }
}