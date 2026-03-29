// Assets/Game/02.Scripts/UI/MapIconSystem.cs
using System.Collections.Generic;
using UnityEngine;

public class MapIconSystem : MonoBehaviour
{
    public static MapIconSystem Instance { get; private set; }

    [Header("Cameras")]
    public Camera miniMapCamera;
    public Camera worldMapCamera;

    [Header("UI Roots (RectTransform)")]
    public RectTransform miniMapIconsRoot;
    public RectTransform worldMapIconsRoot;

    [Header("Icon Prefab (UI)")]
    public MapIconView iconPrefab;

    [Header("Sprites")]
    public Sprite gatherSprite;
    public Sprite normalMonsterSprite;
    public Sprite eliteMonsterSprite;
    public Sprite bossMonsterSprite;

    [Header("Colors")]
    public Color gatherColor = Color.white;                        // 광석
    public Color normalMonsterColor = new Color(1f, 0.55f, 0.1f);  // 주황
    public Color eliteMonsterColor = new Color(0.75f, 0.35f, 1f);  // 보라
    public Color bossMonsterColor = new Color(1f, 0.15f, 0.15f);   // 빨강

    [Header("MiniMap Icon Sizes")]
    public Vector2 gatherSize = new Vector2(40, 40);
    public Vector2 normalSize = new Vector2(30, 30);
    public Vector2 eliteSize = new Vector2(60, 60);
    public Vector2 bossSize = new Vector2(65, 65);

    [Header("Policy")]
    [Tooltip("미니맵에서는 카메라 안에 들어온 것만 보이게")]
    public bool minimapOnlyVisibleInCamera = true;

    [Tooltip("씬에서 마커를 주기적으로 재스캔해서(늦게 스폰된 오브젝트 포함) 자동 등록")]
    public bool autoScanMarkers = true;

    [Tooltip("재스캔 주기(초). 0.25~1 추천")]
    public float autoScanInterval = 0.5f;

    // marker -> (miniIcon, worldIcon)
    private readonly Dictionary<MapMarker, (MapIconView mini, MapIconView world)> _icons = new();
    private readonly List<MapMarker> _toRemove = new();

    float _scanTimer;

    private void Awake()
    {
        Debug.Log("[MapIconSystem] Awake");
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        Debug.Log("[MapIconSystem] Start");
        Debug.Log($"miniRoot={miniMapIconsRoot} worldRoot={worldMapIconsRoot} iconPrefab={iconPrefab}");

        // 핵심: 시작 시점에 한번 전부 등록
        AutoRegisterAllMarkers();

        // (검증)
        Debug.Log($"[MapIconSystem] Registered icons = {_icons.Count}");
    }

    private void LateUpdate()
    {
        // 늦게 스폰되는 마커(몬스터/광석 등)까지 자동 등록
        if (autoScanMarkers)
        {
            _scanTimer += Time.deltaTime;
            if (_scanTimer >= Mathf.Max(0.05f, autoScanInterval))
            {
                _scanTimer = 0f;
                AutoRegisterAllMarkers();
            }
        }

        PruneDeadMarkers();
        UpdateAllIcons();
    }

    private void AutoRegisterAllMarkers()
    {
        var markers = FindObjectsOfType<MapMarker>(true);
        // Debug.Log($"[MapIconSystem] Found markers = {markers.Length}");
        for (int i = 0; i < markers.Length; i++)
            Register(markers[i]);
    }

    public void Register(MapMarker marker)
    {
        if (marker == null) return;
        if (_icons.ContainsKey(marker)) return;

        var mini = CreateIcon(miniMapIconsRoot);
        var world = CreateIcon(worldMapIconsRoot);

        ApplyStyle(marker, mini);
        ApplyStyle(marker, world);

        _icons.Add(marker, (mini, world));

        // Debug.Log($"[MapIconSystem] Register {marker.name} type={marker.type}");
    }

    public void Unregister(MapMarker marker)
    {
        if (marker == null) return;
        if (!_icons.TryGetValue(marker, out var pair)) return;

        if (pair.mini != null) Destroy(pair.mini.gameObject);
        if (pair.world != null) Destroy(pair.world.gameObject);

        _icons.Remove(marker);
    }

    private MapIconView CreateIcon(RectTransform root)
    {
        if (root == null || iconPrefab == null) return null;
        var v = Instantiate(iconPrefab, root);
        v.EnsureRefs();
        return v;
    }

    private void ApplyStyle(MapMarker m, MapIconView v)
    {
        if (m == null || v == null) return;
        var (sprite, color, size) = GetStyle(m.type);
        v.SetStyle(sprite, color, size);
    }

    private (Sprite sprite, Color color, Vector2 size) GetStyle(MapMarkerType type)
    {
        switch (type)
        {
            case MapMarkerType.GatherOre:
                return (gatherSprite, gatherColor, gatherSize);

            case MapMarkerType.MonsterElite:
                return (eliteMonsterSprite, eliteMonsterColor, eliteSize);

            case MapMarkerType.MonsterBoss:
                return (bossMonsterSprite, bossMonsterColor, bossSize);

            case MapMarkerType.MonsterNormal:
            default:
                return (normalMonsterSprite, normalMonsterColor, normalSize);
        }
    }

    private void PruneDeadMarkers()
    {
        _toRemove.Clear();
        foreach (var kv in _icons)
        {
            var m = kv.Key;
            if (m == null || !m.gameObject.activeInHierarchy || m.forceHidden)
                _toRemove.Add(m);
        }

        for (int i = 0; i < _toRemove.Count; i++)
            Unregister(_toRemove[i]);
    }

    private void UpdateAllIcons()
    {
        if (miniMapCamera == null && worldMapCamera == null) return;

        foreach (var kv in _icons)
        {
            var marker = kv.Key;
            if (marker == null) continue;

            var (mini, world) = kv.Value;
            Vector3 wp = marker.WorldPosition;

            // MiniMap
            if (miniMapCamera != null && mini != null)
            {
                Vector3 vp = miniMapCamera.WorldToViewportPoint(wp);
                bool inside = (vp.z > 0f && vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f);

                if (minimapOnlyVisibleInCamera)
                {
                    mini.gameObject.SetActive(inside);
                    if (inside) mini.SetViewportPosition(vp);
                }
                else
                {
                    mini.gameObject.SetActive(vp.z > 0f);
                    if (vp.z > 0f) mini.SetViewportPosition(vp);
                }
            }

            // WorldMap
            if (worldMapCamera != null && world != null)
            {
                Vector3 vp = worldMapCamera.WorldToViewportPoint(wp);
                bool visible = (vp.z > 0f);
                world.gameObject.SetActive(visible);
                if (visible) world.SetViewportPosition(vp);
            }
        }
    }
}