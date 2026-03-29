using UnityEngine;
using UnityEngine.SceneManagement;

public class BattleStarter : MonoBehaviour, IInteractable
{
    [Header("Pick one (Encounter recommended)")]
    [Tooltip("권장 방식. 전투 시작 시 사용할 EncounterData")]
    [SerializeField] private EncounterData encounter;

    [Tooltip("구형 호환용 적 배열. EncounterData가 있으면 이 값은 무시된다.")]
    [SerializeField] private EnemyData[] enemyPack;

    [Header("Respawn")]
    [Tooltip("이 필드 몬스터의 고유 ID. 씬 내에서 유일해야 한다.")]
    public string spawnId;

    [Tooltip("전투 승리 후 이 오브젝트가 다시 활성화되기까지 걸리는 시간(초)")]
    public float respawnDelay = 30f;

    [Header("Battle Requirement")]
    [Tooltip("true이면 특정 아이템을 가지고 있어야 전투를 시작할 수 있다.")]
    [SerializeField] private bool requireKeyItem = false;

    [Tooltip("전투 시작에 필요한 키 아이템")]
    [SerializeField] private ItemData requiredKeyItem;

    [Tooltip("전투 시작 조건을 만족하지 못했을 때 보여줄 메시지")]
    [SerializeField, TextArea] private string failMessage = "조건이 충족되지 않았습니다.";

    [Tooltip("조건 실패 메시지를 출력할 시스템 배너 UI")]
    [SerializeField] private SystemBannerController systemBanner;

    [Header("Return Point")]
    [Tooltip("지정 시 전투 후 이 위치로 복귀한다. 비어 있으면 현재 플레이어 위치를 사용한다.")]
    [SerializeField] private Transform returnPointOverride;

    private bool _spawnEnabled = true;

    public EncounterData Encounter => encounter;
    public EnemyData[] EnemyPack => enemyPack;


    /// <summary>
    /// 시작 시 시스템 배너 참조를 보정하고,
    /// 현재 리스폰 상태를 기준으로 오브젝트 표시 여부를 적용한다.
    /// </summary>
    private void Start()
    {
        if (systemBanner == null)
            systemBanner = FindFirstObjectByType<SystemBannerController>();

        ApplyRespawnVisibility();
    }

    /// <summary>
    /// 매 프레임 현재 spawnId의 상태를 확인하여
    /// 유니크 처치 여부 / 리스폰 쿨다운 여부에 따라 활성 상태를 갱신한다.
    /// </summary>
    private void Update()
    {
        if (string.IsNullOrEmpty(spawnId)) return;
        if (GameContext.I == null) return;

        bool uniqueDefeated = GameContext.I.IsUniqueDefeated(spawnId);
        bool onCooldown = GameContext.I.IsSpawnOnCooldown(spawnId);

        bool shouldEnable = !uniqueDefeated && !onCooldown;

        if (_spawnEnabled != shouldEnable)
            SetSpawnEnabled(shouldEnable);
    }

    /// <summary>
    /// 현재 BattleStarter가 실제 전투 데이터를 가지고 있는지 검사한다.
    /// EncounterData가 유효하거나, 구형 enemyPack에 데이터가 있으면 true를 반환한다.
    /// </summary>
    public bool HasValidData
    {
        get
        {
            if (EncounterHasData(encounter)) return true;
            if (enemyPack != null && enemyPack.Length > 0) return true;
            return false;
        }
    }

    private static bool EncounterHasData(EncounterData e)
    {
        if (e == null) return false;

        if (e.guaranteedEnemy != null && e.guaranteedCount > 0)
            return true;

        if (e.optionalSlots != null)
        {
            for (int i = 0; i < e.optionalSlots.Length; i++)
            {
                var slot = e.optionalSlots[i];
                if (slot == null || slot.candidates == null) continue;

                for (int j = 0; j < slot.candidates.Length; j++)
                {
                    var c = slot.candidates[j];
                    if (c != null && c.enemy != null && c.count > 0)
                        return true;
                }
            }
        }

        return false;
    }

    private static int CountOptionalCandidates(EncounterData e)
    {
        if (e == null || e.optionalSlots == null) return 0;

        int total = 0;
        for (int i = 0; i < e.optionalSlots.Length; i++)
        {
            var slot = e.optionalSlots[i];
            if (slot == null || slot.candidates == null) continue;

            for (int j = 0; j < slot.candidates.Length; j++)
            {
                var c = slot.candidates[j];
                if (c != null && c.enemy != null) total++;
            }
        }
        return total;
    }

    public string GetDebugInfo()
    {
        int packLen = (enemyPack != null) ? enemyPack.Length : 0;

        if (encounter == null)
        {
            return $"enc=null packLen={packLen} spawnId={(string.IsNullOrEmpty(spawnId) ? "EMPTY" : spawnId)} respawnDelay={respawnDelay}";
        }

        int optionalCandidateCount = CountOptionalCandidates(encounter);

        return $"enc={encounter.name} " +
               $"guaranteed={(encounter.guaranteedEnemy ? encounter.guaranteedEnemy.name : "null")}x{Mathf.Max(0, encounter.guaranteedCount)} " +
               $"optionalCandidateTotal={optionalCandidateCount} " +
               $"packLen={packLen} " +
               $"spawnId={(string.IsNullOrEmpty(spawnId) ? "EMPTY" : spawnId)} respawnDelay={respawnDelay}";
    }

    /// <summary>
    /// 탐험 씬에서 플레이어가 이 오브젝트와 상호작용했을 때 호출된다.
    /// 실제 전투 시작 처리는 StartBattleFromField()에서 수행한다.
    /// </summary>
    public void Interact(PlayerControllerHumanoid player)
    {
        StartBattleFromField();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(spawnId))
            spawnId = System.Guid.NewGuid().ToString("N");

        if (respawnDelay < 0f) respawnDelay = 0f;

        if (encounter != null && enemyPack != null && enemyPack.Length > 0)
        {
            Debug.LogWarning($"[BattleStarter] {name}: Encounter가 설정되어 있으므로 EnemyPack은 무시됩니다.");
        }

        bool encEmpty = !EncounterHasData(encounter);
        bool packEmpty = (enemyPack == null || enemyPack.Length == 0);

        if (encEmpty && packEmpty)
        {
            Debug.LogWarning($"[BattleStarter] {name}: Encounter/EnemyPack이 비어 있습니다. 전투 데이터가 없습니다.");
        }
    }
#endif

    /// <summary>
    /// 현재 spawnId 상태를 기준으로 이 오브젝트의 표시/비표시를 적용한다.
    /// 유니크 처치 상태이거나 리스폰 쿨다운 중이면 비활성화한다.
    /// </summary>
    private void ApplyRespawnVisibility()
    {
        if (string.IsNullOrEmpty(spawnId)) return;
        if (GameContext.I == null) return;

        bool uniqueDefeated = GameContext.I.IsUniqueDefeated(spawnId);
        bool onCooldown = GameContext.I.IsSpawnOnCooldown(spawnId);

        if (uniqueDefeated)
        {
            SetSpawnEnabled(false);
            return;
        }

        SetSpawnEnabled(!onCooldown);
    }

    /// <summary>
    /// 이 오브젝트 하위의 Renderer와 Collider를 함께 켜거나 끈다.
    /// 리스폰 상태나 유니크 처치 상태를 시각적으로 반영할 때 사용한다.
    /// </summary>
    private void SetSpawnEnabled(bool enabled)
    {
        _spawnEnabled = enabled;

        var rends = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < rends.Length; i++)
            rends[i].enabled = enabled;

        var cols = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < cols.Length; i++)
            cols[i].enabled = enabled;
    }

    /// <summary>
    /// 전투 시작 조건을 만족하는지 검사한다.
    /// 현재는 특정 키 아이템 보유 여부를 확인하는 용도로 사용한다.
    /// </summary>
    private bool CanStartBattleByRequirement()
    {
        if (!requireKeyItem)
            return true;

        if (requiredKeyItem == null)
        {
            Debug.LogWarning($"[BattleStarter] {name}: requireKeyItem=true 이지만 requiredKeyItem이 비어 있음");
            return false;
        }

        return HasItemInInventory(requiredKeyItem);
    }

    /// <summary>
    /// 현재 GameContext 인벤토리에 대상 아이템이 있는지 검사한다.
    /// 같은 에셋 참조이거나, id가 같으면 같은 아이템으로 판단한다.
    /// </summary>
    private bool HasItemInInventory(ItemData target)
    {
        var g = GameContext.I;
        if (g == null)
        {
            Debug.LogWarning("[BattleStarter] GameContext.I is null.");
            return false;
        }

        if (g.inventory == null || g.inventory.items == null)
        {
            Debug.LogWarning("[BattleStarter] GameContext inventory is null.");
            return false;
        }

        for (int i = 0; i < g.inventory.items.Count; i++)
        {
            var stack = g.inventory.items[i];
            if (stack == null) continue;
            if (stack.item == null) continue;
            if (stack.count <= 0) continue;

            if (stack.item == target)
                return true;

            if (!string.IsNullOrEmpty(stack.item.id) &&
                !string.IsNullOrEmpty(target.id) &&
                stack.item.id == target.id)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 전투 시작 조건을 만족하지 못했을 때 시스템 배너로 실패 메시지를 표시한다.
    /// </summary>
    private void ShowFailBanner()
    {

        if (systemBanner != null)
        {
            string msg = string.IsNullOrWhiteSpace(failMessage)
                ? "조건이 충족되지 않았습니다."
                : failMessage;

            systemBanner.ShowMessage(msg);
        }
        else
        {
            Debug.LogWarning($"[BattleStarter] systemBanner가 연결되지 않았습니다. message={failMessage}");
        }
    }

    /// <summary>
    /// 탐험 씬에서 전투를 시작한다.
    /// 조건 아이템 검사, 복귀 위치 저장, 전투 payload 설정 후 Battle 씬으로 이동한다.
    /// </summary>
    public void StartBattleFromField()
    {
        if (!CanStartBattleByRequirement())
        {
            ShowFailBanner();
            return;
        }

        var g = GameContext.I;
        if (g == null)
        {
            Debug.LogError("[BattleStarter] StartBattleFromField failed: GameContext.I is null.");
            return;
        }

        for (int i = 0; i < g.party.Count; i++)
        {
            var cr = g.party[i];
            if (cr == null || cr.data == null) continue;
            //Debug.Log($"[StartBattleFromField] party[{i}] {cr.data.name} lv={cr.level} hp={cr.hp}/{cr.maxHp} sp={cr.sp}");
        }

        if (g.party != null && g.party.Count > 0 && g.party[0] != null)
            Debug.Log($"[StartBattleFromField] BEFORE LOAD: GC={g.GetInstanceID()} lv0={g.party[0].level} promo0={g.party[0].promotionStage} exp0={g.party[0].exp}");
        else
            Debug.Log("[StartBattleFromField] BEFORE LOAD: GC party empty");

        if (!HasValidData)
        {
            Debug.LogWarning($"[BattleStarter] StartBattleFromField 실패: 전투 데이터 없음 ({GetDebugInfo()})");
            return;
        }

        TempBattlePayload.encounter = encounter;
        TempBattlePayload.enemySet = (encounter == null) ? enemyPack : null;
        TempBattlePayload.spawnId = spawnId;

        float delay = Mathf.Max(0f, respawnDelay);
        float overrideDelay = GetEncounterRespawnOverride(encounter);
        if (overrideDelay >= 0f)
            delay = Mathf.Max(delay, overrideDelay);

        TempBattlePayload.respawnDelay = delay;

        g.currentEncounter = encounter;

        SaveReturnPoint(g);

        InventoryController inventory = FindFirstObjectByType<InventoryController>();
        inventory?.Close();

        g.PreparePartyForBattleEntry();

        if (SceneFader.I != null)
            SceneFader.I.LoadSceneWithFade("Battle");
        else
            SceneManager.LoadScene("Battle");
    }

    private void SaveReturnPoint(GameContext g)
    {
        if (g == null) return;

        // 1순위: 전용 복귀 포인트
        if (returnPointOverride != null)
        {
            g.SetReturnPoint(
                returnPointOverride.position,
                returnPointOverride.rotation,
                SceneManager.GetActiveScene().name
            );

            Debug.Log($"[BattleStarter] ReturnPointOverride used: {returnPointOverride.position}");
            return;
        }

        // 2순위: 현재 플레이어 위치
        var player = FindCurrentPlayer();
        if (player != null)
        {
            g.SetReturnPoint(
                player.position,
                player.rotation,
                SceneManager.GetActiveScene().name
            );

            Debug.Log($"[BattleStarter] Player position used as return point: {player.position}");
            return;
        }

        Debug.LogWarning("[BattleStarter] Player not found. Return point was not saved.");
    }

    private float GetEncounterRespawnOverride(EncounterData enc)
    {
        if (enc == null) return -1f;

        float best = -1f;

        void Consider(EnemyData e)
        {
            if (e == null) return;
            if (e.respawnDelayOverride >= 0f)
                best = Mathf.Max(best, e.respawnDelayOverride);
        }

        Consider(enc.guaranteedEnemy);

        if (enc.optionalSlots != null)
        {
            foreach (var slot in enc.optionalSlots)
            {
                if (slot?.candidates == null) continue;
                foreach (var c in slot.candidates)
                    Consider(c?.enemy);
            }
        }

        return best;
    }

    private Transform FindCurrentPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            return playerObj.transform;

        return null;
    }
}