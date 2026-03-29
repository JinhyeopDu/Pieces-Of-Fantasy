using UnityEngine;
using System.Collections.Generic;

public enum ItemUseFailReason
{
    None = 0,
    NoHpTarget,
    NoDeadTarget,
    SecretArtFull,
    AlreadyBuffed,
}

public enum UIOverlayKind
{
    None = 0,
    Inventory = 10,
    CharacterScreen = 20,
    QuestPanel = 30,
    // 나중에 GatherList, Map, Settings 등도 확장 가능
}

public class GameContext : MonoBehaviour
{

    public static GameContext I { get; private set; }

    [Header("Save / Load")]
    [SerializeField] private GameDataRegistry dataRegistry;

    [Header("Pending Loaded World Position")]
    public bool hasPendingLoadedWorldPosition = false;
    public Vector3 pendingLoadedWorldPosition = Vector3.zero;

    [System.NonSerialized] private Transform _registeredExplorationPlayer;

    [Header("Quest")]
    [System.NonSerialized, HideInInspector] public QuestData currentQuest;
    [System.NonSerialized, HideInInspector] public QuestRuntimeProgress currentQuestProgress;
    public List<string> completedQuestIds = new();
    public bool allQuestsCompleted = false;

    [Header("Tutorial")]
    public bool tutorialMoveDone = false;
    public bool tutorialSprintDone = false;
    public bool tutorialCharacterOpenDone = false;
    public bool tutorialLevelUpDone = false;
    public bool tutorialSecretArtDone = false;
    public bool tutorialBattleDone = false;
    public bool tutorialInventoryDone = false;
    public bool tutorialQuestDone = false;

    [Header("Load State")]
    public bool lastLoadHadQuestRestoreFailure = false;

    [Header("UI Pending Rewards (toast)")]
    public List<RewardLine> pendingRewards = new();

    [Header("Party/Inventory")]
    public List<CharacterRuntime> party = new();
    public InventoryRuntime inventory = new();

    // === [Inventory Changed Event] ===
    public System.Action OnInventoryChanged;

    [Header("Exploration / Battle")]
    public string lastExplorationSpawnPoint = "SP_Default";
    public EncounterData currentEncounter;

    [Header("Secret Art Points (Shared)")]
    public int secretArtPointsMax = 5;
    public int secretArtPoints = 5;

    [Header("Return To Exploration (Battle -> Exploration)")]
    public bool hasReturnPoint = false;
    public Vector3 returnPlayerPos;
    public Quaternion returnPlayerRot;

    // (선택) 전투 후 복귀할 씬 이름까지 저장하고 싶으면
    public string returnExplorationSceneName = "Exploration";

    // === [Battle Skill Points (Shared)] ===
    [Header("Battle Skill Points (Shared)")]
    public int battleSkillPointsMax = 5;
    public int battleSkillPoints = 0;

    // UI 갱신용 이벤트(필요 없으면 나중에 제거 가능)
    public System.Action<int, int> OnBattleSkillPointsChanged;

    [Header("Active Party")]
    public int activePartyIndex = 0;

    public bool inventorySortEnabled = false;

    [SerializeField] private UIOverlayKind _openOverlay = UIOverlayKind.None;
    public UIOverlayKind OpenOverlay => _openOverlay;

    // =========================
    // Inventory UI Batch Support
    // =========================
    private int _inventoryBatchDepth = 0;
    private bool _inventoryBatchDirty = false;

    // ──────────────────────────────────────
    // Respawn (Exploration Enemy Cooldown)
    // ──────────────────────────────────────
    [Header("Respawn (Exploration Enemy Cooldown)")]
    [Tooltip("spawnId -> respawn ready time (realtime since startup)")]
    [SerializeField] private List<string> _respawnKeys = new();
    [SerializeField] private List<float> _respawnReadyTimes = new();

    // ──────────────────────────────────────
    // Unique Defeat (Boss Permanent Despawn)
    // ──────────────────────────────────────
    [Header("Unique Defeat (Boss Permanent Despawn)")]
    [SerializeField] private List<string> _uniqueDefeatedKeys = new();

    [Header("Ending")]
    public bool endingShown = false;

    // 런타임 캐시
    private HashSet<string> _uniqueDefeatedSet;

    // 내부 캐시(Dictionary). Serialize용 리스트와 동기화해서 사용
    private Dictionary<string, float> _respawnMap;

    // UI 입력 잠금 플래그
    public bool IsUIBlockingLook { get; private set; }
    public void SetUIBlockingLook(bool v) => IsUIBlockingLook = v;

    public bool IsGatherListOpen { get; private set; }
    public void SetGatherListOpen(bool v) => IsGatherListOpen = v;

    void Awake()
    {
        //Debug.Log($"[GC] Awake instance={GetInstanceID()} scene={UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");

        if (I != null && I != this)
        {
            Debug.LogWarning($"[GC] DUPLICATE! destroy {GetInstanceID()} keep {I.GetInstanceID()}");
            Destroy(gameObject);
            return;
        }
        I = this;
        DontDestroyOnLoad(gameObject);

        RebuildRespawnMapFromLists();
        RebuildUniqueSetFromList();

        if (dataRegistry != null)
            dataRegistry.BuildMaps();

    }

    public struct ItemUsePreview
    {
        public bool isValid;
        public bool needsTargetSelect;
        public int targetIndex;

        public int[] hpBefore;
        public int[] hpAfter;
        public int[] hpDelta;

        public int secretArtBefore;
        public int secretArtAfter;
        public int secretArtDelta;
    }

    // ──────────────────────────────────────
    // New Game
    // ──────────────────────────────────────
    public void StartNewGame(CharacterData starter)
    {
        var list = new List<CharacterData>();
        if (starter != null) list.Add(starter);
        StartNewGame(list);
    }

    public void StartNewGame(List<CharacterData> starters)
    {
        // 강력추천 가드: 게임 진행 중 실수로 StartNewGame이 다시 호출되면 "모든 성장값/파티"가 리셋됨
        // 원치 않으면 아래 가드를 반드시 유지하세요.
        if (party != null && party.Count > 0)
        {
            Debug.LogWarning(
                "[GameContext] StartNewGame was called but party already exists. " +
                "Ignoring to prevent accidental reset.\n" +
                System.Environment.StackTrace
            );
            return;
        }

        //Debug.Log("[GameContext] StartNewGame CALLED\n" + System.Environment.StackTrace);

        party.Clear();

        var seen = new HashSet<CharacterData>();

        if (starters != null)
        {
            for (int i = 0; i < starters.Count; i++)
            {
                var cd = starters[i];
                if (cd == null) continue;
                if (seen.Contains(cd)) continue;

                seen.Add(cd);

                // 1) 런타임 생성
                var cr = new CharacterRuntime(cd, 1);

                // 2) 새 게임 초기화는 여기서 "명시적으로" 한다 (성장값 리셋/풀피/SP 정책 포함)
                cr.InitForNewGame();

                party.Add(cr);
            }
        }

        if (party.Count == 0)
        {
            Debug.LogWarning("[GameContext] StartNewGame: starters가 비어 파티가 비었습니다. 최소 1명의 CharacterData가 필요합니다.");
        }

        inventory = new InventoryRuntime();

        lastExplorationSpawnPoint = "SP_Default";
        currentEncounter = null;

        secretArtPointsMax = 5;
        secretArtPoints = secretArtPointsMax;

        battleSkillPointsMax = 5;
        battleSkillPoints = 0;

        activePartyIndex = 0;
        endingShown = false;

        currentQuest = null;
        currentQuestProgress = null;
        completedQuestIds.Clear();
        allQuestsCompleted = false;

        tutorialMoveDone = false;
        tutorialSprintDone = false;
        tutorialCharacterOpenDone = false;
        tutorialLevelUpDone = false;
        tutorialSecretArtDone = false;
        tutorialBattleDone = false;
        tutorialInventoryDone = false;
        tutorialQuestDone = false;

        // 진단 로그: 새 게임 직후 상태 확정
        for (int i = 0; i < party.Count; i++)
        {
            var p = party[i];
            if (p == null || p.data == null) continue;
            //Debug.Log($"[GameContext] party[{i}] {p.data.name} lv={p.level} promo={p.promotionStage} exp={p.exp} hp={p.hp}/{p.maxHp} sp={p.sp}");
        }
    }

    // ──────────────────────────────────────
    // Save/Load
    // ──────────────────────────────────────

    public void ResetForNewGame()
    {
        // Party / Inventory
        party.Clear();
        inventory = new InventoryRuntime();

        // Quest
        currentQuest = null;
        currentQuestProgress = null;
        completedQuestIds.Clear();
        allQuestsCompleted = false;

        // Tutorial
        tutorialMoveDone = false;
        tutorialSprintDone = false;
        tutorialCharacterOpenDone = false;
        tutorialLevelUpDone = false;
        tutorialSecretArtDone = false;
        tutorialBattleDone = false;
        tutorialInventoryDone = false;
        tutorialQuestDone = false;

        // Pending rewards
        pendingRewards.Clear();

        // Encounter / battle return
        currentEncounter = null;
        lastExplorationSpawnPoint = "SP_Default";

        hasReturnPoint = false;
        returnPlayerPos = Vector3.zero;
        returnPlayerRot = Quaternion.identity;
        returnExplorationSceneName = "Exploration";

        // Shared points
        secretArtPointsMax = 5;
        secretArtPoints = secretArtPointsMax;

        battleSkillPointsMax = 5;
        battleSkillPoints = 0;
        OnBattleSkillPointsChanged?.Invoke(battleSkillPoints, battleSkillPointsMax);

        // Active party / UI state
        activePartyIndex = 0;
        inventorySortEnabled = false;
        _openOverlay = UIOverlayKind.None;
        SetUIBlockingLook(false);
        SetGatherListOpen(false);

        endingShown = false;

        // Save/Load world restore state
        hasPendingLoadedWorldPosition = false;
        pendingLoadedWorldPosition = Vector3.zero;

        // Respawn / unique defeat
        ClearAllRespawn();

        _uniqueDefeatedKeys.Clear();
        RebuildUniqueSetFromList();

        //Debug.Log("[GameContext] ResetForNewGame complete.");
    }

    public SaveData BuildSaveData()
    {
        var data = new SaveData();

        // Scene
        data.currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        data.returnExplorationSceneName = returnExplorationSceneName;

        // Party / active
        data.activePartyIndex = activePartyIndex;

        if (party != null)
        {
            for (int i = 0; i < party.Count; i++)
            {
                var cr = party[i];
                if (cr == null || cr.data == null) continue;

                var c = new CharacterSaveData
                {
                    characterId = cr.data.id,

                    level = cr.level,
                    exp = cr.exp,
                    hp = cr.hp,
                    maxHp = cr.maxHp,
                    sp = cr.sp,
                    promotionStage = cr.promotionStage,

                    atk = cr.atk,
                    def = cr.def,
                    spd = cr.spd,

                    permAtkAdd = cr.permAtkAdd,
                    permDefAdd = cr.permDefAdd,
                    permSpdAdd = cr.permSpdAdd,

                    tempAtkAdd = cr.tempAtkAdd,
                    tempDefAdd = cr.tempDefAdd,
                    tempSpdAdd = cr.tempSpdAdd,

                    secretArtReady = cr.secretArtReady
                };

                if (cr.tempAtkSources != null)
                    c.tempAtkSources = new List<string>(cr.tempAtkSources);

                if (cr.tempDefSources != null)
                    c.tempDefSources = new List<string>(cr.tempDefSources);

                if (cr.tempSpdSources != null)
                    c.tempSpdSources = new List<string>(cr.tempSpdSources);

                if (cr.tempMaxHpSources != null)
                    c.tempMaxHpSources = new List<string>(cr.tempMaxHpSources);

                data.party.Add(c);
            }
        }

        // Inventory
        if (inventory != null && inventory.items != null)
        {
            for (int i = 0; i < inventory.items.Count; i++)
            {
                var st = inventory.items[i];
                if (st.item == null || st.count <= 0) continue;

                data.inventory.Add(new InventoryItemSaveData
                {
                    itemId = st.item.id,
                    count = st.count
                });
            }
        }

        // Shared points / battle
        data.secretArtPoints = secretArtPoints;
        data.secretArtPointsMax = secretArtPointsMax;
        data.battleSkillPoints = battleSkillPoints;
        data.battleSkillPointMax = battleSkillPointsMax;

        // Optional UI
        data.inventorySortEnabled = inventorySortEnabled;
        data.isUIBlockingLook = IsUIBlockingLook;

        // Optional world position
        // 우선순위:
        // 1) return point가 있으면 그 좌표를 저장
        // 2) 없으면 현재 플레이어 위치 저장
        Transform saveTf = FindBestSavePlayerTransform();

        if (hasReturnPoint)
        {
            data.hasWorldPosition = true;
            data.worldPosition = SerializableVector3.FromVector3(returnPlayerPos);
#if UNITY_EDITOR
            Debug.Log($"[SaveData] Save return point = {returnPlayerPos}");
#endif
        }
        else if (saveTf != null)
        {
            data.hasWorldPosition = true;
            data.worldPosition = SerializableVector3.FromVector3(saveTf.position);
#if UNITY_EDITOR
            Debug.Log($"[SaveData] Save world pos = {saveTf.position} ({saveTf.name})");
#endif
        }
        else
        {
            data.hasWorldPosition = false;
            Debug.LogWarning("[SaveData] No valid player transform found. world position was not saved.");
        }

        // Respawn / unique defeat
        FillDefeatStateToSaveData(data);

        data.endingShown = endingShown;
        data.allQuestsCompleted = allQuestsCompleted;

        data.tutorialMoveDone = tutorialMoveDone;
        data.tutorialSprintDone = tutorialSprintDone;
        data.tutorialCharacterOpenDone = tutorialCharacterOpenDone;
        data.tutorialLevelUpDone = tutorialLevelUpDone;
        data.tutorialSecretArtDone = tutorialSecretArtDone;
        data.tutorialBattleDone = tutorialBattleDone;
        data.tutorialInventoryDone = tutorialInventoryDone;
        data.tutorialQuestDone = tutorialQuestDone;

        if (currentQuestProgress != null)
        {
            data.currentQuestId = currentQuest != null
                ? currentQuest.questId
                : currentQuestProgress.questId;

            data.currentQuestValue = currentQuestProgress.currentValue;
            data.currentQuestCompleted = currentQuestProgress.isCompleted;
            data.currentQuestRewardClaimed = currentQuestProgress.rewardClaimed;
        }
        else
        {
            data.currentQuestId = null;
            data.currentQuestValue = 0;
            data.currentQuestCompleted = false;
            data.currentQuestRewardClaimed = false;
        }

#if UNITY_EDITOR
        Debug.Log(
            $"[Save] Quest | id={data.currentQuestId ?? "null"} | " +
            $"value={data.currentQuestValue} | " +
            $"completed={data.currentQuestCompleted} | " +
            $"reward={data.currentQuestRewardClaimed} | " +
            $"completedCount={(data.completedQuestIds != null ? data.completedQuestIds.Count : 0)} | " +
            $"allDone={data.allQuestsCompleted}"
        );
#endif

        data.completedQuestIds = new List<string>(completedQuestIds);

        return data;
    }

    public void RegisterExplorationPlayer(Transform playerTf)
    {
        _registeredExplorationPlayer = playerTf;
    }

    public void ClearExplorationPlayer(Transform playerTf = null)
    {
        if (playerTf == null || _registeredExplorationPlayer == playerTf)
            _registeredExplorationPlayer = null;
    }

    private Transform FindBestSavePlayerTransform()
    {
        if (_registeredExplorationPlayer == null)
            return null;

        if (!_registeredExplorationPlayer.gameObject.activeInHierarchy)
            return null;

        return _registeredExplorationPlayer;
    }

    public void SaveGame()
    {
        var data = BuildSaveData();
        SaveManager.Save(data);
    }

    public void ApplySaveData(SaveData data)
    {
        if (data == null)
        {
            Debug.LogWarning("[GameContext] ApplySaveData failed: data is null.");
            return;
        }

        if (dataRegistry == null)
        {
            Debug.LogError("[GameContext] ApplySaveData failed: dataRegistry is null.");
            return;
        }

        dataRegistry.BuildMaps();

        // 먼저 완전 초기화
        ResetForNewGame();

        // Scene-related
        returnExplorationSceneName = string.IsNullOrEmpty(data.returnExplorationSceneName)
            ? "Exploration"
            : data.returnExplorationSceneName;

        // Continue 로드는 Battle 복귀가 아니므로 return point는 비활성화
        hasReturnPoint = false;
        returnPlayerPos = Vector3.zero;
        returnPlayerRot = Quaternion.identity;

        // Party
        party.Clear();

        for (int i = 0; i < data.party.Count; i++)
        {
            var src = data.party[i];
            var cd = dataRegistry.GetCharacter(src.characterId);

            if (cd == null)
            {
                Debug.LogWarning($"[GameContext] CharacterData not found for id: {src.characterId}");
                continue;
            }

            var cr = new CharacterRuntime(cd, Mathf.Max(1, src.level));

            cr.exp = src.exp;
            cr.hp = src.hp;
            cr.maxHp = src.maxHp;
            cr.sp = src.sp;
            cr.promotionStage = src.promotionStage;

            cr.atk = src.atk;
            cr.def = src.def;
            cr.spd = src.spd;

            cr.permAtkAdd = src.permAtkAdd;
            cr.permDefAdd = src.permDefAdd;
            cr.permSpdAdd = src.permSpdAdd;

            cr.tempAtkAdd = src.tempAtkAdd;
            cr.tempDefAdd = src.tempDefAdd;
            cr.tempSpdAdd = src.tempSpdAdd;

            cr.secretArtReady = src.secretArtReady;

            cr.tempAtkSources = src.tempAtkSources != null
                ? new HashSet<string>(src.tempAtkSources)
                : new HashSet<string>();

            cr.tempDefSources = src.tempDefSources != null
                ? new HashSet<string>(src.tempDefSources)
                : new HashSet<string>();

            cr.tempSpdSources = src.tempSpdSources != null
                ? new HashSet<string>(src.tempSpdSources)
                : new HashSet<string>();

            cr.tempMaxHpSources = src.tempMaxHpSources != null
                ? new HashSet<string>(src.tempMaxHpSources)
                : new HashSet<string>();

            // 최종 스탯 재계산
            cr.RecalculateStats(keepHpRatio: false);

            // 저장값 우선 복원
            cr.hp = Mathf.Clamp(src.hp, 0, cr.maxHp);
            cr.sp = src.sp;

            party.Add(cr);
        }
#if UNITY_EDITOR
        Debug.Log(
            $"[Load] Quest | id={data.currentQuestId ?? "null"} | " +
            $"value={data.currentQuestValue} | " +
            $"completed={data.currentQuestCompleted} | " +
            $"reward={data.currentQuestRewardClaimed} | " +
            $"completedCount={(data.completedQuestIds != null ? data.completedQuestIds.Count : 0)} | " +
            $"allDone={data.allQuestsCompleted}"
        );
#endif

        // Inventory
        inventory = new InventoryRuntime();

        for (int i = 0; i < data.inventory.Count; i++)
        {
            var src = data.inventory[i];
            var item = dataRegistry.GetItem(src.itemId);

            if (item == null)
            {
                Debug.LogWarning($"[GameContext] ItemData not found for id: {src.itemId}");
                continue;
            }

            AddItem(item, src.count);
        }

        // Shared points
        secretArtPoints = Mathf.Clamp(data.secretArtPoints, 0, data.secretArtPointsMax);
        secretArtPointsMax = Mathf.Max(1, data.secretArtPointsMax);

        battleSkillPointsMax = Mathf.Max(0, data.battleSkillPointMax);
        battleSkillPoints = Mathf.Clamp(data.battleSkillPoints, 0, battleSkillPointsMax);
        OnBattleSkillPointsChanged?.Invoke(battleSkillPoints, battleSkillPointsMax);

        // Optional UI state
        inventorySortEnabled = data.inventorySortEnabled;

        // Quest / Ending state
        endingShown = data.endingShown;
        allQuestsCompleted = data.allQuestsCompleted;
        completedQuestIds = data.completedQuestIds != null
            ? new List<string>(data.completedQuestIds)
            : new List<string>();

        lastLoadHadQuestRestoreFailure = false;

        tutorialMoveDone = data.tutorialMoveDone;
        tutorialSprintDone = data.tutorialSprintDone;
        tutorialCharacterOpenDone = data.tutorialCharacterOpenDone;
        tutorialLevelUpDone = data.tutorialLevelUpDone;
        tutorialSecretArtDone = data.tutorialSecretArtDone;
        tutorialBattleDone = data.tutorialBattleDone;
        tutorialInventoryDone = data.tutorialInventoryDone;
        tutorialQuestDone = data.tutorialQuestDone;

        currentQuest = null;
        currentQuestProgress = null;

        if (!string.IsNullOrEmpty(data.currentQuestId))
        {
            var quest = dataRegistry.GetQuest(data.currentQuestId);
            if (quest != null)
            {
                currentQuest = quest;
                currentQuestProgress = new QuestRuntimeProgress(quest.questId);
                currentQuestProgress.currentValue = data.currentQuestValue;
                currentQuestProgress.isCompleted = data.currentQuestCompleted;
                currentQuestProgress.rewardClaimed = data.currentQuestRewardClaimed;
            }
            else
            {
                lastLoadHadQuestRestoreFailure = true;

                Debug.LogWarning(
                    $"[GameContext] QuestData not found for id: {data.currentQuestId} | " +
                    $"registry={(dataRegistry != null ? dataRegistry.name : "NULL")}"
                );
            }
        }
        else
        {
            //Debug.Log("[GameContext] No currentQuestId in save.");
        }

        // Overlay/UI는 안전하게 닫힌 상태로 시작 권장
        _openOverlay = UIOverlayKind.None;
        SetUIBlockingLook(false);
        SetGatherListOpen(false);

        // Active character
        activePartyIndex = Mathf.Clamp(data.activePartyIndex, 0, Mathf.Max(0, party.Count - 1));

        // Optional world position
        if (data.hasWorldPosition)
        {
            hasPendingLoadedWorldPosition = true;
            pendingLoadedWorldPosition = data.worldPosition.ToVector3();

#if UNITY_EDITOR
            Debug.Log($"[LoadData] Loaded world pos = {pendingLoadedWorldPosition}");
#endif
        }
        else
        {
            hasPendingLoadedWorldPosition = false;
            Debug.LogWarning("[LoadData] Save file has no world position.");
        }

        // Respawn / unique defeat
        ApplyDefeatStateFromSaveData(data);

#if UNITY_EDITOR
        Debug.Log(
            $"[GameContext] Restored currentQuest={(currentQuest != null ? currentQuest.questId : "null")} | " +
            $"progress={(currentQuestProgress != null ? currentQuestProgress.currentValue.ToString() : "null")}"
        );
#endif

#if UNITY_EDITOR
        Debug.Log("[GameContext] ApplySaveData complete.");
#endif
    }



    public bool LoadGameFromSave()
    {
        if (!SaveManager.HasSave())
            return false;

        var data = SaveManager.Load();
        if (data == null)
            return false;

        ApplySaveData(data);
        return true;
    }




    // ──────────────────────────────────────
    // Active Character
    // ──────────────────────────────────────
    public CharacterRuntime GetActiveCharacter()
    {
        if (party == null || party.Count == 0) return null;
        activePartyIndex = Mathf.Clamp(activePartyIndex, 0, party.Count - 1);
        return party[activePartyIndex];
    }

    // ──────────────────────────────────────
    // 전투불능/교대/전멸 헬퍼
    // ──────────────────────────────────────
    public bool IsPartyWiped()
    {
        if (party == null || party.Count == 0) return true;
        for (int i = 0; i < party.Count; i++)
        {
            if (party[i] != null && party[i].hp > 0)
                return false;
        }
        return true;
    }

    public int GetFirstAliveIndex()
    {
        if (party == null) return -1;
        for (int i = 0; i < party.Count; i++)
        {
            if (party[i] != null && party[i].hp > 0)
                return i;
        }
        return -1;
    }

    public bool TrySetActiveIndex(int idx)
    {
        if (party == null || idx < 0 || idx >= party.Count) return false;
        if (party[idx] == null || party[idx].hp <= 0) return false;

        activePartyIndex = idx;
        return true;
    }

    public bool EnsureActiveIsAlive()
    {
        var cr = GetActiveCharacter();
        if (cr != null && cr.hp > 0) return true;

        int alive = GetFirstAliveIndex();
        if (alive >= 0)
        {
            activePartyIndex = alive;
            return true;
        }

        // 여기까지 왔다는 건 "전원이 hp<=0" 상태.
        // 새 게임 직후엔 데이터 생성/초기화 문제일 가능성이 매우 높으므로 복구(정책 선택)
        if (party != null && party.Count > 0)
        {
            for (int i = 0; i < party.Count; i++)
            {
                var p = party[i];
                if (p == null || p.data == null) continue;

                p.maxHp = Mathf.Max(1, p.maxHp > 0 ? p.maxHp : p.data.baseHP);
                p.hp = p.maxHp;
            }

            activePartyIndex = 0;
            Debug.LogWarning("[GameContext] EnsureActiveIsAlive: party was all dead. Restored HP to full (safety).");
            return true;
        }

        return false;
    }

    // ──────────────────────────────────────
    // Secret Art Helpers
    // ──────────────────────────────────────
    public void ConsumeAllSecretArtReady()
    {
        if (party == null) return;
        for (int i = 0; i < party.Count; i++)
        {
            if (party[i] != null)
                party[i].secretArtReady = false;
        }
    }

    // ──────────────────────────────────────
    // Battle Skill Points
    // ──────────────────────────────────────
    public void ResetBattleSkillPoints(int startPoints, int maxPoints)
    {
        battleSkillPointsMax = Mathf.Max(0, maxPoints);
        battleSkillPoints = Mathf.Clamp(startPoints, 0, battleSkillPointsMax);
        OnBattleSkillPointsChanged?.Invoke(battleSkillPoints, battleSkillPointsMax);
    }

    public bool CanSpendBattleSkillPoint(int cost)
    {
        if (cost <= 0) return true;
        return battleSkillPoints >= cost;
    }

    public bool TrySpendBattleSkillPoint(int cost)
    {
        if (cost <= 0) return true;
        if (battleSkillPoints < cost) return false;

        battleSkillPoints -= cost;
        OnBattleSkillPointsChanged?.Invoke(battleSkillPoints, battleSkillPointsMax);
        return true;
    }

    public void AddBattleSkillPoints(int delta)
    {
        if (delta == 0) return;
        battleSkillPoints = Mathf.Clamp(battleSkillPoints + delta, 0, battleSkillPointsMax);
        OnBattleSkillPointsChanged?.Invoke(battleSkillPoints, battleSkillPointsMax);
    }

    // ──────────────────────────────────────
    // Return Point (Battle -> Exploration)
    // ──────────────────────────────────────
    public void SetReturnPoint(Vector3 pos, Quaternion rot, string sceneName = "Exploration")
    {
        hasReturnPoint = true;
        returnPlayerPos = pos;
        returnPlayerRot = rot;
        returnExplorationSceneName = string.IsNullOrEmpty(sceneName) ? "Exploration" : sceneName;
    }

    public void ClearReturnPoint()
    {
        hasReturnPoint = false;
    }

    void RebuildUniqueSetFromList()
    {
        _uniqueDefeatedSet = new HashSet<string>();
        for (int i = 0; i < _uniqueDefeatedKeys.Count; i++)
        {
            var k = _uniqueDefeatedKeys[i];
            if (!string.IsNullOrEmpty(k))
                _uniqueDefeatedSet.Add(k);
        }
    }

    void SyncListFromUniqueSet()
    {
        _uniqueDefeatedKeys.Clear();
        if (_uniqueDefeatedSet == null) return;

        foreach (var k in _uniqueDefeatedSet)
            _uniqueDefeatedKeys.Add(k);
    }

    public bool IsUniqueDefeated(string spawnId)
    {
        if (string.IsNullOrEmpty(spawnId)) return false;
        if (_uniqueDefeatedSet == null) RebuildUniqueSetFromList();
        return _uniqueDefeatedSet.Contains(spawnId);
    }

    public void MarkUniqueDefeated(string spawnId)
    {
        if (string.IsNullOrEmpty(spawnId)) return;
        if (_uniqueDefeatedSet == null) RebuildUniqueSetFromList();

        if (_uniqueDefeatedSet.Add(spawnId))
            SyncListFromUniqueSet();
    }

    public ItemUsePreview PreviewUseItem(ItemData item, int targetIndex = 0)
    {
        var p = new ItemUsePreview
        {
            isValid = false,
            secretArtBefore = secretArtPoints,
            secretArtAfter = secretArtPoints,   // ★ 추가: 기본은 before 유지
        };

        if (item == null) return p;
        if (item.itemType != ItemType.Consumable) return p;
        if (item.useScope != ItemUseScope.ExplorationOnly) return p;
        if (party == null || party.Count == 0) return p;

        int n = party.Count;
        targetIndex = Mathf.Clamp(targetIndex, 0, n - 1);

        p.needsTargetSelect = (item.targetPolicy == ItemTargetPolicy.SingleAlly);
        p.targetIndex = targetIndex;

        p.hpBefore = new int[n];
        p.hpAfter = new int[n];
        p.hpDelta = new int[n];

        for (int i = 0; i < n; i++)
        {
            var c = party[i];
            int hp = (c != null) ? c.hp : 0;
            p.hpBefore[i] = hp;
            p.hpAfter[i] = hp;
            p.hpDelta[i] = 0;
        }

        // 효과 A/B를 프리뷰에 반영
        ApplyEffectSlotPreview(item.effectA, item, ref p, targetIndex);
        ApplyEffectSlotPreview(item.effectB, item, ref p, targetIndex);

        for (int i = 0; i < n; i++)
            p.hpDelta[i] = Mathf.Max(0, p.hpAfter[i] - p.hpBefore[i]);

        p.secretArtAfter = Mathf.Clamp(p.secretArtAfter, 0, secretArtPointsMax);
        p.secretArtDelta = Mathf.Max(0, p.secretArtAfter - p.secretArtBefore);

        p.isValid = true;
        return p;
    }

    private void PreviewHealAt(int idx, ItemEffectSlot slot, ref ItemUsePreview p)
    {
        if (idx < 0 || idx >= party.Count) return;
        var c = party[idx];
        if (c == null) return;
        if (c.hp <= 0) return; // 네 기존 정책 유지(죽어있으면 힐로 못 살림)

        int maxHp = c.maxHp;
        int cur = p.hpAfter[idx];

        int amount = slot.isPercent ? Mathf.CeilToInt(maxHp * (slot.value / 100f)) : slot.value;
        if (amount <= 0) return;

        p.hpAfter[idx] = Mathf.Min(maxHp, cur + amount);
    }

    private void ApplyEffectSlotPreview(ItemEffectSlot slot, ItemData item, ref ItemUsePreview p, int targetIndex)
    {
        if (slot == null) return;
        if (slot.type == ConsumableEffectType.None) return;

        int n = party.Count;

        switch (slot.type)
        {
            case ConsumableEffectType.HealHP:
                {
                    if (item.targetPolicy == ItemTargetPolicy.AllParty)
                    {
                        for (int i = 0; i < n; i++)
                            PreviewHealAt(i, slot, ref p);
                    }
                    else if (item.targetPolicy == ItemTargetPolicy.SingleAlly)
                    {
                        PreviewHealAt(targetIndex, slot, ref p);
                        p.targetIndex = targetIndex;
                    }
                    // None이면 HealHP랑 논리 충돌 -> 무시
                    break;
                }

            case ConsumableEffectType.RestoreSecretArt:
                {
                    // 공용 자원: 대상 선택 없음
                    int add = Mathf.Max(0, slot.value);
                    p.secretArtAfter = Mathf.Clamp(p.secretArtBefore + add, 0, secretArtPointsMax);
                    break;
                }

            case ConsumableEffectType.Revive:
                {
                    // 프리뷰에서는 "부활 가능한 첫 대상"을 찾아 HP=1로 보여줌
                    // (Single/All 정책과 별개로, 실제 정책을 정할 수 있음)
                    for (int i = 0; i < n; i++)
                    {
                        var c = party[i];
                        if (c != null && c.hp <= 0)
                        {
                            p.hpAfter[i] = Mathf.Max(p.hpAfter[i], 1);
                            break;
                        }
                    }
                    break;
                }

            // 버프류는 프리뷰 UI에 표시하지 않는다고 가정(원하면 나중에 추가)
            case ConsumableEffectType.BuffSpeed:
            case ConsumableEffectType.BuffDefense:
            case ConsumableEffectType.BuffMaxHP:
                break;
        }
    }

    //private void PreviewHealAt(int idx, ItemEffectSlot slot, ref ItemUsePreview p)
    //{
    //    if (idx < 0 || idx >= party.Count) return;
    //    var c = party[idx];
    //    if (c == null) return;

    //    // 죽어있으면 힐 프리뷰 X (현재 네 정책과 동일)
    //    if (c.hp <= 0) return;

    //    int maxHp = c.maxHp;
    //    int cur = p.hpAfter[idx];

    //    int amount = slot.isPercent ? Mathf.CeilToInt(maxHp * (slot.value / 100f)) : slot.value;
    //    if (amount <= 0) return;

    //    int after = Mathf.Min(maxHp, cur + amount);
    //    p.hpAfter[idx] = after;
    //}

    // ──────────────────────────────────────
    // 아이템 추가
    // ──────────────────────────────────────
    public void AddItem(ItemData item, int amount)
    {
        if (item == null || amount <= 0) return;

        if (inventory == null)
            inventory = new InventoryRuntime();
        if (inventory.items == null)
            inventory.items = new List<ItemStack>();

        if (inventory.items.Count > 10000)
        {
            Debug.LogError("[Safety] Inventory item count exceeded 10,000. Aborting AddItem to prevent memory explosion.");
            return;
        }

        if (item == null || amount <= 0) return;

        if (inventory == null)
            inventory = new InventoryRuntime();

        if (inventory.items == null)
            inventory.items = new List<ItemStack>();

        int stackLimit = Mathf.Max(1, item.maxStack);

        if (stackLimit == 1)
        {
            // ★ 핵심: List 재할당/복사 스파이크 최소화
            int needed = inventory.items.Count + amount;
            if (inventory.items.Capacity < needed)
                inventory.items.Capacity = needed;

            for (int i = 0; i < amount; i++)
            {
                inventory.items.Add(new ItemStack { item = item, count = 1 });
            }

            MarkInventoryDirtyOrNotify();
            return;
        }

        for (int i = 0; i < inventory.items.Count && amount > 0; i++)
        {
            var s = inventory.items[i];
            if (s.item != item) continue;

            int space = stackLimit - s.count;
            if (space <= 0) continue;

            int add = Mathf.Min(space, amount);
            s.count += add;
            amount -= add;
            inventory.items[i] = s;
        }

        while (amount > 0)
        {
            int add = Mathf.Min(stackLimit, amount);
            inventory.items.Add(new ItemStack { item = item, count = add });
            amount -= add;
        }

        MarkInventoryDirtyOrNotify();
    }

    // 사용(소모품이면 1개 소모)
    // 기존 시그니처는 호환용 유지
    public bool TryUseItem(ItemData item)
    {
        return TryUseItem(item, 0);
    }

    // 새 시그니처: 대상 인덱스 지원
    public bool TryUseItem(ItemData item, int targetIndex)
    {
        if (item == null) return false;
        if (item.itemType != ItemType.Consumable) return false;

        // 탐험 전용 사용 정책
        if (item.useScope != ItemUseScope.ExplorationOnly)
            return false;

        if (party == null || party.Count == 0)
            return false;

        targetIndex = Mathf.Clamp(targetIndex, 0, party.Count - 1);

        bool applied = false;

        applied |= ApplyItemEffectSlot(item.effectA, item, targetIndex);
        applied |= ApplyItemEffectSlot(item.effectB, item, targetIndex);

        if (!applied)
            return false;

        return RemoveItem(item, 1);
    }

    private bool ApplyItemEffectSlot(ItemEffectSlot slot, ItemData item, int targetIndex)
    {
        if (slot == null) return false;
        if (slot.type == ConsumableEffectType.None) return false;

        if (party == null || party.Count == 0)
            return false;

        targetIndex = Mathf.Clamp(targetIndex, 0, party.Count - 1);

        bool anyApplied = false;

        switch (slot.type)
        {
            case ConsumableEffectType.HealHP:
                {
                    if (item.targetPolicy == ItemTargetPolicy.AllParty)
                    {
                        for (int i = 0; i < party.Count; i++)
                            anyApplied |= ApplyHealHP(party[i], slot);
                    }
                    else if (item.targetPolicy == ItemTargetPolicy.SingleAlly)
                    {
                        anyApplied |= ApplyHealHP(party[targetIndex], slot);
                    }
                    // None이면 HealHP와 정책 충돌 → 적용 안 함
                    return anyApplied;
                }

            case ConsumableEffectType.RestoreSecretArt:
                {
                    // 공용 자원: 대상 무관
                    return ApplyRestoreSecretArt(null, slot);
                }

            case ConsumableEffectType.Revive:
                {
                    if (item.targetPolicy == ItemTargetPolicy.SingleAlly)
                        return ApplyReviveAt(targetIndex);
                    else
                        return ApplyRevive(); // 기존 정책(첫 사망자 부활)
                }

            case ConsumableEffectType.BuffSpeed:
                {
                    if (item.targetPolicy == ItemTargetPolicy.AllParty)
                    {
                        for (int i = 0; i < party.Count; i++)
                            anyApplied |= ApplyBuffSpeed(party[i], slot, item);
                    }
                    else if (item.targetPolicy == ItemTargetPolicy.SingleAlly)
                    {
                        anyApplied |= ApplyBuffSpeed(party[targetIndex], slot, item);
                    }
                    return anyApplied;
                }

            case ConsumableEffectType.BuffDefense:
                {
                    if (item.targetPolicy == ItemTargetPolicy.AllParty)
                    {
                        for (int i = 0; i < party.Count; i++)
                            anyApplied |= ApplyBuffDefense(party[i], slot, item);
                    }
                    else if (item.targetPolicy == ItemTargetPolicy.SingleAlly)
                    {
                        anyApplied |= ApplyBuffDefense(party[targetIndex], slot, item);
                    }
                    return anyApplied;
                }

            case ConsumableEffectType.BuffMaxHP:
                {
                    if (item.targetPolicy == ItemTargetPolicy.AllParty)
                    {
                        for (int i = 0; i < party.Count; i++)
                            anyApplied |= ApplyBuffMaxHP(party[i], slot, item);
                    }
                    else if (item.targetPolicy == ItemTargetPolicy.SingleAlly)
                    {
                        anyApplied |= ApplyBuffMaxHP(party[targetIndex], slot, item);
                    }
                    return anyApplied;
                }

            case ConsumableEffectType.BuffAttack:
                {
                    if (item.targetPolicy == ItemTargetPolicy.AllParty)
                    {
                        for (int i = 0; i < party.Count; i++)
                            anyApplied |= ApplyBuffAttack(party[i], slot, item);
                    }
                    else if (item.targetPolicy == ItemTargetPolicy.SingleAlly)
                    {
                        anyApplied |= ApplyBuffAttack(party[targetIndex], slot, item);
                    }
                    return anyApplied;
                }
        }

        return false;
    }

    private bool ApplyReviveAt(int index)
    {
        if (party == null) return false;
        if (index < 0 || index >= party.Count) return false;

        var c = party[index];
        if (c == null) return false;
        if (c.hp > 0) return false; // 살아있으면 실패(소모 X)

        c.hp = 1;
        return true;
    }

    // HealHP 체력 회복 관련
    private bool ApplyHealHP(CharacterRuntime target, ItemEffectSlot slot)
    {
        if (target == null) return false;
        if (target.hp <= 0) return false; // 죽어있으면 힐로 못 살림(부활은 별도)

        int amount = slot.isPercent ? Mathf.CeilToInt(target.maxHp * (slot.value / 100f)) : slot.value;
        if (amount <= 0) return false;

        target.hp = Mathf.Min(target.maxHp, target.hp + amount);
        return true;
    }

    // RestoreSecretArt 비술 회복 관련 
    private bool ApplyRestoreSecretArt(CharacterRuntime target, ItemEffectSlot slot)
    {
        int amount = slot.value;
        if (amount <= 0) return false;

        secretArtPoints = Mathf.Clamp(secretArtPoints + amount, 0, secretArtPointsMax);
        return true;
    }

    // Revive (HP를 1 남기고 부활)
    private bool ApplyRevive()
    {
        if (party == null) return false;

        for (int i = 0; i < party.Count; i++)
        {
            var c = party[i];
            if (c == null) continue;
            if (c.hp > 0) continue;

            c.hp = 1; // 요구사항: HP 1 남기고 부활
            return true;
        }

        return false; // 죽은 캐릭터 없으면 실패(소모 X)
    }

    // ──────────────────────────────────────────────────────────────
    // BuffSpeed / BuffDefense / BuffMaxHP(전투 종료 시 해제용)
    // ──────────────────────────────────────────────────────────────
    private bool ApplyBuffSpeed(CharacterRuntime target, ItemEffectSlot slot, ItemData sourceItem)
    {
        if (target == null) return false;
        if (slot.value == 0) return false;
        if (sourceItem == null || string.IsNullOrEmpty(sourceItem.id)) return false;

        // 같은 아이템 id로 이미 speed 버프를 받은 상태면 막기
        target.tempSpdSources ??= new HashSet<string>();
        if (target.tempSpdSources.Contains(sourceItem.id))
            return false;

        target.tempSpdSources.Add(sourceItem.id);
        target.tempSpdAdd += slot.value;
        return true;
    }

    private bool ApplyBuffDefense(CharacterRuntime target, ItemEffectSlot slot, ItemData sourceItem)
    {
        if (target == null) return false;
        if (slot.value == 0) return false;
        if (sourceItem == null || string.IsNullOrEmpty(sourceItem.id)) return false;

        target.tempDefSources ??= new HashSet<string>();
        if (target.tempDefSources.Contains(sourceItem.id))
            return false;

        target.tempDefSources.Add(sourceItem.id);
        target.tempDefAdd += slot.value;
        return true;
    }

    private bool ApplyBuffMaxHP(CharacterRuntime target, ItemEffectSlot slot, ItemData sourceItem)
    {
        if (target == null) return false;
        if (sourceItem == null || string.IsNullOrEmpty(sourceItem.id)) return false;

        int amount = slot.isPercent ? Mathf.CeilToInt(target.maxHp * (slot.value / 100f)) : slot.value;
        if (amount == 0) return false;

        target.tempMaxHpSources ??= new HashSet<string>();
        if (target.tempMaxHpSources.Contains(sourceItem.id))
            return false;

        target.tempMaxHpSources.Add(sourceItem.id);

        target.tempMaxHpAdd += amount;
        target.maxHp += amount;
        target.hp = Mathf.Min(target.maxHp, target.hp + amount);
        return true;
    }

    private bool ApplyBuffAttack(CharacterRuntime target, ItemEffectSlot slot, ItemData sourceItem)
    {
        if (target == null) return false;
        if (slot.value == 0) return false;
        if (sourceItem == null || string.IsNullOrEmpty(sourceItem.id)) return false;

        target.tempAtkSources ??= new HashSet<string>();
        if (target.tempAtkSources.Contains(sourceItem.id))
            return false;

        target.tempAtkSources.Add(sourceItem.id);
        target.tempAtkAdd += slot.value;
        return true;
    }

    // ──────────────────────────────────────────────────────────────
    // BuffSpeed / BuffDefense / BuffMaxHP(전투 종료 시 해제용)
    // ──────────────────────────────────────────────────────────────

    // 전투 종료 시 버프 해제 함수
    public void ClearBattleTemporaryBuffs()
    {
        if (party == null) return;

        foreach (var c in party)
        {
            if (c == null) continue;

            if (c.tempMaxHpAdd != 0)
            {
                c.maxHp -= c.tempMaxHpAdd;
                c.hp = Mathf.Min(c.hp, c.maxHp);
                c.tempMaxHpAdd = 0;
            }

            c.tempAtkAdd = 0; // (공격 버프도 쓰게 될 거라면 같이)
            c.tempSpdAdd = 0;
            c.tempDefAdd = 0;

            // 소스 기록 초기화(전투 다녀오면 다시 사용 가능)
            // 같은 아이템 중복방지 기록 초기화
            c.tempAtkSources?.Clear();
            c.tempSpdSources?.Clear();
            c.tempDefSources?.Clear();
            c.tempMaxHpSources?.Clear();
        }
    }

    public void QueueReward(ItemData item, int qty)
    {
        if (item == null || qty <= 0) return;

        // 같은 아이템은 합산(토스트 줄 수 줄이기)
        for (int i = 0; i < pendingRewards.Count; i++)
        {
            if (pendingRewards[i].item == item)
            {
                var r = pendingRewards[i];
                r.qty += qty;
                pendingRewards[i] = r;
                return;
            }
        }

        pendingRewards.Add(new RewardLine { item = item, qty = qty });
    }

    // 탐험 씬에서 호출: 읽고 비우기(한 번만 표시되게)
    public List<RewardLine> ConsumePendingRewards()
    {
        var copy = new List<RewardLine>(pendingRewards);
        pendingRewards.Clear();
        return copy;
    }

    // ──────────────────────────────────────
    // 아이템 제거 (스택 분할 구조 대응) + Batch 지원
    // ──────────────────────────────────────
    public bool RemoveItem(ItemData item, int amount)
    {
        if (item == null || amount <= 0) return false;
        if (inventory == null || inventory.items == null) return false;

        int remaining = amount;

        for (int i = inventory.items.Count - 1; i >= 0 && remaining > 0; i--)
        {
            var s = inventory.items[i];
            if (s.item != item) continue;

            int take = Mathf.Min(s.count, remaining);
            s.count -= take;
            remaining -= take;

            if (s.count <= 0) inventory.items.RemoveAt(i);
            else inventory.items[i] = s;
        }

        bool success = (remaining == 0);
        if (success)
            NotifyInventoryChanged();   

        return success;
    }

    // ──────────────────────────────────────
    // 아이템 제거 (스택 분할 구조 대응)
    // ──────────────────────────────────────

    // ──────────────────────────────────────
    // Respawn API
    // ──────────────────────────────────────
    void RebuildRespawnMapFromLists()
    {
        _respawnMap = new Dictionary<string, float>();

        int n = Mathf.Min(_respawnKeys.Count, _respawnReadyTimes.Count);
        for (int i = 0; i < n; i++)
        {
            var key = _respawnKeys[i];
            if (string.IsNullOrEmpty(key)) continue;
            _respawnMap[key] = _respawnReadyTimes[i];
        }
    }
    // ──────────────────────────────────────
    // Respawn API
    // ──────────────────────────────────────


    public ItemUseFailReason CheckCanUseItem(ItemData item, int targetIndex)
    {
        if (item == null) return ItemUseFailReason.None;
        if (party == null || party.Count == 0) return ItemUseFailReason.None;

        var a = item.effectA;
        var b = item.effectB;

        var aType = a != null ? a.type : ConsumableEffectType.None;
        var bType = b != null ? b.type : ConsumableEffectType.None;

        // ───────────── HealHP 검사 ─────────────
        if (aType == ConsumableEffectType.HealHP || bType == ConsumableEffectType.HealHP)
        {
            if (item.targetPolicy == ItemTargetPolicy.SingleAlly)
            {
                var c = party[targetIndex];
                bool canHeal = (c != null && c.hp > 0 && c.hp < c.maxHp);
                if (!canHeal) return ItemUseFailReason.NoHpTarget;
            }
            else if (item.targetPolicy == ItemTargetPolicy.AllParty)
            {
                bool anyNeedHeal = false;
                foreach (var c in party)
                {
                    if (c == null) continue;
                    if (c.hp > 0 && c.hp < c.maxHp) { anyNeedHeal = true; break; }
                }
                if (!anyNeedHeal) return ItemUseFailReason.NoHpTarget;
            }
        }

        // ───────────── SecretArt 검사 ─────────────
        if (aType == ConsumableEffectType.RestoreSecretArt || bType == ConsumableEffectType.RestoreSecretArt)
        {
            if (secretArtPoints >= secretArtPointsMax)
                return ItemUseFailReason.SecretArtFull;
        }

        // ───────────── Revive 검사 ─────────────
        bool isRevive =
            (aType == ConsumableEffectType.Revive || bType == ConsumableEffectType.Revive);

        if (isRevive)
        {
            if (item.targetPolicy == ItemTargetPolicy.SingleAlly)
            {
                var c = party[targetIndex];
                bool canRevive = (c != null && c.hp <= 0);
                if (!canRevive) return ItemUseFailReason.NoDeadTarget;
            }
            else if (item.targetPolicy == ItemTargetPolicy.AllParty)
            {
                bool anyDead = false;
                foreach (var c in party)
                {
                    if (c != null && c.hp <= 0)
                    {
                        anyDead = true;
                        break;
                    }
                }

                if (!anyDead) return ItemUseFailReason.NoDeadTarget;
            }
        }

        // ───────────── Buff 중복 검사(같은 아이템만) ─────────────
        bool isSpeedBuff = (item.effectA.type == ConsumableEffectType.BuffSpeed ||
                            item.effectB.type == ConsumableEffectType.BuffSpeed);

        bool isDefBuff = (item.effectA.type == ConsumableEffectType.BuffDefense ||
                            item.effectB.type == ConsumableEffectType.BuffDefense);

        bool isMaxHpBuff = (item.effectA.type == ConsumableEffectType.BuffMaxHP ||
                            item.effectB.type == ConsumableEffectType.BuffMaxHP);

        // (빵 공격버프까지 쓰는 경우만) ItemData에 BuffAttack을 추가했다면 포함
        bool isAtkBuff = (item.effectA.type == ConsumableEffectType.BuffAttack ||
                            item.effectB.type == ConsumableEffectType.BuffAttack);

        if (isSpeedBuff || isDefBuff || isMaxHpBuff || isAtkBuff)
        {
            if (string.IsNullOrEmpty(item.id))
                return ItemUseFailReason.None;

            // SingleAlly면 선택한 1명만 검사
            if (item.targetPolicy == ItemTargetPolicy.SingleAlly)
            {
                var c = party[targetIndex];
                if (c == null) return ItemUseFailReason.None;

                if (isSpeedBuff && c.tempSpdSources != null && c.tempSpdSources.Contains(item.id)) return ItemUseFailReason.AlreadyBuffed;
                if (isDefBuff && c.tempDefSources != null && c.tempDefSources.Contains(item.id)) return ItemUseFailReason.AlreadyBuffed;
                if (isMaxHpBuff && c.tempMaxHpSources != null && c.tempMaxHpSources.Contains(item.id)) return ItemUseFailReason.AlreadyBuffed;
                if (isAtkBuff && c.tempAtkSources != null && c.tempAtkSources.Contains(item.id)) return ItemUseFailReason.AlreadyBuffed;

                return ItemUseFailReason.None;
            }

            // AllParty면 전원 검사(하나라도 이미면 막고 싶다면)
            foreach (var c in party)
            {
                if (c == null) continue;

                if (isSpeedBuff && c.tempSpdSources != null && c.tempSpdSources.Contains(item.id)) return ItemUseFailReason.AlreadyBuffed;
                if (isDefBuff && c.tempDefSources != null && c.tempDefSources.Contains(item.id)) return ItemUseFailReason.AlreadyBuffed;
                if (isMaxHpBuff && c.tempMaxHpSources != null && c.tempMaxHpSources.Contains(item.id)) return ItemUseFailReason.AlreadyBuffed;
                if (isAtkBuff && c.tempAtkSources != null && c.tempAtkSources.Contains(item.id)) return ItemUseFailReason.AlreadyBuffed;
            }
        }

        return ItemUseFailReason.None;
    }

    /// <summary>
    /// AddItem/RemoveItem이 여러 번 연속 호출될 때 OnInventoryChanged를 1번만 쏘기 위한 배치 시작.
    /// </summary>
    public void BeginInventoryBatch()
    {
        _inventoryBatchDepth++;
    }

    /// <summary>
    /// 배치 종료. 배치 중 변경이 있었으면 이 시점에 OnInventoryChanged를 1번만 호출.
    /// </summary>
    public void EndInventoryBatch()
    {
        _inventoryBatchDepth = Mathf.Max(0, _inventoryBatchDepth - 1);

        if (_inventoryBatchDepth == 0 && _inventoryBatchDirty)
        {
            _inventoryBatchDirty = false;
            OnInventoryChanged?.Invoke();
        }
    }

    /// <summary>
    /// 배치 중이면 dirty만 표시, 아니면 즉시 OnInventoryChanged.
    /// </summary>
    private void MarkInventoryDirtyOrNotify()
    {
        if (_inventoryBatchDepth > 0)
        {
            _inventoryBatchDirty = true;
        }
        else
        {
            OnInventoryChanged?.Invoke();
        }
    }

    void SyncListsFromRespawnMap()
    {
        _respawnKeys.Clear();
        _respawnReadyTimes.Clear();

        if (_respawnMap == null) return;

        foreach (var kv in _respawnMap)
        {
            _respawnKeys.Add(kv.Key);
            _respawnReadyTimes.Add(kv.Value);
        }
    }

    /// <summary>
    /// spawnId의 몬스터를 "처치됨"으로 기록하고, delay초 뒤에 다시 나오게 함
    /// </summary>
    public void MarkSpawnDefeated(string spawnId, float delay)
    {
        if (string.IsNullOrEmpty(spawnId)) return;

        if (_respawnMap == null) RebuildRespawnMapFromLists();

        float readyAt = Time.realtimeSinceStartup + Mathf.Max(0f, delay);
        _respawnMap[spawnId] = readyAt;

        SyncListsFromRespawnMap();
    }

    /// <summary>
    /// spawnId가 아직 쿨다운 중인지
    /// </summary>
    public bool IsSpawnOnCooldown(string spawnId)
    {
        if (string.IsNullOrEmpty(spawnId)) return false;

        if (_respawnMap == null) RebuildRespawnMapFromLists();

        if (!_respawnMap.TryGetValue(spawnId, out float readyAt))
            return false;

        return Time.realtimeSinceStartup < readyAt;
    }

    /// <summary>
    /// (호환용) 다른 스크립트가 IsOnRespawnCooldown(spawnId)로 호출해도 동작
    /// </summary>
    public bool IsOnRespawnCooldown(string spawnId)
    {
        return IsSpawnOnCooldown(spawnId);
    }

    /// <summary>
    /// (권장) 쿨다운 여부 + 남은 시간(out)까지 한 번에 제공
    /// ExplorationEnemyRespawn가 이 시그니처를 사용함
    /// </summary>
    public bool IsOnRespawnCooldown(string spawnId, out float remainSeconds)
    {
        remainSeconds = 0f;

        if (string.IsNullOrEmpty(spawnId))
            return false;

        if (_respawnMap == null) RebuildRespawnMapFromLists();

        if (!_respawnMap.TryGetValue(spawnId, out float readyAt))
            return false;

        float now = Time.realtimeSinceStartup;
        if (now >= readyAt)
        {
            remainSeconds = 0f;
            return false;
        }

        remainSeconds = Mathf.Max(0f, readyAt - now);
        return true;
    }

    /// <summary>
    /// 남은 쿨다운 시간(초). 쿨다운 아니면 0.
    /// </summary>
    public float GetSpawnRemaining(string spawnId)
    {
        if (string.IsNullOrEmpty(spawnId)) return 0f;

        if (_respawnMap == null) RebuildRespawnMapFromLists();

        if (!_respawnMap.TryGetValue(spawnId, out float readyAt))
            return 0f;

        return Mathf.Max(0f, readyAt - Time.realtimeSinceStartup);
    }

    /// <summary>
    /// 시간이 지난 respawn 기록 정리(선택)
    /// </summary>
    public void CleanupExpiredRespawns()
    {
        if (_respawnMap == null) RebuildRespawnMapFromLists();

        var toRemove = new List<string>();
        foreach (var kv in _respawnMap)
        {
            if (Time.realtimeSinceStartup >= kv.Value)
                toRemove.Add(kv.Key);
        }

        for (int i = 0; i < toRemove.Count; i++)
            _respawnMap.Remove(toRemove[i]);

        SyncListsFromRespawnMap();
    }

    /// <summary>
    /// (선택) 모든 리스폰 쿨다운 초기화
    /// </summary>
    public void ClearAllRespawn()
    {
        if (_respawnMap == null) RebuildRespawnMapFromLists();
        _respawnMap.Clear();
        SyncListsFromRespawnMap();
    }

    public List<ItemStack> GetItemsByCategory(ItemCategory category)
    {
        var result = new List<ItemStack>();
        if (inventory == null || inventory.items == null) return result;

        for (int i = 0; i < inventory.items.Count; i++)
        {
            var s = inventory.items[i];
            if (s.item == null) continue;
            if (s.count <= 0) continue;
            if (s.item.category != category) continue;
            result.Add(s);
        }
        return result;
    }

    public void NotifyInventoryChanged()
    {
        // 배치 지원
        MarkInventoryDirtyOrNotify();
    }

    /// <summary>
    /// 다른 UI가 열려 있으면 진입 실패.
    /// 같은 종류가 이미 열려 있으면 true(중복 방지에 도움).
    /// </summary>
    public bool TryEnterOverlay(UIOverlayKind kind)
    {
        if (kind == UIOverlayKind.None) return false;

        if (_openOverlay != UIOverlayKind.None && _openOverlay != kind)
            return false;

        _openOverlay = kind;
        return true;
    }

    /// <summary>
    /// 해당 종류가 열려 있을 때만 해제.
    /// </summary>
    public void ExitOverlay(UIOverlayKind kind)
    {
        if (_openOverlay == kind)
            _openOverlay = UIOverlayKind.None;
    }

    public bool CanSelectExpMaterial(CharacterRuntime c)
    {
        if (c == null) return false;

        // 레벨캡이면 exp 재료 못 씀
        if (!LevelingPolicy.CanGainExp(c.level, c.promotionStage))
            return false;

        int need = LevelingPolicy.GetNeedExpForNextLevel(c.level, c.promotionStage);

        // need가 1이라도 남아있으면(= exp < need) 선택 가능
        // exp가 need 이상이면(= 꽉 참) 선택 불가
        return c.exp < need;
    }

    public bool TryConsumeExpMaterial(int partyIndex, int materialExpValue)
    {
        if (materialExpValue <= 0) return false;
        if (party == null || partyIndex < 0 || partyIndex >= party.Count) return false;

        var c = party[partyIndex];
        if (c == null) return false;

        // 캡이면 불가
        if (!LevelingPolicy.CanGainExp(c.level, c.promotionStage))
            return false;

        // exp가 이미 꽉 찼으면(현재 레벨 기준) 재료 사용 불가
        int need = LevelingPolicy.GetNeedExpForNextLevel(c.level, c.promotionStage);
        if (c.exp >= need) return false;

        // overshoot 허용: 남은치보다 커도 사용 가능
        return TryAddExpToCharacter(partyIndex, materialExpValue);
    }

    /// <summary>
    /// 파티 캐릭터에게 exp 추가 + 레벨업 처리 + 스탯 재계산(중요!)
    /// </summary>
    public bool TryAddExpToCharacter(int partyIndex, int addExp)
    {
        if (addExp <= 0) return false;
        if (party == null || partyIndex < 0 || partyIndex >= party.Count) return false;

        var c = party[partyIndex];
        if (c == null) return false;

        // 캡이면 불가
        if (!LevelingPolicy.CanGainExp(c.level, c.promotionStage))
            return false;

        // 성장 전 상태 저장
        int beforeLevel = c.level;
        int beforeHp = c.hp;
        int beforeMaxHp = Mathf.Max(1, c.maxHp);
        bool wasAlive = beforeHp > 0;

        // 실제 레벨/exp 갱신
        LevelingPolicy.ApplyExpAndLevelUp(ref c.level, ref c.exp, addExp, c.promotionStage);

        // 스탯 재계산
        c.RecalculateStats(keepHpRatio: false);

        int afterMaxHp = Mathf.Max(1, c.maxHp);
        int gainedMaxHp = Mathf.Max(0, afterMaxHp - beforeMaxHp);

        if (wasAlive)
        {
            // 살아있는 캐릭터만 오른 maxHp 수치만큼 회복
            c.hp = Mathf.Clamp(beforeHp + gainedMaxHp, 0, c.maxHp);
        }
        else
        {
            // 죽어있던 캐릭터는 절대 부활하지 않음
            c.hp = 0;
        }

        QuestManager.I?.RefreshAutoCompleteConditions();

        return true;
    }

    public void PreparePartyForBattleEntry()
    {
        if (party == null) return;

        for (int i = 0; i < party.Count; i++)
        {
            var c = party[i];
            if (c == null) continue;

            // 1) 현재 상태 스냅샷
            int hpBefore = c.hp;
            int maxBefore = Mathf.Max(1, c.maxHp);

            // "탐험에서 풀피였다" 판정(오차/버그 대비로 >=)
            bool wasFull = (hpBefore >= maxBefore);

            // 2) 스탯 재계산은 '비율 유지'가 아니라, 일단 확정 계산
            //    (여기서 keepHpRatio=true를 쓰면, hp01이 1이 아닐 때 줄어들 수 있음)
            c.RecalculateStats(keepHpRatio: false);

            // 3) HP 정책 적용
            if (wasFull)
            {
                // 탐험에서 풀피였으면 전투도 풀피 강제
                c.hp = c.maxHp;
            }
            else
            {
                // 풀피가 아니었으면 "절대 HP 유지" (새 maxHp에 맞춰 clamp)
                c.hp = Mathf.Clamp(hpBefore, 0, c.maxHp);
            }

            // (선택) 진입 진단 로그
            Debug.Log($"[PreparePartyForBattleEntry] {c.data?.name} full={wasFull} hp {hpBefore}/{maxBefore} -> {c.hp}/{c.maxHp}");
        }
    }

    private void FillDefeatStateToSaveData(SaveData data)
    {
        if (data == null) return;

        if (_respawnMap == null)
            RebuildRespawnMapFromLists();

        if (_uniqueDefeatedSet == null)
            RebuildUniqueSetFromList();

        data.defeatedSpawns.Clear();
        data.defeatedUniqueIds.Clear();

        foreach (var kv in _respawnMap)
        {
            float remain = Mathf.Max(0f, kv.Value - Time.realtimeSinceStartup);

            // 이미 만료된 건 굳이 저장 안 함
            if (remain <= 0f) continue;

            data.defeatedSpawns.Add(new DefeatedSpawnSaveData
            {
                spawnId = kv.Key,
                remainingSeconds = remain
            });
        }

        foreach (var id in _uniqueDefeatedSet)
        {
            data.defeatedUniqueIds.Add(id);
        }
    }

    private void ApplyDefeatStateFromSaveData(SaveData data)
    {
        if (data == null) return;

        // Respawn map 초기화
        if (_respawnMap == null)
            RebuildRespawnMapFromLists();

        _respawnMap.Clear();

        for (int i = 0; i < data.defeatedSpawns.Count; i++)
        {
            var s = data.defeatedSpawns[i];
            if (string.IsNullOrEmpty(s.spawnId)) continue;

            float readyAt = Time.realtimeSinceStartup + Mathf.Max(0f, s.remainingSeconds);
            _respawnMap[s.spawnId] = readyAt;
        }

        SyncListsFromRespawnMap();

        // Unique defeated
        _uniqueDefeatedKeys.Clear();
        for (int i = 0; i < data.defeatedUniqueIds.Count; i++)
        {
            var id = data.defeatedUniqueIds[i];
            if (!string.IsNullOrEmpty(id))
                _uniqueDefeatedKeys.Add(id);
        }

        RebuildUniqueSetFromList();
    }
}

// ──────────────────────────────────────
// Runtime Models
// CharacterData가 정적 설계 정보라면,
// 아래 클래스들은 실제 플레이 중 변하는 상태를 저장하는 런타임 모델이다.
// ──────────────────────────────────────

/// <summary>
/// 실제 플레이 중 변하는 캐릭터 상태를 저장하는 런타임 데이터.
/// CharacterData가 "정적 설계 데이터"라면,
/// CharacterRuntime은 레벨, HP, 버프, 승급 상태 같은 "현재 상태"를 보관한다.
/// </summary>
[System.Serializable]
public class CharacterRuntime
{
    /// <summary>
    /// 이 런타임이 참조하는 캐릭터 정적 데이터.
    /// 기본 스탯, 성장치, 스킬 정보는 CharacterData에서 가져온다.
    /// </summary>
    public CharacterData data;

    // 성장
    /// <summary>
    /// 현재 캐릭터 레벨.
    /// </summary>
    public int level;

    /// <summary>
    /// 현재 레벨 구간 내 경험치.
    /// 레벨업 시 필요한 경험치를 넘기면 다음 레벨로 이월된다.
    /// </summary>
    public int exp;

    /// <summary>
    /// 현재 승급 단계. 범위는 0~4.
    /// </summary>
    public int promotionStage;

    /// <summary>
    /// 현재 HP.
    /// </summary>
    public int hp;

    /// <summary>
    /// 현재 최대 HP.
    /// 영구 성장 및 임시 버프를 모두 반영한 최종 최대 체력이다.
    /// </summary>
    public int maxHp;

    /// <summary>
    /// 캐릭터 개인 SP.
    /// 현재 프로젝트에서는 주로 탐험/캐릭터 상태용으로 유지한다.
    /// </summary>
    public int sp;

    // ★ 최종 전투 스탯(= UI가 표시할 값)
    /// <summary>
    /// 최종 공격력.
    /// 기본 스탯 + 레벨 성장 + 승급 보너스 + 영구 성장 + 임시 버프가 모두 반영된 값이다.
    /// </summary>
    public int atk;

    /// <summary>
    /// 최종 방어력.
    /// </summary>
    public int def;

    /// <summary>
    /// 최종 속도.
    /// 턴 순서 계산에도 사용된다.
    /// </summary>
    public int spd;

    // 영구 성장(레벨업/스탯트리)
    /// <summary>
    /// 영구적으로 증가한 HP 보너스.
    /// 레벨업, 성장 시스템, 스탯 강화 등의 결과를 누적 저장한다.
    /// </summary>
    public int permHpAdd;

    public int permAtkAdd;
    public int permDefAdd;
    public int permSpdAdd;

    // 전투/아이템 임시 버프
    /// <summary>
    /// 전투 중 또는 소비 아이템 사용으로 얻는 임시 공격력 증가량.
    /// </summary>
    public int tempAtkAdd;

    /// <summary>
    /// 전투 중 또는 소비 아이템 사용으로 얻는 임시 속도 증가량.
    /// </summary>
    public int tempSpdAdd;

    /// <summary>
    /// 전투 중 또는 소비 아이템 사용으로 얻는 임시 방어력 증가량.
    /// </summary>
    public int tempDefAdd;

    /// <summary>
    /// 전투 중 또는 소비 아이템 사용으로 얻는 임시 최대 HP 증가량.
    /// </summary>
    public int tempMaxHpAdd;

    /// <summary>
    /// 동일 아이템/효과의 중복 적용을 막기 위한 임시 공격 버프 출처 집합.
    /// </summary>
    [System.NonSerialized] public HashSet<string> tempAtkSources;

    /// <summary>
    /// 동일 아이템/효과의 중복 적용을 막기 위한 임시 속도 버프 출처 집합.
    /// </summary>
    [System.NonSerialized] public HashSet<string> tempSpdSources;

    /// <summary>
    /// 동일 아이템/효과의 중복 적용을 막기 위한 임시 방어 버프 출처 집합.
    /// </summary>
    [System.NonSerialized] public HashSet<string> tempDefSources;

    /// <summary>
    /// 동일 아이템/효과의 중복 적용을 막기 위한 임시 최대 HP 버프 출처 집합.
    /// </summary>
    [System.NonSerialized] public HashSet<string> tempMaxHpSources;

    [Header("Exploration Flags")]
    /// <summary>
    /// 탐험에서 Secret Art를 준비한 상태인지 여부.
    /// true이면 전투 시작 시 1회성 Secret Art 효과가 적용된다.
    /// </summary>
    public bool secretArtReady;

    /// <summary>
    /// 런타임 객체 생성자.
    /// 여기서는 "새 게임 초기화"를 하지 않고,
    /// 기본 참조 연결 및 스탯 계산만 수행한다.
    /// 새 게임 초기화는 InitForNewGame()에서 별도로 처리한다.
    /// </summary>
    public CharacterRuntime(CharacterData d, int level)
    {
        data = d;
        this.level = Mathf.Max(1, level);

        // 생성자는 "리셋 금지"
        // exp / promotionStage / perm / temp는
        // - 새 게임이면 InitForNewGame()
        // - 로드/복제면 외부에서 값 세팅 후 RecalculateStats()
        // 로 흐름을 강제한다.

        RecalculateStats(keepHpRatio: false);

        // 생성 직후 비정상 값 방지용 최소 보정
        if (maxHp <= 0) maxHp = 1;
        if (hp < 0) hp = 0;
        if (hp > maxHp) hp = maxHp;
    }

    /// <summary>
    /// 현재 레벨, 승급, 영구 성장, 임시 버프를 기준으로
    /// 최종 스탯(HP/ATK/DEF/SPD)을 다시 계산한다.
    /// </summary>
    /// <param name="keepHpRatio">
    /// true이면 maxHp 변경 전후의 HP 비율을 유지한다.
    /// false이면 현재 hp 값을 새 maxHp 범위 안으로만 보정한다.
    /// </param>
    public void RecalculateStats(bool keepHpRatio)
    {
        if (data == null)
        {
            maxHp = 100;
            hp = Mathf.Clamp(hp, 0, maxHp);

            atk = 10;
            def = 10;
            spd = 100;
            return;
        }

        // 현재 HP 비율 유지 옵션
        float hp01 = 1f;
        if (keepHpRatio && maxHp > 0)
            hp01 = hp / (float)maxHp;

        int lvl = Mathf.Max(1, level);

        // 1) 베이스 + 레벨 성장
        int hpBase = data.baseHP + data.hpPerLevel * (lvl - 1);
        int atkBase = data.baseATK + data.atkPerLevel * (lvl - 1);
        int defBase = data.baseDEF + data.defPerLevel * (lvl - 1);
        int spdBase = data.baseSPD + data.spdPerLevel * (lvl - 1);

        // 2) 승급 보너스(퍼센트) 누적
        int p = Mathf.Clamp(promotionStage, 0, 4);

        float hpMul = 1f;
        float atkMul = 1f;
        float defMul = 1f;
        float spdMul = 1f;

        for (int i = 0; i < p; i++)
        {
            hpMul *= (1f + data.promoHpPercent);
            atkMul *= (1f + data.promoAtkPercent);
            defMul *= (1f + data.promoDefPercent);
            spdMul *= (1f + data.promoSpdPercent);
        }

        int hpAfterPromo = Mathf.RoundToInt(hpBase * hpMul);
        int atkAfterPromo = Mathf.RoundToInt(atkBase * atkMul);
        int defAfterPromo = Mathf.RoundToInt(defBase * defMul);
        int spdAfterPromo = Mathf.RoundToInt(spdBase * spdMul);

        // 3) 영구 성장(perm) + 임시 버프(temp)
        maxHp = hpAfterPromo + permHpAdd + tempMaxHpAdd;
        atk = atkAfterPromo + permAtkAdd + tempAtkAdd;
        def = defAfterPromo + permDefAdd + tempDefAdd;
        spd = spdAfterPromo + permSpdAdd + tempSpdAdd;

        // 4) HP 적용
        if (keepHpRatio)
            hp = Mathf.Clamp(Mathf.RoundToInt(maxHp * hp01), 0, maxHp);
        else
            hp = Mathf.Clamp(hp, 0, maxHp);
    }

    /// <summary>
    /// 새 게임 시작 시 캐릭터 런타임 상태를 초기화한다.
    /// 레벨 1 기준 상태로 맞추고, HP는 풀피로 설정한다.
    /// </summary>
    public void InitForNewGame()
    {
        exp = 0;
        promotionStage = 0;

        permHpAdd = permAtkAdd = permDefAdd = permSpdAdd = 0;
        tempMaxHpAdd = tempAtkAdd = tempDefAdd = tempSpdAdd = 0;

        RecalculateStats(keepHpRatio: false);
        hp = maxHp;   // 새 게임은 풀피
        sp = 0;
    }
}

/// <summary>
/// 인벤토리의 런타임 상태.
/// 실제 아이템 스택 목록을 보관한다.
/// </summary>
[System.Serializable]
public class InventoryRuntime
{
    /// <summary>
    /// 현재 인벤토리에 들어있는 아이템 스택 목록.
    /// 같은 아이템이라도 스택 제한에 따라 여러 칸으로 나뉠 수 있다.
    /// </summary>
    public List<ItemStack> items = new();
}

/// <summary>
/// 인벤토리 안에서 하나의 아이템 묶음을 나타내는 스택 데이터.
/// </summary>
[System.Serializable]
public class ItemStack
{
    /// <summary>
    /// 스택이 참조하는 아이템 정적 데이터.
    /// </summary>
    public ItemData item;

    /// <summary>
    /// 현재 스택에 들어있는 개수.
    /// </summary>
    public int count;
}
