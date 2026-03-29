using System.Collections.Generic;
using UnityEngine;

public class BreathDamageZone : MonoBehaviour
{
    [Header("Damage")]
    public int damage = 20;
    public LayerMask victimMask; // Player/Ally 레이어로 지정 추천

    [Header("Hit Policy")]
    public bool hitOncePerEnable = true; // 켜진 동안 1회만

    HashSet<Collider> _hit = new();

    void OnEnable()
    {
        _hit.Clear();

        // 이미 안에 들어와 있던 대상도 맞게 하려면 오버랩 체크
        // (Trigger 콜라이더 기준으로 주변 겹침 검사)
        var myCol = GetComponent<Collider>();
        if (myCol == null) return;

        // 가장 단순하게 Bounds 기반 OverlapBox로 처리
        var b = myCol.bounds;
        var hits = Physics.OverlapBox(b.center, b.extents, transform.rotation, victimMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            TryApply(hits[i]);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & victimMask) == 0) return;
        TryApply(other);
    }

    void TryApply(Collider other)
    {
        if (hitOncePerEnable && _hit.Contains(other)) return;
        _hit.Add(other);

        // 여기서 데미지 적용
        // 너 프로젝트가 "아군이 BattleActorRuntime" 기반이라서,
        // 실제로는 AllyView -> runtime 찾는 방식이 필요함.
        // 일단 구조만 잡고, 아래 Hook을 너 구조에 맞게 연결하면 됨.

        // 예시 1) 플레이어가 HP 컴포넌트가 있다면:
        // other.GetComponentInParent<HpComponent>()?.TakeDamage(damage);

        // 예시 2) BattleController에 데미지 라우팅 함수 만들기:
        // BattleController.Instance?.ApplyBreathDamageToCollider(other, damage);

        Debug.Log($"[BreathDamageZone] Hit {other.name} for {damage}");
    }

    // OverlapBox gizmo 보고 싶으면(선택)
    void OnDrawGizmosSelected()
    {
        var c = GetComponent<Collider>();
        if (c == null) return;
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(c.bounds.center - transform.position, c.bounds.size);
    }
}