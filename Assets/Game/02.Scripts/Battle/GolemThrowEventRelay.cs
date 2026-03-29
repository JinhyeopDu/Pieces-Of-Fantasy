using System.Collections;
using UnityEngine;

public class GolemThrowEventRelay : MonoBehaviour
{
    [Header("Assign in Prefab")]
    [Tooltip("골렘 오른손 소켓(예: RockSocket_R)")]
    public Transform rockSocketR;

    [Tooltip("던질 돌 프리팹 (PF_Rock_Projectile)")]
    public GameObject rockProjectilePrefab;

    [Header("Throw Motion")]
    [Tooltip("돌이 날아가는 시간(초)")]
    public float flyTime = 0.35f;

    [Tooltip("포물선 높이")]
    public float arcHeight = 1.2f;

    [Tooltip("아군 중앙을 겨냥할 때 y로 살짝 올려서 던지고 싶으면 사용")]
    public float targetHeightOffset = 0.2f;

    [Header("Scale Policy")]
    [Tooltip("체크하면 아래 forcedWorldScale로 월드 스케일을 강제로 맞춤(디버깅/보정용)")]
    public bool useForcedWorldScale = false;

    [Tooltip("강제로 유지할 월드 스케일(손 뼈대 0.01 문제 등에서 확실히 보이게 하고 싶을 때)")]
    public Vector3 forcedWorldScale = Vector3.one;

    [Header("Debug")]
    public bool enableDebugLog = true;

    GameObject _heldRock;
    // [추가] Throw Context (BattleController가 토큰/목표점 주입)
    BattleController _ctxBC;
    int _ctxToken = -1;
    Vector3 _ctxEnd;
    bool _hasCtx;

    // ------------------------------------------------------------
    // Animation Events (Attack01에 박아야 함)
    // 1) 손에 돌을 쥠: OnThrowGrabRock()
    // 2) 손에서 놓음: OnThrowReleaseRock()
    // 데미지는 "도착(Impact)"에서 Notify로 발생
    // ------------------------------------------------------------

    // Attack01에서 "손에 쥐는" 프레임에 호출
    public void OnThrowGrabRock()
    {
        if (enableDebugLog) Debug.Log("[GolemThrow] GrabRock event fired");

        if (rockSocketR == null || rockProjectilePrefab == null)
        {
            if (enableDebugLog) Debug.LogWarning("[GolemThrow] Missing rockSocketR or rockProjectilePrefab");
            return;
        }

        if (_heldRock != null) return;

        // 1) 월드에 생성
        var go = Instantiate(rockProjectilePrefab);
        go.name = rockProjectilePrefab.name + "(Held)";
        go.transform.SetPositionAndRotation(rockSocketR.position, rockSocketR.rotation);

        // 2) 유지할 월드 스케일 결정
        //    - 기본은 프리팹 localScale을 '월드에서 보이는 크기'로 간주
        //    - 필요하면 forcedWorldScale로 강제 가능
        Vector3 desiredWorldScale = useForcedWorldScale ? forcedWorldScale : rockProjectilePrefab.transform.localScale;

        // 3) 소켓에 붙이되 월드 포즈 유지
        go.transform.SetParent(rockSocketR, worldPositionStays: true);

        // 4) 스케일 보정 (손 소켓의 0.01 스케일 상속 문제 해결)
        SetWorldScale(go.transform, desiredWorldScale);

        // 5) 손에 든 동안 물리/충돌 비활성(권장)
        var rb = go.GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = true; // kinematic에 velocity 세팅하면 경고 -> 하지 않음

        var col = go.GetComponent<Collider>();
        if (col) col.enabled = false;

        _heldRock = go;
    }

    // Attack01에서 "던지는" 프레임에 호출 (여기서는 발사 시작만)
    public void OnThrowReleaseRock()
    {
        Debug.Log("[GolemThrow] ReleaseRock event fired (CONFIRM)");

        if (enableDebugLog) Debug.Log("[GolemThrow] ReleaseRock event fired");

        if (_heldRock == null)
        {
            if (enableDebugLog) Debug.LogWarning("[GolemThrow] No held rock to release");
            return;
        }

        // bc 결정: 주입된 컨텍스트 우선, 없으면 Instance fallback
        var bc = _hasCtx ? _ctxBC : BattleController.Instance;
        if (bc == null)
        {
            if (enableDebugLog) Debug.LogWarning("[GolemThrow] BattleController is NULL");
            Destroy(_heldRock);
            _heldRock = null;
            _hasCtx = false;
            return;
        }

        // 로컬로 빼서 안전 처리
        GameObject rock = _heldRock;
        _heldRock = null;

        Vector3 start = rock.transform.position;

        // 목표점(end): 주입된 컨텍스트 우선, 없으면 계산
        Vector3 end = _hasCtx ? _ctxEnd : bc.GetAlliesCenterPosition();
        end.y += targetHeightOffset;

        // 토큰: 주입된 컨텍스트 우선, 없으면 CurrentEnemyHitToken
        int token = _hasCtx ? _ctxToken : bc.CurrentEnemyHitToken;

        // Release 기준 타이밍을 BattleController에 알려준다
        bc.NotifyThrowReleased(token, flyTime);

        // 컨텍스트는 1회성
        _hasCtx = false;

        if (enableDebugLog)
            Debug.Log($"[GolemThrow] Release using token={token}, end={end}");

        // 손에서 분리
        rock.transform.SetParent(null, worldPositionStays: true);

        // 물리/충돌 OFF
        var rb = rock.GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = true;

        var col = rock.GetComponent<Collider>();
        if (col) col.enabled = false;

        // 투척 시작 (데미지는 도착 Impact에서 Notify)
        StartCoroutine(ParabolaFlyAndImpact(bc, token, rock, start, end, flyTime, arcHeight));
    }

    IEnumerator ParabolaFlyAndImpact(
     BattleController bc,
     int token,
     GameObject rock,
     Vector3 start,
     Vector3 end,
     float time,
     float height)
    {
        if (rock == null) yield break;

        float t = 0f;
        float inv = 1f / Mathf.Max(0.0001f, time);

        while (t < time)
        {
            // 권장: timeScale(미니 슬로우 등)에 영향 받지 않게 unscaled 사용
            t += Time.unscaledDeltaTime;

            float u = Mathf.Clamp01(t * inv);

            Vector3 p = Vector3.Lerp(start, end, u);
            p.y += Mathf.Sin(u * Mathf.PI) * height;

            if (rock) rock.transform.position = p;
            yield return null;
        }

        if (rock) rock.transform.position = end;

        // "맞는 순간" 데미지 트리거
        if (bc != null)
        {
            Debug.Log($"[GolemThrow] Impact now. notify token={token}, time={time}");
            bc.NotifyEnemyAttackHit(token);
        }

        if (rock) Destroy(rock, 0.1f);
    }

    // ------------------------------------------------------------
    // Utils
    // ------------------------------------------------------------
    static void SetWorldScale(Transform t, Vector3 worldScale)
    {
        if (t == null) return;

        Transform p = t.parent;
        if (p == null)
        {
            t.localScale = worldScale;
            return;
        }

        Vector3 ps = p.lossyScale;

        float sx = Mathf.Abs(ps.x) < 1e-6f ? 1f : ps.x;
        float sy = Mathf.Abs(ps.y) < 1e-6f ? 1f : ps.y;
        float sz = Mathf.Abs(ps.z) < 1e-6f ? 1f : ps.z;

        t.localScale = new Vector3(worldScale.x / sx, worldScale.y / sy, worldScale.z / sz);
    }

    public void SetThrowContext(BattleController bc, int token, Vector3 end)
    {
        _ctxBC = bc;
        _ctxToken = token;
        _ctxEnd = end;
        _hasCtx = true;

        if (enableDebugLog)
            Debug.Log($"[GolemThrow] Context set. token={token}, end={end}");
    }
}
