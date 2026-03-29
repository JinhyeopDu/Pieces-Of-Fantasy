using System.Collections.Generic;
using UnityEngine;

public class InteractSensor : MonoBehaviour
{
    [Header("Filter")]
    public LayerMask detectableMask = ~0;
    public bool logNonInteractable = false;

    // 후보 목록
    readonly List<IInteractable> _candidates = new();
    readonly List<Collider> _candidateColliders = new(); // 거리 계산용(선택/정렬)

    private Transform ownerRoot;

    public IReadOnlyList<IInteractable> Candidates => _candidates;
    public int CandidateCount => _candidates.Count;

    private void Awake()
    {
        ownerRoot = transform.root;
    }

    void LateUpdate()
    {
        PruneInvalidCandidates();
    }

    private void OnTriggerEnter(Collider other)
    {
        if ((detectableMask.value & (1 << other.gameObject.layer)) == 0) return;
        if (other.transform.root == ownerRoot) return;

        var interactable = other.GetComponentInParent<IInteractable>();
        if (interactable == null)
        {
            if (logNonInteractable)
                Debug.Log($"[InteractSensor] entered non-interactable: {other.name}", this);
            return;
        }

        // 중복 방지
        if (_candidates.Contains(interactable)) return;

        _candidates.Add(interactable);
        _candidateColliders.Add(other);

        if (interactable is MonoBehaviour mb)
            Debug.Log($"[InteractSensor] + {mb.name} via {other.name}");
        else
            Debug.Log($"[InteractSensor] + {interactable} via {other.name}");
    }

    private void OnTriggerExit(Collider other)
    {
        var interactable = other.GetComponentInParent<IInteractable>();
        if (interactable == null) return;

        int idx = _candidates.IndexOf(interactable);
        if (idx >= 0)
        {
            _candidates.RemoveAt(idx);
            _candidateColliders.RemoveAt(idx);
        }
    }

    // 가장 가까운 대상 반환
    public IInteractable GetClosest()
    {
        if (_candidates.Count == 0) return null;

        int best = 0;
        float bestDist = float.PositiveInfinity;
        Vector3 p = transform.position;

        for (int i = 0; i < _candidateColliders.Count; i++)
        {
            var col = _candidateColliders[i];
            if (!col) continue;

            float d = (col.ClosestPoint(p) - p).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = i; }
        }
        return _candidates[best];
    }

    /// <summary>
    /// 후보 인덱스의 거리 제곱(정렬/선택 유지용). 유효하지 않으면 +Infinity.
    /// </summary>
    public float GetCandidateDistanceSqr(int index, Vector3 fromPos)
    {
        if (index < 0 || index >= _candidateColliders.Count) return float.PositiveInfinity;
        var col = _candidateColliders[index];
        if (!col) return float.PositiveInfinity;
        return (col.ClosestPoint(fromPos) - fromPos).sqrMagnitude;
    }

    public IInteractable GetCandidate(int index)
    {
        if (index < 0 || index >= _candidates.Count) return null;
        return _candidates[index];
    }

    public void PruneInvalidCandidates()
    {
        for (int i = _candidates.Count - 1; i >= 0; i--)
        {
            var t = _candidates[i];
            var col = _candidateColliders[i];

            if (t == null || col == null)
            {
                _candidates.RemoveAt(i);
                _candidateColliders.RemoveAt(i);
                continue;
            }

            if (!col.enabled || !col.gameObject.activeInHierarchy)
            {
                _candidates.RemoveAt(i);
                _candidateColliders.RemoveAt(i);
                continue;
            }

            if (t is MonoBehaviour mb && !mb.gameObject.activeInHierarchy)
            {
                _candidates.RemoveAt(i);
                _candidateColliders.RemoveAt(i);
                continue;
            }
        }
    }
}
