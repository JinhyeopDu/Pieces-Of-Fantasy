using UnityEngine;
using UnityEngine.UI;

public class TutorialArrowController : MonoBehaviour
{
    public static TutorialArrowController I { get; private set; }

    [Header("UI")]
    [SerializeField] private RectTransform arrowRect;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Camera")]
    [SerializeField] private Camera targetCamera;

    [Header("Behavior")]
    [SerializeField] private float screenEdgePadding = 80f;
    [SerializeField] private bool hideWhenTargetVisible = true;
    [SerializeField] private float visibleCheckMargin = 0.05f;

    private Transform _target;

    void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;

        if (arrowRect == null)
            arrowRect = GetComponent<RectTransform>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (targetCamera == null)
            targetCamera = Camera.main;

        Hide();
    }

    void Update()
    {
        if (_target == null)
        {
            Hide();
            return;
        }

        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera == null)
        {
            Hide();
            return;
        }

        UpdateArrow();
    }

    public void SetTarget(Transform target)
    {
        _target = target;
        Debug.Log($"[TutorialArrow] SetTarget = {(target != null ? target.name : "NULL")}");

        if (_target != null)
            Show();
    }

    public void ClearTarget()
    {
        _target = null;
        Debug.Log("[TutorialArrow] ClearTarget");
        Hide();
    }

    private void UpdateArrow()
    {
        // Debug.Log($"[TutorialArrow] Updating Arrow -> target={_target.name}"); 
        Vector3 screenPos = targetCamera.WorldToScreenPoint(_target.position);

        bool isBehind = screenPos.z < 0f;
        if (isBehind)
        {
            screenPos *= -1f;
        }

        float screenW = Screen.width;
        float screenH = Screen.height;

        bool isVisible =
            !isBehind &&
            screenPos.x >= screenW * visibleCheckMargin &&
            screenPos.x <= screenW * (1f - visibleCheckMargin) &&
            screenPos.y >= screenH * visibleCheckMargin &&
            screenPos.y <= screenH * (1f - visibleCheckMargin);

        if (hideWhenTargetVisible && isVisible)
        {
            Hide();
            return;
        }

        Show();

        Vector2 screenCenter = new Vector2(screenW * 0.5f, screenH * 0.5f);
        Vector2 dir = ((Vector2)screenPos - screenCenter).normalized;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        arrowRect.rotation = Quaternion.Euler(0f, 0f, angle - 90f);

        Vector2 pos = screenCenter;

        float minX = screenEdgePadding;
        float maxX = screenW - screenEdgePadding;
        float minY = screenEdgePadding;
        float maxY = screenH - screenEdgePadding;

        if (Mathf.Abs(dir.x) > 0.001f)
        {
            float tX = dir.x > 0
                ? (maxX - screenCenter.x) / dir.x
                : (minX - screenCenter.x) / dir.x;

            float yAtX = screenCenter.y + dir.y * tX;
            if (yAtX >= minY && yAtX <= maxY)
            {
                pos = new Vector2(
                    dir.x > 0 ? maxX : minX,
                    yAtX
                );
                SetAnchoredPosition(pos);
                return;
            }
        }

        if (Mathf.Abs(dir.y) > 0.001f)
        {
            float tY = dir.y > 0
                ? (maxY - screenCenter.y) / dir.y
                : (minY - screenCenter.y) / dir.y;

            float xAtY = screenCenter.x + dir.x * tY;
            pos = new Vector2(
                Mathf.Clamp(xAtY, minX, maxX),
                dir.y > 0 ? maxY : minY
            );
        }

        SetAnchoredPosition(pos);
    }

    private void SetAnchoredPosition(Vector2 screenPos)
    {
        arrowRect.position = screenPos;
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