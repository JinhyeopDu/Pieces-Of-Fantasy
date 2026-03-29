using UnityEngine;

public class TutorialPlayerGuideController : MonoBehaviour
{
    public static TutorialPlayerGuideController I { get; private set; }

    [Header("UI")]
    [SerializeField] private RectTransform rootRect;
    [SerializeField] private RectTransform arrowRect;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Canvas parentCanvas;

    [Header("References")]
    [SerializeField] private Camera targetCamera;

    [Header("Behavior")]
    [SerializeField] private float radius = 55f;
    [SerializeField] private Vector3 targetWorldOffset = new Vector3(0f, 0.8f, 0f);
    [SerializeField] private Vector2 rootScreenOffset = new Vector2(0f, 40f);
    [SerializeField] private bool hideWhenTargetVeryClose = true;
    [SerializeField] private float hideDistance = 2.5f;
    [SerializeField] private float missingPlayerGraceTime = 0.25f;
    [SerializeField] private float arrowAngleOffset = -90f;

    [Header("Retarget")]
    [SerializeField] private bool autoRetargetWhenCurrentTargetInvalid = true;
    [SerializeField] private bool autoRetargetWhenHiddenByDistance = true;

    [Header("Anchor")]
    [SerializeField] private string guideAnchorName = "GuideAnchor";

    private Transform _player;
    private Transform _playerGuideAnchor;
    private Transform _target;
    private TutorialTargetType _targetType = TutorialTargetType.None;
    private float _missingPlayerTimer = 0f;

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

        if (parentCanvas == null)
            parentCanvas = GetComponentInParent<Canvas>();

        if (targetCamera == null)
            targetCamera = Camera.main;

        Hide();
    }

    void LateUpdate()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera == null)
        {
            Hide();
            return;
        }

        if (_player == null || _playerGuideAnchor == null)
        {
            TryResolvePlayer();

            if (_player == null || _playerGuideAnchor == null)
            {
                _missingPlayerTimer += Time.unscaledDeltaTime;

                if (_missingPlayerTimer >= missingPlayerGraceTime)
                    Hide();

                return;
            }
        }

        _missingPlayerTimer = 0f;

        if (_target == null || !_target.gameObject.activeInHierarchy)
        {
            bool retargeted = TryRetarget();

            if (!retargeted)
            {
                Hide();
                return;
            }
        }

        UpdateGuide();
    }

    public void SetTargets(Transform player, Transform target, TutorialTargetType targetType)
    {
        _player = player;
        _target = target;
        _targetType = targetType;
        _playerGuideAnchor = FindGuideAnchor(_player);
        _missingPlayerTimer = 0f;

        if (_player != null && _playerGuideAnchor != null && _target != null)
            Show();
        else
            Hide();
    }

    public void SetPlayer(Transform player)
    {
        _player = player;
        _playerGuideAnchor = FindGuideAnchor(_player);
        _missingPlayerTimer = 0f;

        if (_player != null && _playerGuideAnchor != null && _target != null)
            Show();
    }

    public void ClearTargets()
    {
        _player = null;
        _playerGuideAnchor = null;
        _target = null;
        _targetType = TutorialTargetType.None;
        _missingPlayerTimer = 0f;
        Hide();
    }

    private void UpdateGuide()
    {
        if (_target == null || !_target.gameObject.activeInHierarchy)
        {
            Hide();
            return;
        }

        if (rootRect == null || arrowRect == null || parentCanvas == null)
            return;

        if (_playerGuideAnchor == null)
        {
            Hide();
            return;
        }

        Vector3 playerBodyWorld = _playerGuideAnchor.position;
        Vector3 playerBodyScreen = targetCamera.WorldToScreenPoint(playerBodyWorld);

        if (playerBodyScreen.z <= 0f)
        {
            Hide();
            return;
        }

        float worldDistance = Vector3.Distance(_player.position, _target.position);
        if (hideWhenTargetVeryClose && worldDistance <= hideDistance)
        {
            if (autoRetargetWhenHiddenByDistance)
            {
                bool retargeted = TryRetarget();

                // retarget가 되더라도 여전히 너무 가까우면 그냥 숨김
                if (_target == null)
                {
                    Hide();
                    return;
                }

                worldDistance = Vector3.Distance(_player.position, _target.position);
                if (worldDistance <= hideDistance)
                {
                    Hide();
                    return;
                }
            }
            else
            {
                Hide();
                return;
            }
        }

        Vector3 targetWorld = _target.position + targetWorldOffset;
        Vector3 targetScreen = targetCamera.WorldToScreenPoint(targetWorld);

        Camera uiCamera = null;
        if (parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCamera = parentCanvas.worldCamera;

        RectTransform canvasRect = parentCanvas.GetComponent<RectTransform>();

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            playerBodyScreen,
            uiCamera,
            out Vector2 playerLocalPoint))
        {
            Hide();
            return;
        }

        Vector2 rootScreenOffset = new Vector2(0f, 40f);

        rootRect.anchoredPosition = playerLocalPoint + rootScreenOffset;
        rootRect.localRotation = Quaternion.identity;

        Vector2 dir2D = (Vector2)(targetScreen - playerBodyScreen);

        if (dir2D.sqrMagnitude <= 0.001f)
        {
            Hide();
            return;
        }

        dir2D.Normalize();

        arrowRect.anchoredPosition = dir2D * radius;

        float angle = Mathf.Atan2(dir2D.y, dir2D.x) * Mathf.Rad2Deg;
        arrowRect.localRotation = Quaternion.Euler(0f, 0f, angle + arrowAngleOffset);

        Show();
    }

    private void TryResolvePlayer()
    {
        if (_player == null)
        {
            var playerController = FindObjectOfType<PlayerControllerHumanoid>();
            if (playerController != null)
                _player = playerController.transform;
        }

        if (_player != null && _playerGuideAnchor == null)
        {
            _playerGuideAnchor = FindGuideAnchor(_player);
        }
    }

    private Transform FindGuideAnchor(Transform playerRoot)
    {
        if (playerRoot == null)
            return null;

        Transform found = playerRoot.Find(guideAnchorName);
        if (found != null)
            return found;

        Transform[] children = playerRoot.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name == guideAnchorName)
                return children[i];
        }

        return null;
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

    private bool TryRetarget()
    {
        if (!autoRetargetWhenCurrentTargetInvalid)
            return false;

        if (_player == null)
            return false;

        if (_targetType == TutorialTargetType.None)
            return false;

        if (TutorialHighlighter.I == null)
            return false;

        Transform newTarget = TutorialHighlighter.I.GetNearestTargetPoint(_targetType, _player.position);
        if (newTarget == null)
            return false;

        _target = newTarget;
        return true;
    }
}