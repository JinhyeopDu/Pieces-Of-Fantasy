using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GatherableNode : MonoBehaviour, IInteractable
{
    [Header("Drop")]
    public DropTable dropTable;

    [Header("Preview (Gather List UX)")]
    [Tooltip("근처 리스트에 표시할 대표 아이템(아이콘/이름). DropTable 정책을 UI가 몰라도 되게 하기 위함.")]
    public ItemData previewItem;
    [Tooltip("옵션: 아이템 표시명 대신 강제로 표시할 이름. 비워두면 previewItem.displayName(또는 오브젝트명) 사용.")]
    public string displayNameOverride;

    [Header("Optional Respawn")]
    public bool respawn = false;
    public float respawnDelay = 10f;

    [Header("Debug/Seed")]
    public bool deterministicPerNode = false;
    public int seedSalt = 12345;

    [Header("Disable Targets")]
    public GameObject visualRoot;
    public Collider triggerCollider;

    bool _isAvailable = true;

    void Awake()
    {
        if (!visualRoot) visualRoot = gameObject;
        if (!triggerCollider) triggerCollider = GetComponent<Collider>();
    }

    public void Interact(PlayerControllerHumanoid player)
    {
        if (!_isAvailable) return;

        if (dropTable == null)
        {
            Debug.LogWarning($"[GatherableNode] DropTable is NULL on {name}");
            return;
        }

        System.Random rng;
        if (deterministicPerNode)
        {
            int seed = seedSalt ^ transform.position.GetHashCode() ^ name.GetHashCode();
            rng = new System.Random(seed);
        }
        else
        {
            rng = new System.Random(Environment.TickCount);
        }

        List<(ItemData item, int qty)> drops = dropTable.Roll(rng);

        bool any = false;

        for (int i = 0; i < drops.Count; i++)
        {
            var (item, qty) = drops[i];
            if (item == null || qty <= 0) continue;

            // Debug.Log($"[GatherableNode] drop item={item.id}, name={item.displayName}, qty={qty}, gatherType={item.gatherMaterialType}");

            GameContext.I.AddItem(item, qty);
            GameContext.I.QueueReward(item, qty);

            // 광석 채집 퀘스트 카운트 + 튜토리얼 조건
            if (IsOreItem(item))
            {
                // Debug.Log($"[GatherableNode] NotifyGatherOre({qty})");
                QuestManager.I?.NotifyGatherOre(qty);

                // 튜토리얼 패널 진행도 즉시 갱신
                TutorialManager.I?.CheckAndShowNextTutorial();
            }

            any = true;
        }

        if (!any)
        {
            // Debug.Log($"[GatherableNode] No drops from table '{dropTable.name}'");
        }


        _isAvailable = false;
        SetEnabledState(false);

        if (respawn)
        {
            StopAllCoroutines();
            StartCoroutine(CoRespawn());
        }
        else
        {
            if (triggerCollider) triggerCollider.enabled = false;
        }
    }

    void SetEnabledState(bool enabled)
    {
        if (visualRoot && visualRoot != gameObject)
            visualRoot.SetActive(enabled);

        if (triggerCollider)
            triggerCollider.enabled = enabled;
    }

    IEnumerator CoRespawn()
    {
        yield return new WaitForSeconds(respawnDelay);
        _isAvailable = true;
        SetEnabledState(true);
    }

    private bool IsOreItem(ItemData item)
    {
        return item != null && item.gatherMaterialType == GatherMaterialType.Ore;
    }
}