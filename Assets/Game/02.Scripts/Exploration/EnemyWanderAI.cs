using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class EnemyWanderAI : MonoBehaviour
{
    [Header("Wander Area")]
    public float wanderRadius = 5f;
    public float repathInterval = 0.3f;

    [Header("Move")]
    public float walkSpeed = 2.0f;
    public float arrivalDistance = 0.3f;

    [Header("Idle")]
    public float idleMin = 0.5f;
    public float idleMax = 1.5f;

    [Header("Battle Disable")]
    public bool disableOnBattleScene = true;

    [Header("Animator (Optional)")]
    [Tooltip("이동 속도(float) 파라미터 이름. (예: MoveSpeed)")]
    public string animSpeedParam = "MoveSpeed";

    NavMeshAgent _agent;
    float _nextRepathTime;
    float _idleTimer;
    Vector3 _origin;
    Vector3 _target;
    bool _hasTarget;

    Animator _anim;
    int _hashSpeed;

    // 안정성: 파라미터 존재 여부 캐시
    bool _hasSpeedParam;

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _anim = GetComponentInChildren<Animator>(true);
        _origin = transform.position;

        _hashSpeed = Animator.StringToHash(animSpeedParam);
        _hasSpeedParam = HasFloatParam(_anim, animSpeedParam);
    }

    void OnEnable()
    {
        // 1) Battle 씬이면: WanderAI + NavMeshAgent 둘 다 꺼서 "no valid NavMesh"를 원천 차단
        if (disableOnBattleScene && SceneManager.GetActiveScene().name == "Battle")
        {
            if (_agent != null) _agent.enabled = false;
            enabled = false;
            return;
        }

        if (_agent == null) return;

        // NavMeshAgent가 켜진 상태에서만 NavMesh 기능 호출 가능
        if (!_agent.enabled) _agent.enabled = true;

        // 2) NavMesh 위로 위치 보정(스폰이 NavMesh 밖이면 떠보이거나 튀는 원인)
        //    - 성공하면 Warp로 스냅
        if (NavMesh.SamplePosition(transform.position, out var hit, 2.0f, NavMesh.AllAreas))
        {
            _agent.Warp(hit.position);
            transform.position = hit.position;
        }

        _agent.speed = walkSpeed;
        _agent.stoppingDistance = arrivalDistance;

        _agent.updateRotation = true;
        _agent.updatePosition = true;

        // 최초 타겟 설정
        PickNewTarget();
    }

    void Update()
    {
        if (_agent == null || !_agent.enabled) return;

        // 목적지 도착 → 잠깐 대기 → 새 목적지
        if (_hasTarget && !_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance + 0.05f)
        {
            _hasTarget = false;
            _idleTimer = Random.Range(idleMin, idleMax);
        }

        if (!_hasTarget)
        {
            _idleTimer -= Time.deltaTime;
            if (_idleTimer <= 0f)
                PickNewTarget();
        }
        else
        {
            // 가끔 경로가 끊기면 재설정
            if (Time.time >= _nextRepathTime)
            {
                _nextRepathTime = Time.time + repathInterval;

                // path가 없는데도 목적지가 유효하면 다시 SetDestination
                if (!_agent.hasPath)
                    _agent.SetDestination(_target);
            }
        }

        // 애니메이션: Speed(float)에 velocity magnitude 넣기
        // (중요) 파라미터가 실제로 있을 때만 SetFloat
        if (_anim != null && _hasSpeedParam)
        {
            float v = _agent.velocity.magnitude;
            _anim.SetFloat(_hashSpeed, v);
        }
    }

    void PickNewTarget()
    {
        // NavMesh 샘플링 실패 시 안전하게 대기 모드로
        if (!TryRandomNavmeshPoint(_origin, wanderRadius, out _target))
        {
            _hasTarget = false;
            _idleTimer = Random.Range(idleMin, idleMax);
            return;
        }

        // SetDestination이 실패하는 경우도 있으니 결과 체크(선택)
        bool ok = _agent.SetDestination(_target);
        _hasTarget = ok;

        if (!_hasTarget)
            _idleTimer = Random.Range(idleMin, idleMax);

        _nextRepathTime = Time.time + repathInterval;
    }

    static bool TryRandomNavmeshPoint(Vector3 center, float range, out Vector3 result)
    {
        for (int i = 0; i < 12; i++)
        {
            Vector3 randomPoint = center + Random.insideUnitSphere * range;
            if (NavMesh.SamplePosition(randomPoint, out var hit, 2.0f, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }
        }

        result = center;
        return false;
    }

    static bool HasFloatParam(Animator anim, string paramName)
    {
        if (!anim || string.IsNullOrEmpty(paramName)) return false;

        var ps = anim.parameters;
        for (int i = 0; i < ps.Length; i++)
        {
            if (ps[i].type == AnimatorControllerParameterType.Float && ps[i].name == paramName)
                return true;
        }
        return false;
    }
}
