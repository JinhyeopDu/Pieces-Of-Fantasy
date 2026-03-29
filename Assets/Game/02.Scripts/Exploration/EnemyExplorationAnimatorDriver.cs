using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyExplorationAnimatorDriver : MonoBehaviour
{
    [Header("Refs")]
    public NavMeshAgent agent;
    [Tooltip("목적지 갱신/배회 스크립트(EnemyWanderAI). 반드시 이걸 넣으세요.")]
    public MonoBehaviour wanderAI;
    public Animator animator;

    [Header("Animator Params")]
    public string triggerHit = "Hit";

    [Header("Hit Policy")]
    public float hitLockDuration = 0.6f;

    [Header("Safety")]
    public bool ignoreMissingAnimatorParams = true;

    bool _locked;
    Coroutine _hitCo;

    void Reset()
    {
        agent = GetComponentInChildren<NavMeshAgent>(true);
        animator = GetComponentInChildren<Animator>(true);
        // wanderAI는 자동으로 못 잡는 경우가 많아서 Inspector 연결 권장
    }

    void Awake()
    {
        if (!agent) agent = GetComponentInChildren<NavMeshAgent>(true);
        if (!animator) animator = GetComponentInChildren<Animator>(true);
    }

    void OnDisable()
    {
        if (_hitCo != null)
        {
            StopCoroutine(_hitCo);
            _hitCo = null;
        }
        _locked = false;
    }

    public void PlayHit()
    {
        Debug.Log($"[Hit] PlayHit called on {name}", this);
        if (!isActiveAndEnabled) return;
        if (!animator) return;

        if (_hitCo != null) StopCoroutine(_hitCo);
        _hitCo = StartCoroutine(HitRoutine());
    }

    IEnumerator HitRoutine()
    {
        LockMovement(true);

        if (HasTriggerParam(animator, triggerHit))
        {
            animator.ResetTrigger(triggerHit);
            animator.SetTrigger(triggerHit);
        }
        else if (!ignoreMissingAnimatorParams)
        {
            Debug.LogWarning($"[EnemyExplorationAnimatorDriver] Trigger '{triggerHit}' not found on {animator.name} ({name}).");
        }

        if (hitLockDuration > 0f)
            yield return new WaitForSeconds(hitLockDuration);

        LockMovement(false);
        _hitCo = null;
    }

    void LockMovement(bool on)
    {
        _locked = on;

        // ★ WanderAI는 먼저 끈다(충돌 방지)
        if (wanderAI) wanderAI.enabled = !on;

        if (!agent) return;

        // NavMesh 위가 아닐 때는 Stop/ResetPath 금지(에러 방지)
        if (!agent.enabled || !agent.isOnNavMesh)
            return;

        if (on)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.velocity = Vector3.zero;
        }
        else
        {
            agent.isStopped = false;
        }
    }

    static bool HasTriggerParam(Animator a, string name)
    {
        if (!a || string.IsNullOrEmpty(name)) return false;
        var ps = a.parameters;
        for (int i = 0; i < ps.Length; i++)
            if (ps[i].type == AnimatorControllerParameterType.Trigger && ps[i].name == name)
                return true;
        return false;
    }
}
