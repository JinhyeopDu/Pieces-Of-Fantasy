// Assets/Game/02.Scripts/UI/MiniMapController.cs
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MiniMapController : MonoBehaviour
{
    public static MiniMapController I { get; private set; }

    [Header("Target (Player)")]
    [SerializeField] private Transform target;
    [SerializeField] private string playerTag = "Player";

    [Header("MiniMap Camera (Follow)")]
    [SerializeField] private Camera miniMapCamera;
    [SerializeField] private float miniMapFixedHeight = 30f;
    [SerializeField] private float miniMapOrthoSize = 25f;

    [Header("MiniMap UI")]
    [Tooltip("MiniMapPanel/RawImage 의 RectTransform (없어도 동작은 함)")]
    [SerializeField] private RectTransform miniMapViewportRect;
    [Tooltip("MiniMapPanel/IconsRoot/PlayerArrow")]
    [SerializeField] private RectTransform playerArrow;
    [Tooltip("true=북쪽고정(카메라 고정 + 화살표 회전), false=플레이어기준(카메라 회전 + 화살표 고정)")]
    [SerializeField] private bool northUp = true;

    [Header("WorldMap Camera (Fixed)")]
    [Tooltip("WorldMapCamera (인스펙터 위치/사이즈를 그대로 유지, 코드에서 건드리지 않음)")]
    [SerializeField] private Camera worldMapCamera;
    [Tooltip("WorldMapPanel/MapFrame/MapMask/WorldMapRT(RawImage)의 RectTransform")]
    [SerializeField] private RectTransform worldMapViewportRect;
    [Tooltip("WorldMapPanel/MapFrame/MapMask/WorldIconsRoot/WorldPlayerArrow(Image)의 RectTransform")]
    [SerializeField] private RectTransform worldPlayerArrow;

    [Header("World Map UI")]
    [Tooltip("WorldMapPanel")]
    [SerializeField] private GameObject worldMapPanel;

    [Header("Input")]
    [Tooltip("월드맵 토글 키 (기본 M)")]
    [SerializeField] private Key toggleWorldMapKey = Key.M;
    [Tooltip("닫기 키 (기본 ESC)")]
    [SerializeField] private Key closeWorldMapKey = Key.Escape;

    private Coroutine _findCo;

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }
        I = this;

        // 미니맵 카메라만 기본값 세팅 (월드맵 카메라는 절대 건드리지 않음)
        if (miniMapCamera != null)
        {
            miniMapCamera.orthographic = true;
            miniMapCamera.orthographicSize = miniMapOrthoSize;
            // 미니맵은 보통 Top-Down 고정이 기본
            miniMapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }

        if (worldMapPanel != null)
            worldMapPanel.SetActive(false);

        if (worldPlayerArrow != null)
            worldPlayerArrow.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        // 플레이어가 런타임 생성되는 구조 대응: 없으면 찾을 때까지 코루틴
        if (target == null)
        {
            if (_findCo != null) StopCoroutine(_findCo);
            _findCo = StartCoroutine(CoFindPlayerTarget());
        }
    }

    private IEnumerator CoFindPlayerTarget()
    {
        while (target == null)
        {
            var go = GameObject.FindGameObjectWithTag(playerTag);
            if (go != null) target = go.transform;
            yield return null;
        }
    }

    private void Update()
    {
        // 월드맵 토글 입력은 ExplorationUIHotkeys에서만 처리
        // HandleToggleWorldMap();
    }

    private void LateUpdate()
    {
        EnsureValidTarget();
        if (target == null) return;

        UpdateMiniMapCameraAndArrow();
        UpdateWorldMapPlayerMarker();
    }

    private void EnsureValidTarget()
    {
        if (target != null && target.gameObject != null && target.gameObject.activeInHierarchy)
            return;

        RefreshTargetByTag();
    }

    private void RefreshTargetByTag()
    {
        var go = GameObject.FindGameObjectWithTag(playerTag);
        if (go != null)
            SetTarget(go.transform);
    }

    // ─────────────────────────────────────────────
    // MiniMap: 카메라는 플레이어 따라감 + 회전 정책(northUp)
    // ─────────────────────────────────────────────
    private void UpdateMiniMapCameraAndArrow()
    {
        if (miniMapCamera == null) return;

        // 1) 위치: 항상 플레이어 중심
        Vector3 p = target.position;
        miniMapCamera.transform.position = new Vector3(p.x, miniMapFixedHeight, p.z);

        // 2) 사이즈: 필요하면 인스펙터 값 강제(원치 않으면 이 줄 삭제)
        miniMapCamera.orthographicSize = miniMapOrthoSize;

        // 3) 회전 정책
        float yaw = target.eulerAngles.y;

        if (northUp)
        {
            // 북쪽 고정: 카메라 고정
            miniMapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            // 플레이어 화살표가 회전(스타레일 느낌)
            if (playerArrow != null)
                playerArrow.localEulerAngles = new Vector3(0f, 0f, -yaw);
        }
        else
        {
            // 플레이어 기준: 카메라 회전
            miniMapCamera.transform.rotation = Quaternion.Euler(90f, yaw, 0f);

            // 화살표는 고정
            if (playerArrow != null)
                playerArrow.localEulerAngles = Vector3.zero;
        }
    }

    // ─────────────────────────────────────────────
    // WorldMap: 카메라는 고정(인스펙터 값 유지), 마커만 이동/회전
    // ─────────────────────────────────────────────
    private void UpdateWorldMapPlayerMarker()
    {
        // 월드맵이 꺼져있으면 마커도 숨김
        if (worldMapPanel == null || !worldMapPanel.activeSelf)
        {
            if (worldPlayerArrow != null) worldPlayerArrow.gameObject.SetActive(false);
            return;
        }

        // 필수 레퍼런스 체크
        if (worldMapCamera == null || worldMapViewportRect == null || worldPlayerArrow == null)
            return;

        // 월드 → 월드맵카메라 Viewport(0~1)
        Vector3 vp = worldMapCamera.WorldToViewportPoint(target.position);

        // 카메라 뒤 / 맵 범위 밖이면 숨김
        if (vp.z <= 0f || vp.x < 0f || vp.x > 1f || vp.y < 0f || vp.y > 1f)
        {
            worldPlayerArrow.gameObject.SetActive(false);
            return;
        }

        if (!worldPlayerArrow.gameObject.activeSelf)
            worldPlayerArrow.gameObject.SetActive(true);

        // Viewport → RawImage(Rect) anchoredPosition
        Vector2 size = worldMapViewportRect.rect.size;
        Vector2 localPos = new Vector2(
            (vp.x - 0.5f) * size.x,
            (vp.y - 0.5f) * size.y
        );

        worldPlayerArrow.anchoredPosition = localPos;

        worldPlayerArrow.localEulerAngles = Vector3.zero;
    }

    // ─────────────────────────────────────────────
    // Input: M 토글 + ESC 닫기
    // ─────────────────────────────────────────────
    private void HandleToggleWorldMap()
    {
        if (Keyboard.current == null || worldMapPanel == null) return;

        // ESC: 열려있으면 닫기
        if (worldMapPanel.activeSelf && IsKeyPressedThisFrame(closeWorldMapKey))
        {
            CloseWorldMap();
            return;
        }

        // M: 토글
        if (IsKeyPressedThisFrame(toggleWorldMapKey))
        {
            if (worldMapPanel.activeSelf) CloseWorldMap();
            else OpenWorldMap();
        }
    }

    private bool IsKeyPressedThisFrame(Key key)
    {
        var k = Keyboard.current;
        if (k == null) return false;

        // Key enum을 직접 접근할 수 없어서 switch로 매핑
        // (필요한 키만 지원: M / ESC. 다른 키 쓰고 싶으면 case 추가)
        switch (key)
        {
            case Key.M: return k.mKey.wasPressedThisFrame;
            case Key.Escape: return k.escapeKey.wasPressedThisFrame;
            default: return false;
        }
    }

    private void OpenWorldMap()
    {
        if (worldMapPanel == null || worldMapPanel.activeSelf) return;

        worldMapPanel.SetActive(true);
        AudioManager.I?.PlaySFX2D(SFXKey.UI_Open);

        // 열릴 때 즉시 마커 갱신
        if (worldPlayerArrow != null) worldPlayerArrow.gameObject.SetActive(true);
    }

    private void CloseWorldMap()
    {
        if (worldMapPanel == null || !worldMapPanel.activeSelf) return;

        worldMapPanel.SetActive(false);
        AudioManager.I?.PlaySFX2D(SFXKey.UI_Close);

        if (worldPlayerArrow != null) worldPlayerArrow.gameObject.SetActive(false);
    }

    // UI Button용
    public void OnClickCloseWorldMap()
    {
        CloseWorldMap();
    }

    // ─────────────────────────────────────────────
    // External API
    // ─────────────────────────────────────────────
    public void SetTarget(Transform t)
    {
        target = t;
        if (_findCo != null)
        {
            StopCoroutine(_findCo);
            _findCo = null;
        }
    }

    public void SetNorthUp(bool value)
    {
        northUp = value;
    }

    public void ToggleWorldMap()
    {
        if (worldMapPanel == null) return;

        if (worldMapPanel.activeSelf)
            CloseWorldMap();
        else
            OpenWorldMap();
    }

    public void OpenWorldMapPublic()
    {
        OpenWorldMap();
    }

    public void CloseWorldMapPublic()
    {
        CloseWorldMap();
    }

    public bool IsWorldMapOpen => worldMapPanel != null && worldMapPanel.activeSelf;
}