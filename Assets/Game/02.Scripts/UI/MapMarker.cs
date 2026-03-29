using UnityEngine;

public enum MapMarkerType
{
    GatherOre = 0,
    MonsterNormal = 10,
    MonsterElite = 20,
    MonsterBoss = 30,
}

public class MapMarker : MonoBehaviour
{
    [Header("Type")]
    public MapMarkerType type = MapMarkerType.MonsterNormal;

    [Header("Icon Anchor (optional)")]
    public Transform iconAnchor;

    [Header("Flags")]
    public bool forceHidden = false;

    public Vector3 WorldPosition => (iconAnchor != null) ? iconAnchor.position : transform.position;

    private void OnEnable()
    {
        // MapIconSystem이 먼저 Awake/Start된 경우 즉시 등록됨
        if (MapIconSystem.Instance != null)
            MapIconSystem.Instance.Register(this);
    }

    private void OnDisable()
    {
        if (MapIconSystem.Instance != null)
            MapIconSystem.Instance.Unregister(this);
    }

    private void OnDestroy()
    {
        if (MapIconSystem.Instance != null)
            MapIconSystem.Instance.Unregister(this);
    }
}