using UnityEngine;

/// <summary>
/// 몬스터 기본공격(Attack02 등) "타격 프레임"에서 호출되는 릴레이 (토큰 기반)
/// Animation Event → OnEnemyAttackHitEvent() → BattleController.NotifyEnemyAttackHit(token)
/// </summary>
public class EnemyAttackEventRelay : MonoBehaviour
{
    int _token = -1;
    bool _tokenValid = false;

    /// <summary>
    /// BattleController가 공격 직전에 이번 타격의 토큰을 주입한다.
    /// </summary>
    public void SetToken(int token)
    {
        _token = token;
        _tokenValid = true;
    }

    /// <summary>
    /// 안전상태 초기화(선택)
    /// </summary>
    public void ClearToken()
    {
        _tokenValid = false;
        _token = -1;
    }

    /// <summary>
    /// 애니메이션 이벤트용 함수 (Attack02 클립의 타격 프레임에 박을 것)
    /// - public void
    /// - 파라미터 없음
    /// - 이름 정확히 일치
    /// </summary>
    public void OnEnemyAttackHitEvent()
    {
        // 너가 남긴 로그(편집본) 유지
        Debug.Log("[EnemyAttackEventRelay] HitEvent fired", this);

        var bc = BattleController.Instance;
        if (bc == null) return;

        // 토큰이 없으면(주입 실패) 무시: 이 경우는 진짜 세팅 문제
        if (!_tokenValid) return;

        bc.NotifyEnemyAttackHit(_token);
    }
}