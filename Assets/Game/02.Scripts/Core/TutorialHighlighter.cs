using System.Collections.Generic;
using UnityEngine;

public class TutorialHighlighter : MonoBehaviour
{
    public static TutorialHighlighter I { get; private set; }

    [Header("Highlight Prefab")]
    public GameObject highlightPrefab;

    private List<GameObject> _activeHighlights = new();

    void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
    }

    // 모든 강조 제거
    public void ClearHighlights()
    {
        for (int i = 0; i < _activeHighlights.Count; i++)
        {
            if (_activeHighlights[i] != null)
                Destroy(_activeHighlights[i]);
        }

        _activeHighlights.Clear();
    }

    // 특정 타입 강조
    public void HighlightTargets(TutorialTargetType type)
    {
        ClearHighlights();

        if (highlightPrefab == null)
        {
            Debug.LogWarning("[TutorialHighlighter] highlightPrefab is NULL");
            return;
        }

        var targets = FindObjectsOfType<TutorialTargetMarker>(true);

        int count = 0;

        for (int i = 0; i < targets.Length; i++)
        {
            TutorialTargetMarker marker = targets[i];

            if (marker == null)
                continue;

            if (marker.targetType != type)
                continue;

            if (!marker.IsValidRuntimeTarget())
                continue;

            Transform point = marker.GetHighlightPoint();
            if (point == null)
                continue;

            GameObject h = Instantiate(highlightPrefab, point.position, Quaternion.identity);

            // 부모로 붙이기 (따라다니게)
            h.transform.SetParent(point);
            h.transform.localPosition = Vector3.zero;

            _activeHighlights.Add(h);
            count++;
        }

        Debug.Log($"[TutorialHighlighter] Highlight {type} count={count}");
    }


    public Transform GetNearestTargetPoint(TutorialTargetType type, Vector3 fromPosition)
    {
        var targets = FindObjectsOfType<TutorialTargetMarker>(true);

        Transform best = null;
        float bestSqrDist = float.MaxValue;

        for (int i = 0; i < targets.Length; i++)
        {
            TutorialTargetMarker marker = targets[i];

            if (marker == null)
                continue;

            if (marker.targetType != type)
                continue;

            if (!marker.IsValidRuntimeTarget())
                continue;

            Transform point = marker.GetHighlightPoint();
            if (point == null)
                continue;

            float sqrDist = (point.position - fromPosition).sqrMagnitude;

            if (sqrDist < bestSqrDist)
            {
                bestSqrDist = sqrDist;
                best = point;
            }
        }

        if (best != null)
            Debug.Log($"[TutorialHighlighter] Nearest target for {type} = {best.name}");
        else
            Debug.Log($"[TutorialHighlighter] No target found for {type}");

        return best;
    }
}