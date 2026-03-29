using System.Collections.Generic;
using UnityEngine;

public class GatherListUIController : MonoBehaviour
{
    [Header("Refs")]
    public InteractSensor sensor;
    public PlayerControllerHumanoid player;

    [Header("UI")]
    public RectTransform panelRoot;   // GatherListPanel
    public Transform contentRoot;     // Content
    public GatherListLine linePrefab; // PF_GatherLine

    [Header("Behavior")]
    public int maxLines = 6;

    [Header("Input")]
    public bool useMouseWheel = true;
    public bool useArrowKeys = true;

    [Header("Icons")]
    public Sprite battleIcon;  // 검 아이콘 (BattleStarter용)

    readonly List<GatherListLine> _lines = new();
    readonly List<IInteractable> _sorted = new();

    int _selectedIndex = 0;
    IInteractable _selected;

    void Update()
    {
        if (player == null || sensor == null)
        {
            if (panelRoot) panelRoot.gameObject.SetActive(false);
            GameContext.I?.SetGatherListOpen(false); // 추가
            return;
        }
        if (GameContext.I != null && GameContext.I.IsUIBlockingLook)
        {
            if (panelRoot) panelRoot.gameObject.SetActive(false);
            GameContext.I?.SetGatherListOpen(false);
            return;
        }

        if (!panelRoot || !contentRoot || !linePrefab)
        {
            GameContext.I?.SetGatherListOpen(false);
            return;
        }

        // (안전) 센서 prune
        sensor.PruneInvalidCandidates();

        // 1) 후보 정렬(거리순)
        BuildSortedCandidates();

        bool hasAny = _sorted.Count > 0;
        panelRoot.gameObject.SetActive(hasAny);

        GameContext.I?.SetGatherListOpen(hasAny);

        if (!hasAny)
        {
            _selected = null;
            _selectedIndex = 0;
            return;
        }

        // 2) 입력(휠/↑↓)로 선택 이동
        HandleSelectionInput();

        // 3) 선택 유지/클램프
        _selectedIndex = Mathf.Clamp(_selectedIndex, 0, _sorted.Count - 1);
        _selected = _sorted[_selectedIndex];

        // 4) 줄 수 맞추기
        int count = Mathf.Min(_sorted.Count, maxLines);
        EnsureLines(count);

        // 5) 각 줄 그리기
        for (int i = 0; i < count; i++)
        {
            var t = _sorted[i];
            var line = _lines[i];

            string displayName = GetDisplayName(t);
            Sprite icon = GetIcon(t);

            line.Bind(
                t,
                icon,
                $"[F] {displayName}",
                i == _selectedIndex
            );

            line.gameObject.SetActive(true);
        }

        // 남는 라인 숨김
        for (int i = count; i < _lines.Count; i++)
            _lines[i].gameObject.SetActive(false);
    }

    void BuildSortedCandidates()
    {
        _sorted.Clear();

        var candidates = sensor.Candidates;
        if (candidates == null || candidates.Count == 0) return;

        // 후보 복사
        for (int i = 0; i < candidates.Count; i++)
        {
            var c = candidates[i];
            if (c != null) _sorted.Add(c);
        }

        // 거리순 정렬(플레이어 기준)
        Vector3 p = player.transform.position;
        _sorted.Sort((a, b) =>
        {
            float da = GetDistanceSqr(a, p);
            float db = GetDistanceSqr(b, p);
            return da.CompareTo(db);
        });

        // 선택 유지(가능하면 이전 선택을 그대로 유지)
        if (_selected != null)
        {
            int idx = _sorted.IndexOf(_selected);
            if (idx >= 0) _selectedIndex = idx;
        }
    }

    float GetDistanceSqr(IInteractable t, Vector3 playerPos)
    {
        // 센서 내부 collider 기반 거리(정확)
        int idx = FindCandidateIndex(t);
        if (idx >= 0) return sensor.GetCandidateDistanceSqr(idx, playerPos);

        // fallback: 트랜스폼 거리
        if (t is MonoBehaviour mb) return (mb.transform.position - playerPos).sqrMagnitude;
        return float.PositiveInfinity;
    }

    int FindCandidateIndex(IInteractable t)
    {
        var list = sensor.Candidates;
        if (list == null) return -1;
        for (int i = 0; i < list.Count; i++)
            if (ReferenceEquals(list[i], t)) return i;
        return -1;
    }

    void HandleSelectionInput()
    {
        int delta = 0;

        if (useMouseWheel)
        {
            float wheel = Input.mouseScrollDelta.y;
            if (wheel > 0.01f) delta = -1;
            else if (wheel < -0.01f) delta = 1;
        }

        if (useArrowKeys)
        {
            if (Input.GetKeyDown(KeyCode.UpArrow)) delta = -1;
            else if (Input.GetKeyDown(KeyCode.DownArrow)) delta = 1;
        }

        if (delta != 0)
            _selectedIndex = Mathf.Clamp(_selectedIndex + delta, 0, _sorted.Count - 1);
    }

    void EnsureLines(int count)
    {
        while (_lines.Count < count)
        {
            var inst = Instantiate(linePrefab, contentRoot);
            _lines.Add(inst);
        }
    }

    // F 눌렀을 때 호출할 함수
    public void TryInteractSelected()
    {
        if (_selected == null || player == null) return;

        // BattleStarter면: 진입 직전에 복귀 위치 저장 (방법 B)
        if (_selected is BattleStarter bs)
        {
            if (GameContext.I != null)
            {
                GameContext.I.SetReturnPoint(
                    player.transform.position,
                    player.transform.rotation,
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
                );
            }

            bs.StartBattleFromField(); // 인수 없음 그대로 호출
            return;
        }

        _selected.Interact(player);
    }

    string GetDisplayName(IInteractable t)
    {
        // 1) GatherableNode 우선
        if (t is GatherableNode node)
        {
            if (!string.IsNullOrWhiteSpace(node.displayNameOverride))
                return node.displayNameOverride;

            if (node.previewItem != null)
                return node.previewItem.displayName;
        }

        // BattleStarter(적)면 전투하기로 표시
        if (t is BattleStarter)
            return "전투하기";

        if (t is MonoBehaviour mb) return mb.gameObject.name;
        return t.ToString();
    }

    Sprite GetIcon(IInteractable t)
    {
        // Enemy(BattleStarter)는 검 아이콘
        if (t is BattleStarter)
            return battleIcon;

        // GatherableNode는 previewItem 아이콘
        if (t is GatherableNode node && node.previewItem != null)
            return node.previewItem.icon;

        return null;
    }

    public void RegisterPlayer(PlayerControllerHumanoid p)
    {
        player = p;
        sensor = (p != null) ? p.GetComponentInChildren<InteractSensor>(true) : null;

        if (panelRoot != null && (player == null || sensor == null))
            panelRoot.gameObject.SetActive(false);

        if (player == null)
            Debug.Log("[GatherListUI] RegisterPlayer: player = NULL (hide UI)");
        else if (sensor == null)
            Debug.LogWarning($"[GatherListUI] RegisterPlayer: InteractSensor not found under '{player.name}'");
        else
            Debug.Log($"[GatherListUI] Registered player='{player.name}', sensor='{sensor.name}'");
    }
}
