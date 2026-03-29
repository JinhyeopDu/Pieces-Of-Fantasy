using UnityEngine;

public class TutorialTargetMarker : MonoBehaviour
{
    [Header("Tutorial Target")]
    public TutorialTargetType targetType = TutorialTargetType.None;

    [Header("Optional Highlight Anchor")]
    [Tooltip("강조 이펙트를 띄울 기준 위치. 비워두면 자기 자신의 transform 사용")]
    public Transform highlightPoint;

    public Transform GetHighlightPoint()
    {
        return highlightPoint != null ? highlightPoint : transform;
    }

    public bool IsValidRuntimeTarget()
    {
        if (!gameObject.activeInHierarchy)
            return false;

        Transform point = GetHighlightPoint();
        if (point == null)
            return false;

        if (!point.gameObject.activeInHierarchy)
            return false;

        return true;
    }
}