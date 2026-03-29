// Assets/Game/02.Scripts/Exploration/ExplorationEnemyRespawn.cs
using System.Collections;
using UnityEngine;

public class ExplorationEnemyRespawn : MonoBehaviour
{
    [Header("Refs")]
    public BattleStarter battleStarter;
    public GameObject visualRoot;   // 모델/렌더러 부모
    public GameObject colliderRoot; // 콜라이더/트리거 부모 (없으면 자기 자신)

    Coroutine co;

    void Awake()
    {
        if (!battleStarter) battleStarter = GetComponent<BattleStarter>();

        // colliderRoot 기본은 "자기 자신"이 아니라 "null 유지" 또는 별도 오브젝트로만 받게
        // if (!colliderRoot) colliderRoot = gameObject;  // 제거
        if (!visualRoot) visualRoot = gameObject;
    }

    void OnEnable()
    {
        StartCoroutine(CoRefreshNextFrame());
    }

    IEnumerator CoRefreshNextFrame()
    {
        yield return null;

        // 1프레임 기다리는 동안 비활성/비활성화 되었으면 중단
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy) yield break;

        RefreshState();
    }

    public void RefreshState()
    {
        // 이미 비활성 상태면 아무 것도 하지 않음 (스팸 방지)
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy) return;

        if (co != null) { StopCoroutine(co); co = null; }

        var g = GameContext.I;
        if (g == null || battleStarter == null) return;

        // 1) 보스(유니크)면 영구 제거
        if (!string.IsNullOrEmpty(battleStarter.spawnId) && g.IsUniqueDefeated(battleStarter.spawnId))
        {
            SetAlive(false);
            return;
        }

        // 2) 아니면 기존 쿨다운
        if (g.IsSpawnOnCooldown(battleStarter.spawnId))
        {
            float remain = g.GetSpawnRemaining(battleStarter.spawnId);
            SetAlive(false);

            // 여기서도 가드 (inactive에서 StartCoroutine 방지)
            if (remain > 0f && isActiveAndEnabled && gameObject.activeInHierarchy)
                co = StartCoroutine(RespawnAfter(remain));
        }
        else
        {
            SetAlive(true);
        }
    }

    IEnumerator RespawnAfter(float seconds)
    {
        if (seconds > 0f) yield return new WaitForSeconds(seconds);
        SetAlive(true);
        co = null;
    }

    void SetAlive(bool alive)
    {
        if (visualRoot) visualRoot.SetActive(alive);

        if (colliderRoot)
        {
            bool colliderIsSelf = (colliderRoot == gameObject);
            bool colliderIsParent = transform.IsChildOf(colliderRoot.transform); // colliderRoot가 내 조상인가?

            if (!colliderIsSelf && !colliderIsParent)
                colliderRoot.SetActive(alive);
        }

        if (battleStarter)
            battleStarter.enabled = alive;
    }
}
