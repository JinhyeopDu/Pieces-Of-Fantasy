using System.Collections.Generic;
using UnityEngine;

public class EnemyContactSensor : MonoBehaviour
{
    public BattleStarter CurrentTarget { get; private set; }

    // 여러 콜라이더가 들어오더라도 안정적으로 유지
    private readonly HashSet<BattleStarter> _touching = new();

    private void OnTriggerEnter(Collider other)
    {
        var t = other.GetComponentInParent<BattleStarter>();
        if (t == null) return;

        _touching.Add(t);
        CurrentTarget = t; // 마지막으로 닿은 타겟을 우선
    }

    private void OnTriggerExit(Collider other)
    {
        var t = other.GetComponentInParent<BattleStarter>();
        if (t == null) return;

        _touching.Remove(t);

        if (CurrentTarget == t)
        {
            // 남아 있는 타겟 중 아무거나 유지
            CurrentTarget = null;
            foreach (var remain in _touching)
            {
                CurrentTarget = remain;
                break;
            }
        }
    }
}
