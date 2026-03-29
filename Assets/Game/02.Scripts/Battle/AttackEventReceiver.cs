using System;
using UnityEngine;

public class AttackEventReceiver : MonoBehaviour
{
    // BattleController가 구독할 콜백
    public Action OnHitFrame;

    // Animation Event에서 이 함수를 호출할 것
    // (애니메이션 이벤트는 public void 메서드만 호출 가능)
    public void AnimEvent_Hit()
    {
        OnHitFrame?.Invoke();
    }

    // 기존 클립 이벤트 호환용
    public void OnEnemyAttackHitEvent()
    {
        OnHitFrame?.Invoke();
    }
}
