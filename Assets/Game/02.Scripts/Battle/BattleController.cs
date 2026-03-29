using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BattleController : MonoBehaviour
{
    [Header("UI")]
    public BattleHud hud;
    public SkillSelectUI skillUI;

    [Header("Camera")]
    public BattleCameraController cameraController;

    [Header("Skill Point Policy (Shared)")]
    public int battleSPStart = 3;
    public int battleSPMax = 5;
    public int basicAttackGainSP = 1;

    [Header("Skill Cinematic (Tribi Heal Only)")]
    public Transform partyCamPivot;  // Battle 씬에서 지정: PartyCamPivot
    public Transform partyCamLook;   // Battle 씬에서 지정: PartyCamLook
    [Tooltip("힐 컷에서 잠깐 유지하는 시간")]
    public float healCinematicHold = 0.65f;

    // ally runtime -> GameContext.party index 매핑
    private readonly Dictionary<BattleActorRuntime, int> _allyPartyIndex = new();

    [Header("Skill Extra Hold (Quick Fix)")]
    [Tooltip("StellarWitch(AoE) 스킬 연출을 이 시간만큼 추가로 유지")]
    public float stellarWitchAoEExtraHold = 4.0f;

    [Header("Enemy Attack Debug")]
    public bool logEnemyHitEventTimeout = false;

    [Header("Enemy Damage Policy")]
    [Tooltip("전체공격(Attack01/Throw 등) 데미지 배율")]
    public float enemyAoEDamageMul = 2.0f;

    [Header("Dragon Boss Policy")]
    public float dragonMaxMoveDistance = 0.0f;      // 드래곤은 제자리면 0 추천 (혹은 0.5)
    public float dragonBreathHitDelay = 0.55f;      // 데미지 타이밍 늦추기(클립에 맞게)
    public float dragonBreathTurnLockExtraHold = 0.35f; // 애니 끝까지 보여주기용 추가 홀드


    // Throw release tracking (unscaled time 기준)
    bool _throwReleaseSeen;
    int _throwReleaseToken;
    float _throwExpectedImpactTime; // unscaledTime

    [Header("Optional Camera FX")]
    public CameraShaker cameraShaker;

    [Header("Spawn Points")]
    public Transform[] allySpawnPoints;
    public Transform[] enemySpawnPoints;

    [Header("Enemy AI")]
    public bool enemyPickRandomVictim = true;

    [Header("Drop MVP")]
    [Tooltip("드랍 재현성을 위해 seed를 고정하고 싶으면 값 입력. 0이면 랜덤 seed.")]
    public int dropSeedOverride = 0;

    private bool _dropsGranted = false; // 드랍 중복 지급 방지


    [Header("MiniBoss Move Policy")]
    public float miniBossMaxMoveDistance = 3.0f;

    float _maxMoveDistanceBackup;
    bool _maxMoveDistanceBackedUp;

    [Header("Attack Motion (Approach Target)")]
    [Tooltip("타겟과 이 거리만큼 남겨두고 멈춤")]
    public float stopDistance = 1.1f;

    [Tooltip("최소 전진거리(너무 가까우면 0이 되어 공격이 안 보이는 것 방지)")]
    public float minMoveDistance = 0.4f;

    [Tooltip("최대 전진거리(너무 멀리 달려가는 것 제한)")]
    public float maxMoveDistance = 3.2f;

    [Tooltip("전진 시간")]
    public float attackMoveTime = 0.2f;

    [Tooltip("복귀 시간")]
    public float attackReturnTime = 0.15f;

    [Tooltip("공격 위치에 도착 후 '서서 공격'이 보이게 대기")]
    public float beforeAttackPause = 0.15f;

    [Tooltip("타격/피격 직후 살짝 멈춤")]
    public float afterAttackPause = 0.1f;

    [Header("Attack Timing (Animation Sync)")]
    [Tooltip("Attack 트리거 이후, 실제 타격(Hit/Die 트리거)을 주기까지 기다리는 시간(클립 길이에 맞춰 조절)")]
    public float hitTimingDelay = 0.1f;

    [Tooltip("Attack 트리거 이후, 복귀를 시작하기 전 추가 대기(공격 애니 끝까지 보여주고 싶으면 늘림)")]
    public float afterAttackAnimHold = 0.15f;

    [Header("Critical Settings")]
    [Range(0f, 1f)] public float playerCritChance = 0.25f;
    [Range(0f, 1f)] public float enemyCritChance = 0.15f;
    public float critDamageMul = 1.5f;

    [Header("StarRail-ish Mini Cinematic (Optional)")]
    public float miniZoomZDelta = 0.5f;
    public float miniZoomDuration = 0.25f;
    [Range(0.5f, 1f)] public float miniSlowTimeScale = 0.88f;
    public float miniSlowDuration = 0.25f;
    public float miniShakeIntensity = 0.12f;
    public float miniShakeDuration = 0.08f;

    [Header("Golem (MiniBoss) Pattern")]
    [Range(0f, 1f)] public float golemThrowChance = 0.35f;  // 던지기 확률
    public float golemThrowPowerMul = 1.2f;                 // 던지기 데미지 배율(기본공격 대비)

    // 죽는 연출 중인 적은 렌더러를 꺼버리지 않도록 보호
    private readonly HashSet<BattleActorRuntime> _dyingEnemies = new();
    public float enemyDieVisibleTime = 1.2f; // Die 클립 길이에 맞게 조절


    [Header("Visibility Policy")]
    public bool hideOtherAlliesDuringTurns = true;

    [Header("Camera Freeze Policy")]
    public bool freezeCameraDuringAttackMotion = true;
    public bool keepCameraIfSameVictim = true;

    [Header("Animation (Safe Triggers)")]
    public bool forceDisableRootMotion = true;
    public bool forceSnapToBasePosition = true;

    [Header("Target Marker Visual")]
    [Range(0f, 1f)]
    public float targetMarkerAlpha = 0.5f; // 0.5 = 50% 투명

    public string animTriggerAttack = "Attack";
    public string animTriggerHit = "Hit";
    public string animTriggerDie = "Die";

    [Header("End Battle Fade (Style B)")]
    public CanvasGroup endFadeGroup;          // Battle 씬 Canvas 아래에 CanvasGroup 하나 만들어서 연결
    public float endFadeOutTime = 0.28f;      // 0.25~0.35 추천
    public float endFadeHoldTime = 0.10f;     // 0~0.2 추천

    // BattleController 멤버에 추가
    private bool _endingBattle = false;

    public bool ignoreMissingAnimatorParams = true;

    // === Enemy hit timing gate ===
    bool _enemyHitArmed;
    bool _enemyHitFired;
    System.Action _enemyHitAction;
    int _enemyHitToken;            // 중복/잔류 이벤트 방지용
    int _armedToken;

    // ─────────────────────────────────────────────
    // Turn / Cinematic Lock (핵심: 연출 중 다음 턴 진행 금지)
    // ─────────────────────────────────────────────
    private int cinematicLock = 0;
    private bool IsCinematicLocked => cinematicLock > 0;

    void PushCinematicLock() => cinematicLock++;
    void PopCinematicLock() => cinematicLock = Mathf.Max(0, cinematicLock - 1);

    // ─────────────────────────────────────────────
    // 준보스 전체공격 - Throw 고정 카메라
    // ─────────────────────────────────────────────
    [Header("Golem Throw Camera (Fixed Pose)")]
    public bool useFixedThrowCamera = true;
    public Transform throwCamPose;   // ThrowCamPose (3번째 스샷 포즈)
    public Transform throwCamLook;   // ThrowCamLook (옵션)
    public bool throwCamUseLookAt = false; // ★ 추가: LookAt으로 rotation 덮을지 여부

    // Throw cam backup
    Vector3 _camPosBackup;
    Quaternion _camRotBackup;
    bool _camHasBackup;
    bool _camControllerWasEnabled;
    bool _throwCamActive;

    // ─────────────────────────────────────────────
    // 보스 패턴용
    // ─────────────────────────────────────────────

    [Header("Dragon Boss Pattern")]
    [Range(0f, 1f)] public float dragonDefendChance = 0.25f;   // 맞을 때 방어 확률
    [Range(0f, 1f)] public float dragonDefendDamageMul = 0.30f; // 70% 감소 => 30%만 받음
    public float dragonDefendAnimHold = 0.15f;                 // 방어 포즈가 '보이게' 살짝 홀드

    [Range(0f, 1f)] public float dragonScreamChance = 0.25f;   // 드래곤 턴에 스크림 확률(공격 대신)
    public float dragonScreamMulMin = 1.5f;
    public float dragonScreamMulMax = 2.0f;
    public float dragonScreamHold = 0.55f;                     // 스크림 연출 홀드(클립 길이에 맞게)

    [Tooltip("플레이어 공격 전에 방어 자세를 '먼저' 보이게 하는 시간")]
    public float dragonDefendEnterHold = 0.10f; // ← 이 줄이 빠져 있었음

    // 내부 상태(한 번의 공격에서만 사용)
    bool _dragonDefendedThisAction = false;
    // Dragon runtime state
    float _dragonNextAttackMul = 1f; // 스크림 버프 (다음 공격 1회에만 적용)

    // ─────────────────────────────────────────────
    // Runtime
    // ─────────────────────────────────────────────
    private readonly List<BattleActorRuntime> allies = new();
    private readonly List<(BattleActorRuntime actor, EnemyData src)> enemies = new();
    private Queue<BattleActorRuntime> turnQueue = new();
    private readonly Dictionary<BattleActorRuntime, Transform> actorViews = new();

    private readonly Dictionary<BattleActorRuntime, Vector3> actorBasePositions = new();
    private readonly Dictionary<BattleActorRuntime, Quaternion> actorBaseRotations = new();

    private Transform enemyCenterAnchor;

    private Transform camPivotAnchor;
    private Transform camLookAnchor;
    private bool cameraFrozen;

    private BattleActorRuntime lastVictim;
    private bool secretArtApplied;

    [Header("Hit Timing Policy")]
    public bool useAnimationEventForHit = true;

    [Tooltip("애니 이벤트가 없을 때, 최대 기다릴 시간(초). 넘기면 hitTimingDelay로 fallback")]
    public float hitEventTimeout = 0.35f;

    // ─────────────────────────────────────────────
    // Target
    // ─────────────────────────────────────────────
    [Header("Target Select (Mouse)")]
    public Sprite targetMarkerSprite;
    [Tooltip("타겟 마커 크기(월드 단위)")]
    public float targetMarkerScale = 0.6f;
    [Tooltip("Raycast에 사용할 레이어 마스크(비우면 Everything)")]
    public LayerMask enemyClickMask = ~0;
    [Tooltip("마커를 몬스터 중앙에서 위/아래로 약간 이동")]
    public Vector3 targetMarkerOffset = Vector3.zero;

    [Header("Target Marker UI (Recommended)")]
    public Image targetMarkerUI;              // Canvas 아래 TargetMarkerUI 연결
    public Vector2 targetMarkerUIOffset = Vector2.zero; // 화면에서 살짝 이동(원하면)
    public float targetMarkerUIScale = 1.0f;  // UI 스케일 배율

    private BattleActorRuntime selectedEnemy;                 // 현재 선택된 타겟(유지)
    private GameObject targetMarkerGO;                        // 마커 오브젝트
    private SpriteRenderer targetMarkerSR;                    // 마커 렌더러
    private readonly Dictionary<Transform, BattleActorRuntime> viewToActor = new(); // 클릭 hit -> actor 찾기

    // 마커 표시 정책
    private bool showTargetMarker = true;      // 현재 프레임에서 마커를 보여줄지
    private bool isPlayerSelecting = false;    // 플레이어 턴에서 선택 단계인지

    // TargetAnchor cache (몬스터별 1회만 찾기)
    private readonly Dictionary<BattleActorRuntime, Transform> _targetAnchorCache = new();

    // ─────────────────────────────────────────────
    // Skill Cinematic Guard (카메라 SoftFollowTick 차단용)
    // ─────────────────────────────────────────────
    private bool blockSoftFollowTick = false;

    public static BattleController Instance { get; private set; }

    // BattleController 클래스 멤버에 추가
    private string _spawnId;
    private float _respawnDelay;

    // ─────────────────────────────────────────────
    // Unity
    // ─────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    /// <summary>
    /// 전투 시작 시 아군/적 런타임을 생성하고,
    /// 카메라/타겟/턴 큐/SP/Secret Art 등을 초기화한 뒤
    /// 전투 루프를 시작한다.
    /// </summary>
    void Start()
    {
#if UNITY_EDITOR
        Debug.Log($"[Battle] partyCount={GameContext.I?.party?.Count}");
#endif

        if (GameContext.I == null)
        {
            Debug.LogError("[BattleController] GameContext가 없습니다. Title로 되돌립니다.");
            SceneManager.LoadScene("Title");
            return;
        }

        if (GameContext.I.party == null || GameContext.I.party.Count == 0)
        {
            SceneManager.LoadScene("Title");
            return;
        }

        EnsureCameraAnchors();

        // 1) Allies build
        allies.Clear();
        _allyPartyIndex.Clear();

        for (int i = 0; i < GameContext.I.party.Count; i++)
        {
            var cr = GameContext.I.party[i];
            if (cr == null || cr.data == null) continue;

            // BattleController.cs : Start() 안 Allies build 루프 내부

            var br = new BattleActorRuntime(cr.data, enemy: false);

            // 레벨/스탯/HP/SP를 전부 GameContext(CharacterRuntime)에서 복사
            br.level = cr.level;

            br.maxHp = Mathf.Max(1, cr.maxHp);
            br.hp = Mathf.Clamp(cr.hp, 0, br.maxHp);
            br.sp = Mathf.Max(0, cr.sp);

            // 핵심: 전투 스탯도 복사
            br.atk = cr.atk;
            br.def = cr.def;
            br.spd = cr.spd;

            allies.Add(br);
            _allyPartyIndex[br] = i;
        }

        if (allies.Count == 0)
        {
            ReturnToExploration("No valid allies");
            return;
        }

        // 2) Enemies build
        enemies.Clear();

        EncounterData enc = TempBattlePayload.encounter != null
            ? TempBattlePayload.encounter
            : (GameContext.I != null ? GameContext.I.currentEncounter : null);

        EnemyData[] set = TempBattlePayload.enemySet;

        _spawnId = TempBattlePayload.spawnId;
        _respawnDelay = TempBattlePayload.respawnDelay;

        TempBattlePayload.encounter = null;
        TempBattlePayload.enemySet = null;
        TempBattlePayload.spawnId = null;
        TempBattlePayload.respawnDelay = 0f;

        if (enc != null)
        {
            var rolled = RollEnemiesFromEncounter(enc);
            for (int i = 0; i < rolled.Count; i++)
            {
                var ed = rolled[i];
                if (ed == null) continue;

                var act = EnemyActorFactory.CreateEnemy(ed);
                if (act != null) enemies.Add((act, ed));
            }
        }
        else if (set != null && set.Length > 0)
        {
            foreach (var ed in set)
            {
                if (ed == null) continue;
                var act = EnemyActorFactory.CreateEnemy(ed);
                if (act != null) enemies.Add((act, ed));
            }
        }

        if (enemies.Count == 0)
        {
            ReturnToExploration("No enemy data (payload empty)");
            return;
        }

        // 여기 추가: 전투 BGM 결정/재생
        PlayBattleBgmForCurrentEncounter();

        // 3) Spawn visuals
        actorViews.Clear();
        actorBasePositions.Clear();
        actorBaseRotations.Clear();

        SpawnAlliesVisual();
        SpawnEnemiesVisual();

        BuildViewReverseLookup();
        EnsureTargetMarker();
        AutoPickTargetIfNeeded();

        showTargetMarker = false;
        UpdateTargetMarker();

        // 4) Enemy center anchor
        CreateEnemyCenterAnchor();
        UpdateEnemyCenterAnchor();

        // 5) Battle SP init
        GameContext.I.ResetBattleSkillPoints(battleSPStart, battleSPMax);

        // 6) Pre-effect hook (SecretArt)
        ApplySecretArtAtBattleStartOnce();

        // 7) Start loop
        RebuildTurnQueue();
        StartCoroutine(BattleLoop());

        if (endFadeGroup != null)
        {
            endFadeGroup.alpha = 0f;
            endFadeGroup.interactable = false;
            endFadeGroup.blocksRaycasts = false;
            endFadeGroup.gameObject.SetActive(true);
        }
    }

    void Update()
    {
        // Battle 씬에서는 커서 항상 자유
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        HandleMouseTargetSelect();
    }

    void LateUpdate()
    {
        if (enemyCenterAnchor != null)
            UpdateEnemyCenterAnchor();

        // 중요: controller가 꺼져 있으면 tick도 호출하지 않게 해야 "고정 포즈"가 유지됨
        if (cameraController != null && cameraController.enabled && !blockSoftFollowTick)
            cameraController.SoftFollowTick();

        UpdateTargetMarker();
    }

    public void ArmEnemyHit(System.Action onHit)
    {
        _enemyHitToken++;
        _armedToken = _enemyHitToken;

        _enemyHitArmed = true;
        _enemyHitFired = false;
        _enemyHitAction = onHit;
    }

    public void NotifyEnemyAttackHit(int token)
    {
        //Debug.Log($"[NotifyEnemyAttackHit] token={token}, armed={_enemyHitArmed}, fired={_enemyHitFired}, armedToken={_armedToken}");
        if (!_enemyHitArmed) return;
        if (token != _armedToken) return;
        if (_enemyHitFired) return;

        _enemyHitFired = true;
        _enemyHitAction?.Invoke();
    }

    public bool IsEnemyHitFired => _enemyHitFired;
    public int CurrentEnemyHitToken => _armedToken;

    public void ClearEnemyHit()
    {
        _enemyHitArmed = false;
        _enemyHitFired = false;
        _enemyHitAction = null;
    }

    Transform FindTargetAnchor(Transform enemyRoot)
    {
        if (enemyRoot == null) return null;

        // selectedEnemy 기준 캐시 (같은 타겟이면 매 프레임 FindChildRecursive 안 하게)
        if (selectedEnemy != null &&
            _targetAnchorCache.TryGetValue(selectedEnemy, out var cached) &&
            cached != null)
            return cached;

        // 재귀로 찾기 (BattleController에 이미 FindChildRecursive가 있음)
        var found = FindChildRecursive(enemyRoot, "TargetAnchor");

        if (selectedEnemy != null)
            _targetAnchorCache[selectedEnemy] = found; // found가 null이어도 캐시(=다음 프레임 재탐색 방지)

        return found;
    }


    void ReturnToExploration(string reason)
    {
        Debug.LogWarning($"[BattleController] {reason} -> return to Exploration");

        // 전투 중 현재 HP/SP 상태를 탐험 파티에 반영
        SyncBattlePartyStateToGameContext();

        // 전투 임시 버프 정리
        if (GameContext.I != null)
            GameContext.I.ClearBattleTemporaryBuffs();

        string next = (GameContext.I != null)
            ? GameContext.I.returnExplorationSceneName
            : "Exploration";

        if (SceneFader.I != null)
            SceneFader.I.LoadSceneWithFade(next);
        else
            SceneManager.LoadScene(next);
    }

    public void ForfeitBattle()
    {
        if (_endingBattle)
            return;

        // 전투 포기: 튜토리얼 완료 처리 없이 현재 상태로 탐험 복귀
        ReturnToExploration("Battle Forfeit");
    }

    void SetSelectedEnemy(BattleActorRuntime enemy)
    {
        if (enemy == null || !enemy.isEnemy || enemy.IsDead)
            return;

        selectedEnemy = enemy;
        UpdateTargetMarker(forceOn: true);

        if (hud && enemy.data != null)
            hud.AppendLog($"[타겟] {enemy.data.displayName} 선택");
    }

    void AutoPickTargetIfNeeded()
    {
        if (selectedEnemy != null && !selectedEnemy.IsDead) return;

        selectedEnemy = enemies.Select(e => e.actor).FirstOrDefault(x => x != null && !x.IsDead);
        UpdateTargetMarker(forceOn: selectedEnemy != null);
    }

    void UpdateTargetMarker(bool forceOn = false)
    {
        if (targetMarkerUI == null) return;

        if (!showTargetMarker && !forceOn)
        {
            targetMarkerUI.gameObject.SetActive(false);
            return;
        }

        if (selectedEnemy == null || selectedEnemy.IsDead)
        {
            targetMarkerUI.gameObject.SetActive(false);
            return;
        }

        if (!actorViews.TryGetValue(selectedEnemy, out var tf) || tf == null)
        {
            targetMarkerUI.gameObject.SetActive(false);
            return;
        }

        var cam = Camera.main;
        if (cam == null)
        {
            targetMarkerUI.gameObject.SetActive(false);
            return;
        }

        // 1. Anchor 기준 월드 좌표 (Offset 최소화)
        Transform anchor = FindTargetAnchor(tf);
        Vector3 worldPos = anchor != null
            ? anchor.position
            : GetActorVisualCenter(tf);

        // 2. 월드 → 스크린
        Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

        if (screenPos.z <= 0f)
        {
            targetMarkerUI.gameObject.SetActive(false);
            return;
        }

        // 3. UI 위치 (Overlay Canvas 기준)
        targetMarkerUI.rectTransform.position = screenPos;

        if (!targetMarkerUI.gameObject.activeSelf)
            targetMarkerUI.gameObject.SetActive(true);
    }

    Vector3 GetActorVisualCenter(Transform root)
    {
        var rends = root.GetComponentsInChildren<Renderer>(true);
        if (rends == null || rends.Length == 0)
            return root.position;

        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++)
            b.Encapsulate(rends[i].bounds);

        return b.center;
    }

    // ─────────────────────────────────────────────
    // Camera helpers
    // ─────────────────────────────────────────────
    void EnsureCameraAnchors()
    {
        if (camPivotAnchor == null)
        {
            var go = new GameObject("CamPivotAnchor");
            camPivotAnchor = go.transform;
        }

        if (camLookAnchor == null)
        {
            var go = new GameObject("CamLookAnchor");
            camLookAnchor = go.transform;
        }

        cameraFrozen = false;
    }

    void CameraFollow(Transform pivot, Transform lookAt, bool instant = false)
    {
        cameraFrozen = false;
        if (cameraController != null && pivot != null && lookAt != null)
            cameraController.FocusDuel(pivot, lookAt, instant);
    }

    void CameraFreezeAt(Transform pivotSource, Transform lookSource, bool instant = false)
    {
        if (cameraController == null || pivotSource == null || lookSource == null) return;

        camPivotAnchor.position = pivotSource.position;
        camLookAnchor.position = lookSource.position;
        cameraFrozen = true;

        cameraController.FocusDuel(camPivotAnchor, camLookAnchor, instant, followLookAt: true);
    }

    bool IsDragonBoss(BattleActorRuntime enemyActor)
    {
        if (enemyActor == null || !enemyActor.isEnemy) return false;

        if (!TryGetEnemySrc(enemyActor, out var src) || src == null) return false;

        bool nameIsDragon = !string.IsNullOrEmpty(src.displayName) && src.displayName.Contains("드래곤");
        bool rankIsBoss = (src.rank == EnemyRank.Boss);

        return rankIsBoss && nameIsDragon;
    }

    bool IsDragonBoss(EnemyData src)
    {
        if (src == null) return false;
        bool nameIsDragon = !string.IsNullOrEmpty(src.displayName) && src.displayName.Contains("드래곤");
        bool rankIsBoss = (src.rank == EnemyRank.Boss);
        return rankIsBoss && nameIsDragon;
    }

    bool IsDragonDefending(BattleActorRuntime actor)
    {
        if (actor == null || !actor.isEnemy) return false;

        if (!TryGetEnemySrc(actor, out var src) || src == null) return false;

        bool isDragonBoss =
            src.rank == EnemyRank.Boss &&
            !string.IsNullOrEmpty(src.displayName) &&
            src.displayName.Contains("드래곤");

        if (!isDragonBoss) return false;

        // 너가 이미 쓰고 있는 "이번 플레이어 행동에 대해 방어 발동" 플래그
        return _dragonDefendedThisAction;
    }

    IEnumerator TryDragonPreDefend(EnemyData targetSrc, BattleActorRuntime targetActor)
    {
        _dragonDefendedThisAction = false;

        // 타겟이 드래곤 보스가 아니면 아무것도 안 함
        if (!IsDragonBoss(targetSrc) || targetActor == null || targetActor.IsDead) yield break;

        // 확률 체크
        if (Random.value > dragonDefendChance) yield break;

        // 1) 먼저 방어 자세
        TriggerAnim(targetActor, "Defend"); // Animator에 Defend Trigger가 있어야 함(스샷에 있음)

        _dragonDefendedThisAction = true;

        // 2) “먼저 방어”가 눈에 보이도록 아주 짧게 대기
        if (dragonDefendEnterHold > 0f)
            yield return new WaitForSeconds(dragonDefendEnterHold);
    }

    void EndDragonDefendIfNeeded(BattleActorRuntime targetActor)
    {
        if (!_dragonDefendedThisAction) return;
        if (targetActor == null) return;

        // 공격 1회 끝났으니 원복
        if (!actorViews.TryGetValue(targetActor, out var tf) || tf == null) return;

        var anim = tf.GetComponentInChildren<Animator>(true);
        if (anim == null) return;

        // 가장 확실한 원복: 드래곤 Idle 상태로 강제 복귀
        // (스샷에 있는 상태명)
        anim.CrossFade("G_Dragon_Idle_Battle", 0.10f, 0, 0f);

        _dragonDefendedThisAction = false;
    }

    int ApplyDragonDefenseIfNeeded(BattleActorRuntime attacker, BattleActorRuntime victim, int damage)
    {
        if (attacker == null || victim == null) return damage;

        // 플레이어가 드래곤을 때릴 때 + 이번 액션에서 방어가 발동된 경우만 감산
        if (!attacker.isEnemy && victim.isEnemy && IsDragonBoss(victim) && _dragonDefendedThisAction)
        {
            int reduced = Mathf.Max(1, Mathf.RoundToInt(damage * dragonDefendDamageMul));
            if (hud) hud.AppendLog($"[Boss] 드래곤 방어! 데미지 감소 ({damage} → {reduced})");
            return reduced;
        }

        return damage;
    }


    // ─────────────────────────────────────────────
    // Enemy Center Anchor
    // ─────────────────────────────────────────────
    void CreateEnemyCenterAnchor()
    {
        var go = new GameObject("EnemyCenterAnchor");
        enemyCenterAnchor = go.transform;
        enemyCenterAnchor.position = Vector3.zero;
    }

    void UpdateEnemyCenterAnchor()
    {
        var alive = enemies.Select(e => e.actor).Where(a => a != null && !a.IsDead).ToList();
        if (alive.Count == 0) return;

        Vector3 sum = Vector3.zero;
        int count = 0;

        for (int i = 0; i < alive.Count; i++)
        {
            var a = alive[i];
            if (actorViews.TryGetValue(a, out var tf) && tf)
            {
                sum += tf.position;
                count++;
            }
        }

        if (count > 0)
            enemyCenterAnchor.position = sum / count;
    }

    Transform GetEnemyCenterLookTarget()
    {
        if (enemyCenterAnchor != null) return enemyCenterAnchor;

        var one = enemies.Select(e => e.actor).FirstOrDefault(x => x != null && !x.IsDead);
        if (one != null && actorViews.TryGetValue(one, out var tf)) return tf;
        return null;
    }

    // ─────────────────────────────────────────────
    // Spawn
    // ─────────────────────────────────────────────
    void SpawnAlliesVisual()
    {
        for (int i = 0; i < allies.Count; i++)
        {
            var actor = allies[i];
            if (actor?.data == null) continue;

            GameObject prefab = actor.data.explorationPrefab;
            if (!prefab) continue;

            Transform point = (allySpawnPoints != null && i < allySpawnPoints.Length) ? allySpawnPoints[i] : null;
            Vector3 pos = point ? point.position : new Vector3(-3f - i, 0f, 0f);
            Quaternion rot = point ? point.rotation : Quaternion.Euler(0, 90f, 0);

            var go = Instantiate(prefab, pos, rot);

            // 탐험용 오동작 차단
            var pc = go.GetComponentInChildren<PlayerControllerHumanoid>(true);
            if (pc != null)
            {
                if (pc.secretArtFxRoot != null) pc.secretArtFxRoot.SetActive(false);
                pc.enabled = false;
            }

            var pi = go.GetComponentInChildren<PlayerInput>(true);
            if (pi != null) pi.enabled = false;

            var cc = go.GetComponentInChildren<CharacterController>(true);
            if (cc != null) cc.enabled = false;

            if (forceDisableRootMotion)
            {
                var anim = go.GetComponentInChildren<Animator>(true);
                if (anim != null) anim.applyRootMotion = false;
            }

            actorViews[actor] = go.transform;
            actorBasePositions[actor] = go.transform.position;
            actorBaseRotations[actor] = go.transform.rotation;
        }
    }

    void SpawnEnemiesVisual()
    {
        for (int i = 0; i < enemies.Count; i++)
        {
            var (actor, src) = enemies[i];
            if (actor == null || src == null) continue;

            GameObject prefab = src.battlePrefab ? src.battlePrefab : src.explorationPrefab;
            if (!prefab) continue;

            Transform point = (enemySpawnPoints != null && i < enemySpawnPoints.Length) ? enemySpawnPoints[i] : null;
            Vector3 pos = point ? point.position : new Vector3(3f + i, 0f, 0f);
            Quaternion rot = point ? point.rotation : Quaternion.Euler(0, -90f, 0);

            var go = Instantiate(prefab, pos, rot);

            // ─────────────────────────────────────────────
            // [중요] Battle 씬은 NavMesh가 없을 수 있으므로
            // NavMeshAgent가 켜진 상태로 존재하면 Instantiate 순간 에러가 날 수 있음.
            // => 생성 직후 즉시 꺼서 원천 차단
            // ─────────────────────────────────────────────
            var agent = go.GetComponentInChildren<UnityEngine.AI.NavMeshAgent>(true);
            if (agent != null) agent.enabled = false;

            var wander = go.GetComponentInChildren<EnemyWanderAI>(true);
            if (wander != null) wander.enabled = false;

            // Battle 애니메이터로 스위칭(있으면)
            var switcher = go.GetComponentInChildren<EnemyAnimatorSwitcher>(true);
            if (switcher != null) switcher.UseBattle();

            // Root Motion 차단(옵션)
            if (forceDisableRootMotion)
            {
                var anim = go.GetComponentInChildren<Animator>(true);
                if (anim != null) anim.applyRootMotion = false;
            }

            actorViews[actor] = go.transform;
            actorBasePositions[actor] = go.transform.position;
            actorBaseRotations[actor] = go.transform.rotation;
        }
    }

    void GrantVictoryDrops()
    {
        if (_dropsGranted) return;
        _dropsGranted = true;

        if (GameContext.I == null || GameContext.I.IsPartyWiped()) return;

        GameContext.I.BeginInventoryBatch();
        try
        {
            var enemyDatas = GetEnemyDatasInThisBattle();
            if (enemyDatas == null || enemyDatas.Count == 0)
                return;

            int seed = (dropSeedOverride != 0) ? dropSeedOverride : System.Environment.TickCount;
            var rng = new System.Random(seed);

            // ★ 1) 먼저 로컬에서 합산 (O(N))
            Dictionary<ItemData, int> acc = new();
            int dropLines = 0;

            foreach (var ed in enemyDatas)
            {
                if (ed == null || ed.dropTable == null) continue;

                var rolled = ed.dropTable.Roll(rng);
                foreach (var pair in rolled)
                {
                    var item = pair.item;
                    var qty = pair.qty;
                    if (item == null || qty <= 0) continue;

                    // 안전 로그(필요시)
                    // Debug.Log($"[Drop] item={item.name} id={item.id} maxStack={item.maxStack} qty={qty}");

                    if (item.maxStack <= 1 && qty > 1)
                        qty = 1;

                    if (acc.TryGetValue(item, out int cur)) acc[item] = cur + qty;
                    else acc[item] = qty;

                    dropLines++;
                    if (dropLines >= 2000) // 임시 상한 (상황에 맞게 조절)
                    {
                        Debug.LogError("[Drop] dropLines exceeded safety limit (2000). Breaking to prevent freeze.");
                        break;
                    }
                }
            }

            // ★ 2) 합산된 결과만 인벤+토스트에 반영
            int applied = 0;
            foreach (var kv in acc)
            {
                var item = kv.Key;
                var qty = kv.Value;

                GameContext.I.AddItem(item, qty);
                GameContext.I.QueueReward(item, qty);
                applied++;
            }

#if UNITY_EDITOR
            if (applied == 0)
                Debug.Log($"[Drop] no drops (seed={seed})");
#endif
        }
        finally
        {
            GameContext.I.EndInventoryBatch();
        }
    }

    // 여기만 너 프로젝트 구조에 맞게 수정하면 MVP 완성
    List<EnemyData> GetEnemyDatasInThisBattle()
    {
        // BattleController가 이미 enemies 리스트에 (actor, EnemyData)를 저장하고 있음.
        // 따라서 src만 뽑으면 "이번 전투에 참가한 적 데이터"가 된다.
        var list = new List<EnemyData>(enemies.Count);

        for (int i = 0; i < enemies.Count; i++)
        {
            var ed = enemies[i].src;
            if (ed != null) list.Add(ed);
        }

        return list;
    }

    void HandleMouseTargetSelect()
    {
        if (!isPlayerSelecting) return;

        if (Mouse.current == null) return;
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

        var cam = Camera.main;
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out var hit, 999f, enemyClickMask, QueryTriggerInteraction.Ignore))
            return;

        Transform t = hit.transform;
        while (t != null)
        {
            if (viewToActor.TryGetValue(t, out var actor))
            {
                if (actor != null && actor.isEnemy && !actor.IsDead)
                {
                    SetSelectedEnemy(actor);
                }
                return;
            }
            t = t.parent;
        }
    }

    void BuildViewReverseLookup()
    {
        viewToActor.Clear();
        foreach (var kv in actorViews)
        {
            if (kv.Value != null && kv.Key != null)
                viewToActor[kv.Value] = kv.Key;
        }
    }

    void EnsureTargetMarker()
    {
        if (targetMarkerUI == null)
        {
            Debug.LogWarning("[Battle] TargetMarkerUI(Image)가 연결되지 않았습니다. Canvas 아래 Image를 만들고 BattleController에 연결하세요.");
            return;
        }

        // 시작은 꺼두기
        targetMarkerUI.gameObject.SetActive(false);

        // 스케일 적용
        targetMarkerUI.rectTransform.localScale = Vector3.one * Mathf.Max(0.01f, targetMarkerUIScale);

        //if (targetMarkerGO != null) return;

        //targetMarkerGO = new GameObject("TargetMarker");
        //targetMarkerGO.transform.position = Vector3.zero;

        //targetMarkerSR = targetMarkerGO.AddComponent<SpriteRenderer>();
        //targetMarkerSR.sprite = targetMarkerSprite;
        //targetMarkerSR.sortingOrder = 5000;

        //// 알파값 적용
        //Color c = targetMarkerSR.color;
        //c.a = targetMarkerAlpha;
        //targetMarkerSR.color = c;

        //targetMarkerGO.transform.localScale = Vector3.one * Mathf.Max(0.01f, targetMarkerScale);
        //targetMarkerGO.SetActive(false);
    }

    IEnumerator WaitHitByAnimEventOrFallback(Transform attackerTf, float fallbackDelay)
    {
        if (!useAnimationEventForHit)
        {
            if (fallbackDelay > 0f) yield return new WaitForSeconds(fallbackDelay);
            yield break;
        }

        var receiver = attackerTf ? attackerTf.GetComponentInChildren<AttackEventReceiver>(true) : null;
        if (receiver == null)
        {
            if (fallbackDelay > 0f) yield return new WaitForSeconds(fallbackDelay);
            yield break;
        }

        bool hitFired = false;
        void OnHit() => hitFired = true;

        receiver.OnHitFrame += OnHit;
        try
        {
            float t = 0f;
            while (!hitFired && t < hitEventTimeout)
            {
                t += Time.deltaTime;
                yield return null;
            }
        }
        finally
        {
            receiver.OnHitFrame -= OnHit;
        }

        if (!hitFired && fallbackDelay > 0f)
            yield return new WaitForSeconds(fallbackDelay);
    }

    /// <summary>
    /// 전투 메인 루프.
    /// SPD 순서에 따라 턴을 진행하고, 승패가 결정되면 전투를 종료한다.
    /// </summary>
    IEnumerator BattleLoop()
    {
        SetAllVisible(true);

        while (true)
        {
            if (!_endingBattle && IsBattleEnd())
            {
                EndBattle();
                yield break;
            }

            if (turnQueue.Count == 0)
                RebuildTurnQueue();

            var actor = turnQueue.Dequeue();
            if (actor == null || actor.IsDead) continue;

            if (hud)
            {
                string who = actor.isEnemy ? $"Enemy : {actor.data.displayName}" : $"Ally : {actor.data.displayName}";
                hud.Render(allies.ToArray(), enemies.Select(e => e.actor).ToArray(), who);
            }

            if (actor.isEnemy) yield return EnemyTurnRoutine(actor);
            else yield return PlayerTurnRoutine(actor);

            // 연출(스킬 등) 잠금이 풀릴 때까지 다음 턴으로 진행 금지
            while (IsCinematicLocked)
                yield return null;

            actor.TickTurnEnd();
        }
    }

    void RebuildTurnQueue()
    {
        var all = allies.Where(a => a != null && !a.IsDead)
            .Concat(enemies.Where(e => e.actor != null && !e.actor.IsDead).Select(e => e.actor))
            .OrderByDescending(GetEffectiveSpd)
            .ThenBy(a => a.isEnemy) // ★ 예: 아군 먼저(false), 적은 나중(true)
            .ToList();

        turnQueue = new Queue<BattleActorRuntime>(all);
    }

    bool IsBattleEnd()
    {
        bool alliesDead = allies.Count == 0 || allies.All(a => a == null || a.IsDead);
        bool enemiesDead = enemies.Count == 0 || enemies.All(e => e.actor == null || e.actor.IsDead);
        return alliesDead || enemiesDead;
    }

    private int GetEffectiveSpd(BattleActorRuntime a)
    {
        //int baseSpd = (a != null && a.data != null) ? a.data.baseSPD : 0;

        //// enemy는 프리버프 없음
        //if (a == null || a.isEnemy) return baseSpd;

        //// ally면 tempSpdAdd 합산
        //if (GameContext.I != null && _allyPartyIndex.TryGetValue(a, out int partyIdx))
        //{
        //    if (partyIdx >= 0 && partyIdx < GameContext.I.party.Count)
        //        baseSpd += GameContext.I.party[partyIdx].tempSpdAdd;
        //}

        //return a.GetEffectiveSPD();   // ★ 여기서 버프 포함된 최종 SPD를 사용
        if (a == null) return 0;
        return a.GetEffectiveSPD();
    }

    void EndBattle()
    {
        SetAllVisible(true);

        // 1) 전투 결과를 원본 파티(GameContext)로 반영
        SyncBattlePartyStateToGameContext();

        bool enemiesDead = enemies.Count == 0 || enemies.All(e => e.actor == null || e.actor.IsDead);
        bool wonFinalBoss = enemiesDead && IsFinalBossBattle();

        if (GameContext.I != null && GameContext.I.IsPartyWiped())
        {
            if (Application.CanStreamedLevelBeLoaded("GameOver"))
                SceneManager.LoadScene("GameOver");
            else
                SceneManager.LoadScene("Title");
            return;
        }

        if (enemiesDead)
        {
            GrantVictoryDrops();
            NotifyQuestEnemyKills();

            // 튜토리얼: 전투는 "승리했을 때만" 완료 처리
            TutorialManager.I?.CompleteBattleTutorialIfNeeded();
        }


        if (wonFinalBoss)
        {
#if UNITY_EDITOR
            Debug.Log("[BattleController] Final boss defeated. Return to Exploration and wait for final quest reward claim.");
#endif
        }

        // 승리일 때만: 로컬 spawn payload로 처리
        if (enemiesDead && GameContext.I != null && !string.IsNullOrEmpty(_spawnId))
        {
            bool uniqueBoss = false;
            float delay = _respawnDelay > 0f ? _respawnDelay : 30f;

            for (int i = 0; i < enemies.Count; i++)
            {
                var src = enemies[i].src;
                if (src == null) continue;

                if (src.rank == EnemyRank.Boss)
                {
                    uniqueBoss = true;
                    break;
                }

                if (src.uniqueDefeat)
                {
                    uniqueBoss = true;
                    break;
                }

                if (src.respawnDelayOverride >= 0f)
                    delay = Mathf.Max(delay, src.respawnDelayOverride);
            }

            if (uniqueBoss)
                GameContext.I.MarkUniqueDefeated(_spawnId);
            else
                GameContext.I.MarkSpawnDefeated(_spawnId, delay);
        }

        GameContext.I.ClearBattleTemporaryBuffs();

        // 로컬도 정리 (선택)
        _spawnId = null;
        _respawnDelay = 0f;

        string next = (GameContext.I != null) ? GameContext.I.returnExplorationSceneName : "Exploration";
        if (SceneFader.I != null) SceneFader.I.LoadSceneWithFade(next);
        else SceneManager.LoadScene(next);
    }

    void ShowEndingPanelOrHandleFinalBossEnding()
    {
        if (SceneFader.I != null)
            SceneFader.I.LoadSceneWithFade("Exploration");
        else
            SceneManager.LoadScene("Exploration");
    }

    void NotifyQuestEnemyKills()
    {
        if (QuestManager.I == null) return;
        if (enemies == null || enemies.Count == 0) return;

        Dictionary<string, int> killCounts = new();

        // 1) 이번 전투의 EnemyData.id별 개수 합산
        for (int i = 0; i < enemies.Count; i++)
        {
            var src = enemies[i].src;
            if (src == null) continue;
            if (string.IsNullOrEmpty(src.id)) continue;

            if (killCounts.TryGetValue(src.id, out int cur))
                killCounts[src.id] = cur + 1;
            else
                killCounts[src.id] = 1;
        }

        // 2) id별로 퀘스트에 통보
        foreach (var kv in killCounts)
        {
            string enemyId = kv.Key;
            int count = kv.Value;

            bool isBoss = false;

            for (int i = 0; i < enemies.Count; i++)
            {
                var src = enemies[i].src;
                if (src == null) continue;
                if (src.id != enemyId) continue;

                if (src.rank == EnemyRank.Boss)
                {
                    isBoss = true;
                    break;
                }
            }

            //Debug.Log($"[QuestKill] enemyId={enemyId}, count={count}, isBoss={isBoss}, questManagerExists={(QuestManager.I != null)}");
            QuestManager.I?.NotifyEnemyKilled(enemyId, count, isBoss);
        }
    }

    IEnumerator CoEndBattleFadeThenExit()
    {
        _endingBattle = true;
        PushCinematicLock();

        // (선택) 마지막 죽음 모션이 1~2프레임이라도 보이게 아주 짧게 양보
        yield return null;

        if (endFadeGroup != null)
        {
            endFadeGroup.gameObject.SetActive(true);
            endFadeGroup.alpha = 0f;

            // 페이드 시작 순간부터만 클릭 막기
            endFadeGroup.interactable = false;
            endFadeGroup.blocksRaycasts = true;

            float t = 0f;
            float dur = Mathf.Max(0.01f, endFadeOutTime);

            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                endFadeGroup.alpha = Mathf.Clamp01(t / dur);
                yield return null;
            }

            if (endFadeHoldTime > 0f)
                yield return new WaitForSecondsRealtime(endFadeHoldTime);
        }
        else
        {
            // CanvasGroup 연결 안 했으면 최소 홀드라도
            yield return new WaitForSecondsRealtime(0.2f);
        }

        PopCinematicLock();
        EndBattle();
    }

    Transform GetTargetAnchorOrFallback(BattleActorRuntime enemy, Transform enemyRoot)
    {
        if (enemy == null || enemyRoot == null) return null;

        // 캐시 먼저
        if (_targetAnchorCache.TryGetValue(enemy, out var cached) && cached != null)
            return cached;

        // 이름은 네가 만든 오브젝트 이름과 정확히 일치해야 함
        var found = FindChildRecursive(enemyRoot, "TargetAnchor");

        // 못 찾으면 null(= fallback 쓰게)
        _targetAnchorCache[enemy] = found;
        return found;
    }

    Transform FindChildRecursive(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;

        for (int i = 0; i < root.childCount; i++)
        {
            var found = FindChildRecursive(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }

    Transform GetVfxAnchorOrFallback(Transform actorRoot, string anchorName, Transform fallback)
    {
        var a = FindChildRecursive(actorRoot, anchorName);
        return a != null ? a : fallback;
    }

    /// <summary>
    /// 플레이어 턴 처리.
    /// 행동 선택 UI를 열고, 기본 공격 또는 일반 스킬을 실행한다.
    /// 현재 프로젝트 범위에서는 궁극기 시스템을 사용하지 않는다.
    /// </summary>
    IEnumerator PlayerTurnRoutine(BattleActorRuntime actor)
    {
        if (!actorViews.TryGetValue(actor, out var actorTf) || !actorTf)
            yield break;

        if (hideOtherAlliesDuringTurns)
        {
            SetAllVisible(true);
            HideOtherAlliesExcept(actor);
        }
        else
        {
            SetAllVisible(true);
        }

        var enemyCenter = GetEnemyCenterLookTarget();
        if (cameraController != null && enemyCenter != null)
            CameraFollow(actorTf, enemyCenter, instant: false);

        // 플레이어 턴: 타겟 선택 가능 + 마커 표시
        isPlayerSelecting = true;
        showTargetMarker = true;
        AutoPickTargetIfNeeded();
        UpdateTargetMarker(forceOn: true);

        // 선택 결과
        // 0 = 기본 공격, 1 = 일반 스킬
        int chosenSkillIndex = 0;

        // 실제 실행할 SkillData
        SkillData usedSkill = null;

        // 선택 단계:
        // 스킬 포인트가 부족하면 행동을 확정하지 않고 다시 선택하게 한다.
        while (true)
        {
            bool chosen = false;

            if (skillUI != null)
            {
                skillUI.ShowFor(actor, (idx) =>
                {
                    chosenSkillIndex = idx;
                    chosen = true;
                });

                if (hud)
                    hud.AppendLog($"{actor.data.displayName}의 턴. 행동을 선택하세요.");

                while (!chosen)
                    yield return null;
            }
            else
            {
                // UI가 없으면 기본 공격으로 안전 처리
                chosenSkillIndex = 0;
            }

            // 선택된 행동에 따라 실제 SkillData를 결정한다.
            usedSkill = null;
            switch (chosenSkillIndex)
            {
                case 0:
                    usedSkill = actor.data.basicAtk;
                    break;

                case 1:
                    usedSkill = actor.data.skill;
                    break;

                default:
                    // 예외 상황에서는 기본 공격으로 안전 처리
                    usedSkill = actor.data.basicAtk;
                    break;
            }

            // 스킬 포인트 소비(최종 방어):
            // 일반 스킬은 SkillData.spCost 기준으로 SP를 소모한다.
            int cost = 0;
            if (chosenSkillIndex == 1 && usedSkill != null)
                cost = Mathf.Max(0, usedSkill.spCost);

            if (GameContext.I != null && cost > 0)
            {
                if (!GameContext.I.TrySpendBattleSkillPoint(cost))
                {
                    if (hud)
                        hud.AppendLog("[SP] 스킬 포인트가 부족합니다. 다른 행동을 선택하세요.");

                    continue;
                }
            }

            break; // 행동 확정
        }

        // 공격/스킬 실행 중에는 타겟 마커를 숨긴다.
        showTargetMarker = false;
        UpdateTargetMarker();

        yield return PlayMiniCinematic();

        // 현재 선택된 적 타겟 확보
        BattleActorRuntime target = selectedEnemy;
        Transform targetTf = null;

        if (target != null && actorViews.TryGetValue(target, out var ttf) && ttf)
            targetTf = ttf;

        // 실행 분기
        if (chosenSkillIndex == 0)
        {
            // 기본 공격은 기존 이동/타격 시퀀스를 사용한다.
            if (target == null || targetTf == null)
            {
                SetAllVisible(true);
                yield break;
            }

            if (cameraController != null)
            {
                if (freezeCameraDuringAttackMotion)
                    CameraFreezeAt(actorTf, targetTf, instant: false);
                else
                    CameraFollow(actorTf, targetTf, instant: false);
            }

            // 드래곤 보스 타겟이면 선방어 패턴을 먼저 확인한다.
            EnemyData targetSrc = null;
            if (target != null && target.isEnemy)
                TryGetEnemySrc(target, out targetSrc);

            yield return TryDragonPreDefend(targetSrc, target);

            // 기본 공격 데미지 계산
            int damage = CalcDamage(actor, target, actor.data.basicAtk, out bool isCritical);

            // 드래곤이 이번 액션에서 방어를 발동했다면 데미지를 추가로 감소시킨다.
            if (_dragonDefendedThisAction)
                damage = Mathf.Max(1, Mathf.RoundToInt(damage * dragonDefendDamageMul));

            if (hud)
            {
                string skillName = (actor.data.basicAtk != null && !string.IsNullOrEmpty(actor.data.basicAtk.displayName))
                    ? actor.data.basicAtk.displayName
                    : "기본 공격";

                if (isCritical)
                    hud.AppendLog($"CRITICAL! {actor.data.displayName} ▶ {skillName} ▶ {target.data.displayName}");
                else
                    hud.AppendLog($"{actor.data.displayName} ▶ {skillName} ▶ {target.data.displayName}");
            }

            float prevMaxMove = maxMoveDistance;

            // 드래곤은 제자리형 보스에 가깝기 때문에 이동 거리 정책을 별도로 적용한다.
            if (target != null && target.isEnemy)
            {
                if (targetSrc != null &&
                    targetSrc.rank == EnemyRank.Boss &&
                    !string.IsNullOrEmpty(targetSrc.displayName) &&
                    targetSrc.displayName.Contains("드래곤"))
                {
                    maxMoveDistance = dragonMaxMoveDistance;
                }
            }

            yield return AttackSequence(
                attacker: actor,
                victim: target,
                attackerTf: actorTf,
                victimTf: targetTf,
                damagePreview: damage,
                isCritical: isCritical,
                onHitInstant: () =>
                {
                    string basicSfxKey = GetPlayerBasicAttackSfxKey(actor);
                    if (!string.IsNullOrEmpty(basicSfxKey))
                        AudioManager.I?.PlaySFX2D(basicSfxKey);

                    ApplyDamageAndPopup(target, targetTf, damage, isCritical);
                }
            );

            // 이동 거리 정책 원복
            maxMoveDistance = prevMaxMove;

            // 이번 액션에서 사용된 드래곤 방어 상태를 종료 처리
            EndDragonDefendIfNeeded(target);

            // 기본 공격 성공 시 Battle SP를 회복한다.
            if (GameContext.I != null && basicAttackGainSP > 0)
                GameContext.I.AddBattleSkillPoints(basicAttackGainSP);
        }
        else if (chosenSkillIndex == 1)
        {
            // 일반 스킬 실행
            yield return ExecuteSkill(actor, usedSkill, target, actorTf, targetTf);
        }

        yield return new WaitForSeconds(0.05f);

        // 턴 종료:
        // 선택 상태와 마커를 정리하고 카메라/가시성을 원복한다.
        isPlayerSelecting = false;
        showTargetMarker = false;
        UpdateTargetMarker();

        cameraFrozen = false;
        SetAllVisible(true);
    }

    // ─────────────────────────────────────────────
    // Enemy Turn
    //  - 골렘: Throw(전체공격) = 기존 EnemyThrowRoutine 유지
    //  - 드래곤(보스): Breath(브레스 전체공격) = "골렘 전체공격 시점(카메라/가시성)"만 동일하게 적용
    //    ※ 드래곤은 EnemyThrowRoutine을 타지 않음(= GolemThrowEventRelay 경고 방지)
    // ─────────────────────────────────────────────
    /// <summary>
    /// 적 턴 처리.
    /// 일반 적, 골렘, 드래곤 보스의 패턴을 분기하여 실행한다.
    /// </summary>
    IEnumerator EnemyTurnRoutine(BattleActorRuntime enemyActor)
    {
        if (!actorViews.TryGetValue(enemyActor, out var enemyTf) || !enemyTf)
            yield break;

        // 플레이어 선택/마커 끄기
        isPlayerSelecting = false;
        showTargetMarker = false;
        UpdateTargetMarker();

        // 살아있는 아군
        var aliveAllies = allies.Where(a => a != null && !a.IsDead).ToList();
        if (aliveAllies.Count == 0) yield break;

        // 피해자 1명(단일공격용)
        BattleActorRuntime victim = enemyPickRandomVictim
            ? aliveAllies[Random.Range(0, aliveAllies.Count)]
            : aliveAllies[0];

        if (!actorViews.TryGetValue(victim, out var victimTf) || !victimTf)
            yield break;

        // ─────────────────────────────────────────────
        // EnemyData 판별
        // ─────────────────────────────────────────────
        EnemyData src = null;
        TryGetEnemySrc(enemyActor, out src);

        // ─────────────────────────────────────────────
        // (A) "골렘" 전체공격(Throw) 분기
        // ─────────────────────────────────────────────
        bool isGolem = false;
        if (src != null)
        {
            if (!string.IsNullOrEmpty(src.displayName) && src.displayName.Contains("골렘"))
                isGolem = true;
        }

        // 골렘은 이동거리 정책(원하면 유지)
        // ※ 여기서 바꾸면 다음 적에게도 영향갈 수 있으니,
        //    최소한 EnemyThrowRoutine 끝나고 원복하는 구조가 더 안전함.
        //    (이번 답변은 기존 흐름 유지하되, 드래곤 쪽은 확실히 원복 처리함)
        if (isGolem)
            maxMoveDistance = 3f;

        bool doThrow = isGolem && (Random.value < golemThrowChance);

        if (doThrow)
        {
            SetAllVisible(true);
            yield return PlayMiniCinematic();

            yield return EnemyThrowRoutine(enemyActor, enemyTf);

            yield return new WaitForSeconds(0.05f);
            cameraFrozen = false;
            SetAllVisible(true);
            yield break;
        }

        // ─────────────────────────────────────────────
        // (B) 드래곤 보스 판별
        // ─────────────────────────────────────────────
        bool isDragonBoss = false;
        if (src != null)
        {
            bool nameIsDragon = (!string.IsNullOrEmpty(src.displayName) && src.displayName.Contains("드래곤"));
            bool rankIsBoss = (src.rank == EnemyRank.Boss);
            isDragonBoss = rankIsBoss && nameIsDragon;
        }

        // ─────────────────────────────────────────────
        // (B-1) 드래곤: 스크림(공격 대신) / 브레스(전체공격)
        // ─────────────────────────────────────────────
        if (isDragonBoss)
        {
            // 드래곤 턴 시작 시 카메라 구도를 먼저 정상화
            if (cameraController != null)
            {
                if (freezeCameraDuringAttackMotion)
                    CameraFreezeAt(victimTf, enemyTf, instant: false);
                else
                    CameraFollow(victimTf, enemyTf, instant: false);
            }

            lastVictim = victim;

            // (3) 드래곤 전용 이동거리 정책
            float prevMaxMove = maxMoveDistance;
            maxMoveDistance = dragonMaxMoveDistance;

            // 턴 잠금(연출 중 다음 턴으로 넘어가지 않게)
            PushCinematicLock();

            try
            {
                // 전체공격/연출은 전원 보이기
                SetAllVisible(true);

                // (원하면) 미니 컷
                yield return PlayMiniCinematic();

                // ────────────────
                // 스크림 확률 분기 (공격 대신)
                // ────────────────
                if (Random.value < dragonScreamChance)
                {
                    // 스크림 애니 트리거
                    TriggerAnim(enemyActor, "Scream");
                    AudioManager.I?.PlaySFX2D(SFXKey.Boss_Scream);

                    // 다음 공격 1회 버프 세팅
                    _dragonNextAttackMul = Random.Range(dragonScreamMulMin, dragonScreamMulMax);

                    if (hud)
                        hud.AppendLog($"[Boss] 드래곤 스크림! 다음 공격 피해 x{_dragonNextAttackMul:0.00}");

                    if (dragonScreamHold > 0f)
                        yield return new WaitForSeconds(dragonScreamHold);

                    // 스크림 종료 후 카메라를 일반 전투 구도로 한번 더 정리
                    cameraFrozen = false;
                    if (cameraController != null)
                        CameraFollow(victimTf, enemyTf, instant: false);

                    yield return new WaitForSeconds(0.05f);
                    SetAllVisible(true);
                    yield break;
                }

                // ────────────────
                // 브레스(전체공격) 진행
                // ────────────────

                // 골렘 전체공격과 동일한 시점: 카메라 고정(원하면)
                if (useFixedThrowCamera)
                    PushThrowCameraPose();

                bool isCrit = Random.value < enemyCritChance;

                // 기본 AoE 배율
                float aoeMul = enemyAoEDamageMul;

                // 스크림 버프(다음 공격 1회) 적용값
                float screamMul = Mathf.Max(1f, _dragonNextAttackMul);

                ClearEnemyHit();

                // "타격 시점"에 실행될 로직(전원 적용)
                ArmEnemyHit(() =>
                {

                    for (int i = 0; i < aliveAllies.Count; i++)
                    {
                        var v = aliveAllies[i];
                        if (v == null || v.IsDead) continue;

                        int raw = Mathf.Max(1, enemyActor.GetEffectiveATK() - v.GetEffectiveDEF());

                        // 여기서 screamMul을 곱해준다 (다음 공격 버프)
                        int scaled = Mathf.Max(1, Mathf.RoundToInt(raw * aoeMul * screamMul));
                        int dmg = isCrit ? Mathf.RoundToInt(scaled * critDamageMul) : scaled;

                        if (actorViews.TryGetValue(v, out var vtf) && vtf)
                        {
                            ApplyDamageAndPopup(v, vtf, dmg, isCrit);

                            bool willDie = v.hp <= 0;
                            TriggerAnim(v, willDie ? animTriggerDie : animTriggerHit);
                        }
                    }
                });

                int token = CurrentEnemyHitToken;

                // (선택) 이벤트 릴레이 토큰 주입
                var relay = GetEnemyAttackRelayFrom(enemyTf);
                if (relay != null)
                    relay.SetToken(token);

                // 브레스 애니 트리거 (현재 Attack)
                TriggerAnim(enemyActor, "Attack"); // 또는 "Breath"
                AudioManager.I?.PlaySFX2D(SFXKey.Boss_Breath);

                // 애니 이벤트(Hit) 기다림
                float t = 0f;
                while (!IsEnemyHitFired && t < hitEventTimeout)
                {
                    t += Time.unscaledDeltaTime;
                    yield return null;
                }

                if (!IsEnemyHitFired)
                {
                    // 이벤트가 없다면 늦은 타이밍으로 강제 타격
                    if (dragonBreathHitDelay > 0f)
                        yield return new WaitForSeconds(dragonBreathHitDelay);

                    NotifyEnemyAttackHit(token);
                }

                // 스크림 버프는 "다음 공격 1회"만 적용 → 여기서 소모 처리
                // (이 브레스가 실제로 데미지를 발생시켰으므로 리셋)
                _dragonNextAttackMul = 1f;

                // 연출 홀드(애니가 너무 빨리 끝나고 턴 넘어가는 문제 방지)
                float hold = Mathf.Max(afterAttackAnimHold, 0f) + Mathf.Max(dragonBreathTurnLockExtraHold, 0f);
                if (hold > 0f)
                    yield return new WaitForSeconds(hold);

                ClearEnemyHit();

                // 카메라 복구
                if (useFixedThrowCamera)
                    PopThrowCameraPose();

                yield return new WaitForSeconds(0.05f);
                cameraFrozen = false;
                SetAllVisible(true);
                yield break;
            }
            finally
            {
                // 잠금 해제 + maxMoveDistance 원복
                PopCinematicLock();
                maxMoveDistance = prevMaxMove;

                // 안전: 혹시 예외/조기 종료로 카메라가 고정된 채 남는 걸 방지
                // (Push 안 했으면 Pop이 바로 return 처리되어도 OK)
                if (useFixedThrowCamera)
                    PopThrowCameraPose();

                ClearEnemyHit();
            }
        }

        // ─────────────────────────────────────────────
        // (C) 나머지 적들: 기존 단일 공격(접근 이동 + 이벤트 히트)
        // ─────────────────────────────────────────────

        if (hideOtherAlliesDuringTurns)
        {
            SetAllVisible(true);
            HideOtherAlliesExcept(victim);
        }
        else
        {
            SetAllVisible(true);
        }

        bool sameVictim = (lastVictim == victim);

        if (cameraController != null)
        {
            if (keepCameraIfSameVictim && sameVictim)
            {
                if (freezeCameraDuringAttackMotion && !cameraFrozen)
                    CameraFreezeAt(victimTf, enemyTf, instant: false);
            }
            else
            {
                if (freezeCameraDuringAttackMotion)
                    CameraFreezeAt(victimTf, enemyTf, instant: false);
                else
                    CameraFollow(victimTf, enemyTf, instant: false);
            }
        }

        lastVictim = victim;

        int atk = enemyActor.GetEffectiveATK();
        int def = victim.GetEffectiveDEF() + GetTempDefBonus(victim);
        int rawDamage = Mathf.Max(1, atk - def);
        bool isCritical = Random.value < enemyCritChance;
        int damage = isCritical ? Mathf.RoundToInt(rawDamage * critDamageMul) : rawDamage;

        if (hud)
        {
            if (isCritical) hud.AppendLog($"CRITICAL! {enemyActor.data.displayName}이(가) {victim.data.displayName}을(를) 강하게 공격!");
            else hud.AppendLog($"{enemyActor.data.displayName}이(가) {victim.data.displayName}을(를) 공격!");
        }

        yield return PlayMiniCinematic();

        ClearEnemyHit();
        ArmEnemyHit(() =>
        {
            AudioManager.I?.PlaySFX2D(SFXKey.Enemy_Basic);

            ApplyDamageAndPopup(victim, victimTf, damage, isCritical);

            bool willDie = victim.hp <= 0;
            TriggerAnim(victim, willDie ? animTriggerDie : animTriggerHit);
        });

        var basicRelay = GetEnemyAttackRelayFrom(enemyTf);
        if (basicRelay != null)
        {
            basicRelay.SetToken(CurrentEnemyHitToken);
        }
        else
        {
            Debug.LogWarning("[EnemyAttack] EnemyAttackEventRelay not found on Animator object. AnimationEvent will not be received.");
        }

        yield return AttackSequence(
            attacker: enemyActor,
            victim: victim,
            attackerTf: enemyTf,
            victimTf: victimTf,
            damagePreview: damage,
            isCritical: isCritical,
            onHitInstant: null,
            attackTriggerOverride: "Attack",
            enemyHitViaNotify: true,
            moveToTarget: true,
            triggerVictimReaction: false
        );

        if (!IsEnemyHitFired)
        {
            Debug.LogWarning("[EnemyAttack] Hit AnimationEvent not fired. Fallback damage applied.");
            NotifyEnemyAttackHit(CurrentEnemyHitToken);
        }

        yield return new WaitForSeconds(0.05f);

        cameraFrozen = false;
        SetAllVisible(true);
    }

    // ─────────────────────────────────────────────
    /// SkillData의 actionType에 맞춰 실제 스킬 연출과 효과를 실행한다.
    /// 현재 프로젝트에서는 HealParty / SingleStrongHit / AoEHitAllEnemies를 사용한다.
    // ─────────────────────────────────────────────
    IEnumerator ExecuteSkill(
        BattleActorRuntime caster,
        SkillData skill,
        BattleActorRuntime selectedTarget,
        Transform casterTf,
        Transform selectedTargetTf)
    {
        if (caster == null || casterTf == null)
            yield break;

        if (skill == null)
        {
            if (hud) hud.AppendLog("[Skill] SkillData가 비어 있습니다.");
            yield break;
        }

        // 핵심: 스킬 실행 동안 턴 진행 잠금
        PushCinematicLock();
        try
        {
            string skillSfx = GetPlayerSkillSfxKey(caster);
            if (!string.IsNullOrEmpty(skillSfx))
                AudioManager.I?.PlaySFX2D(skillSfx);

            switch (skill.actionType)
            {
                case SkillActionType.HealParty:
                    yield return ExecuteHealParty(caster, skill, casterTf);
                    break;

                case SkillActionType.SingleStrongHit:
                    yield return ExecuteSingleStrongHit(caster, skill, selectedTarget, casterTf, selectedTargetTf);
                    break;

                case SkillActionType.AoEHitAllEnemies:
                    yield return ExecuteAoEAllEnemies(caster, skill, casterTf);
                    break;

                default:
                    if (hud) hud.AppendLog($"[Skill] 지원하지 않는 actionType: {skill.actionType}");
                    break;
            }
        }
        finally
        {
            PopCinematicLock();
        }
    }

    IEnumerator ExecuteHealParty(BattleActorRuntime caster, SkillData skill, Transform casterTf)
    {
        // 1) 전원 보이기
        SetAllVisible(true);

        // 컷신 중에도 SoftFollowTick이 돌아야 pose override가 적용됨
        blockSoftFollowTick = false;

        if (cameraController != null && partyCamPivot != null)
        {
            // PartyCamPivot의 Position + Rotation을 카메라 포즈로 강제
            cameraController.PushPoseOverride(partyCamPivot, instant: false, blend: 0.25f);
        }

        Camera cam = Camera.main;

        // (옵션) 현재 카메라 상태 백업: 컷 끝나고 즉시 원복하고 싶을 때 사용 가능
        Vector3 camPosBackup = Vector3.zero;
        Quaternion camRotBackup = Quaternion.identity;
        bool hasBackup = false;

        bool camControllerWasEnabled = false;
        if (cameraController != null)
        {
            camControllerWasEnabled = cameraController.enabled;
            cameraController.enabled = false; // <<< 핵심: 컷 동안 카메라 컨트롤러가 Transform 건드리지 못하게
        }

        if (cam != null)
        {
            camPosBackup = cam.transform.position;
            camRotBackup = cam.transform.rotation;
            hasBackup = true;
        }

        // 네가 로그로 확인한 것처럼 pivot/look이 들어오는데도 구도가 안 맞을 때는
        // FocusDuel 대신 "카메라 Transform을 직접 세팅"해야 원하는 구도가 100% 나온다.
        if (cam != null && partyCamPivot != null)
        {
            cam.transform.SetPositionAndRotation(partyCamPivot.position, partyCamPivot.rotation);

            if (partyCamLook != null)
            {
                // LookAt은 회전을 덮어쓰므로, partyCamPivot.rotation을 그대로 쓰고 싶다면 LookAt을 빼고
                // partyCamPivot 회전을 직접 세팅하는 방식만 사용해도 된다.
                cam.transform.LookAt(partyCamLook.position);
            }
        }

        // 3) 스킬 애니
        TriggerAnim(caster, string.IsNullOrEmpty(skill.animTrigger) ? "Skill" : skill.animTrigger);

        // 4) 히트 타이밍
        yield return WaitHitByAnimEventOrFallback(casterTf, hitTimingDelay);

        // 5) 힐 VFX: 살아있는 아군 각각
        List<GameObject> spawned = null;
        if (skill.vfxPrefab != null)
        {
            spawned = new List<GameObject>(allies.Count);
            foreach (var ally in allies)
            {
                if (ally == null || ally.IsDead) continue;
                if (!actorViews.TryGetValue(ally, out var allyTf) || !allyTf) continue;

                Vector3 pos = allyTf.position + Vector3.up * 1.0f;
                var go = Instantiate(skill.vfxPrefab, pos, Quaternion.identity);
                spawned.Add(go);
            }
        }

        // 6) 회복 적용
        float p = Mathf.Clamp01(skill.healPercent);
        int totalHealed = 0;

        foreach (var ally in allies)
        {
            if (ally == null || ally.IsDead) continue;

            int amount = Mathf.RoundToInt(ally.maxHp * p);
            if (amount <= 0) amount = 1;

            int before = ally.hp;
            ally.hp = Mathf.Min(ally.maxHp, ally.hp + amount);
            totalHealed += Mathf.Max(0, ally.hp - before);
        }

        if (hud != null)
        {
            hud.RefreshHPBars(allies.ToArray(), enemies.Select(e => e.actor).ToArray());
            hud.AppendLog($"{caster.data.displayName} ▶ {skill.displayName} : 파티 회복 +{totalHealed}");
        }

        // 7) 컷 유지
        // "1번 스샷처럼" 충분히 보여주고 싶으면 여기 값을 늘리면 된다.
        // (예: 1.2~2.0) / StellarWitch처럼 4초를 원하면 4.0도 가능
        if (healCinematicHold > 0f)
            yield return new WaitForSeconds(healCinematicHold);

        // 8) VFX 정리
        if (spawned != null && spawned.Count > 0)
        {
            if (skill.vfxLifeTime > 0f)
                yield return new WaitForSeconds(skill.vfxLifeTime);

            for (int i = 0; i < spawned.Count; i++)
                if (spawned[i] != null) Destroy(spawned[i]);
        }

        // 9) 컷 종료: 카메라 컨트롤러 재활성화 + SoftFollow 재개 + 기존 로직으로 복귀
        if (cameraController != null)
            cameraController.enabled = camControllerWasEnabled;

        // 여기서 백업 사용
        if (cam != null && hasBackup)
        {
            cam.transform.SetPositionAndRotation(camPosBackup, camRotBackup);
        }

        if (cameraController != null)
        {
            // 컷신 포즈 강제 해제
            cameraController.PopPoseOverride();
        }

        // 컷신 끝나면 평상시 카메라 로직으로 복귀
        var enemyCenter = GetEnemyCenterLookTarget();
        if (cameraController != null && enemyCenter != null)
            CameraFollow(casterTf, enemyCenter, instant: false);

        // (옵션) 컨트롤러 복귀 전에 즉시 카메라를 백업값으로 돌리고 싶다면:
        // if (cam != null && hasBackup) cam.transform.SetPositionAndRotation(camPosBackup, camRotBackup);

        // 10) 가시성 원복
        if (hideOtherAlliesDuringTurns)
            HideOtherAlliesExcept(caster);
        else
            SetAllVisible(true);

    }

    // EncounterData 기반으로 EnemyData 리스트를 최종 확정(0번 확정 + 1~2번 확률 슬롯)
    List<EnemyData> RollEnemiesFromEncounter(EncounterData enc)
    {
        var result = new List<EnemyData>();
        if (enc == null) return result;

        // Slot 0: 무조건
        if (enc.guaranteedEnemy != null)
        {
            int c = Mathf.Max(1, enc.guaranteedCount);
            for (int i = 0; i < c; i++)
                result.Add(enc.guaranteedEnemy);

            var r = enc.guaranteedEnemy.rank;
            if (r == EnemyRank.Boss || r == EnemyRank.Elite)
            {
                // (선택) 경고로 데이터 실수 잡기
                // Debug.LogWarning($"[Encounter] {enc.name}: {r}는 optionalSlots를 무시합니다.");
                return result; // optional 슬롯 롤링 스킵
            }
        }

        // Slot 1~2: 확률 슬롯
        if (enc.optionalSlots != null)
        {
            for (int s = 0; s < enc.optionalSlots.Length; s++)
            {
                var slot = enc.optionalSlots[s];
                if (slot == null) continue;

                // 이 슬롯 자체가 뜰 확률
                if (Random.value > Mathf.Clamp01(slot.spawnChance))
                    continue;

                // 뜬다면 candidates에서 가중치 랜덤 선택
                var chosen = PickWeightedCandidate(slot.candidates);
                if (chosen == null || chosen.enemy == null) continue;

                int c = Mathf.Max(1, chosen.count);
                for (int i = 0; i < c; i++)
                    result.Add(chosen.enemy);
            }
        }

        return result;
    }

    public Vector3 GetAlliesCenterPosition()
    {
        var alive = allies.Where(a => a != null && !a.IsDead).ToList();
        if (alive.Count == 0) return Vector3.zero;

        Vector3 sum = Vector3.zero;
        int count = 0;

        for (int i = 0; i < alive.Count; i++)
        {
            var a = alive[i];
            if (actorViews.TryGetValue(a, out var tf) && tf)
            {
                // 몸통쯤(원하면 1.0~1.8 조절)
                sum += tf.position + Vector3.up * 1.2f;
                count++;
            }
        }

        return (count > 0) ? (sum / count) : Vector3.zero;
    }

    IEnumerator EnemyThrowRoutine(BattleActorRuntime enemyActor, Transform enemyTf)
    {
        _throwReleaseSeen = false;
        _throwReleaseToken = -1;
        _throwExpectedImpactTime = 0f;

        // 살아있는 아군
        var aliveAllies = allies.Where(a => a != null && !a.IsDead).ToList();
        if (aliveAllies.Count == 0)
            yield break;

        // Relay(돌 생성/발사/도착에서 Notify 호출하는 쪽)
        var relay = enemyTf.GetComponentInChildren<GolemThrowEventRelay>(true);
        if (relay == null)
            Debug.LogWarning("[Battle] GolemThrowEventRelay가 없습니다. (프리팹에 컴포넌트 추가 필요)");

        // 전체공격은 전원 보이게
        SetAllVisible(true);

        // 고정 카메라 진입
        if (useFixedThrowCamera)
            PushThrowCameraPose();

        // ─────────────────────────────────────────────
        // (딜 정책) Attack01 = 전체공격
        // ─────────────────────────────────────────────
        bool isCrit = Random.value < enemyCritChance;

        // 기본 던지기 배율 × 전체공격 배율
        float aoeMul = golemThrowPowerMul * 2.0f;

        // IMPORTANT:
        //  - 데미지는 NotifyEnemyAttackHit()가 호출될 때 딱 1회
        //  - 그 외 지점에서는 ApplyDamage를 절대 호출하지 않음
        ClearEnemyHit();

        // ─────────────────────────────────────────────
        // ArmEnemyHit: "도착 시점"에 실행될 데미지 로직
        // ─────────────────────────────────────────────
        ArmEnemyHit(() =>
        {
            AudioManager.I?.PlaySFX2D(SFXKey.Golem_ThrowImpact);

            for (int i = 0; i < aliveAllies.Count; i++)
            {
                var v = aliveAllies[i];
                if (v == null || v.IsDead) continue;

                int raw = Mathf.Max(1, enemyActor.GetEffectiveATK() - (v.GetEffectiveDEF() + GetTempDefBonus(v)));
                int scaled = Mathf.Max(1, Mathf.RoundToInt(raw * aoeMul));
                int dmg = isCrit ? Mathf.RoundToInt(scaled * critDamageMul) : scaled;

                if (actorViews.TryGetValue(v, out var vtf) && vtf)
                {
                    ApplyDamageAndPopup(v, vtf, dmg, isCrit);

                    bool willDie = v.hp <= 0;
                    TriggerAnim(v, willDie ? animTriggerDie : animTriggerHit);
                }
            }
        });

        // ─────────────────────────────────────────────
        // 핵심: Relay에 "토큰 + 목표 지점" 주입 (수정안 A)
        // ─────────────────────────────────────────────
        if (relay != null)
        {
            int token = CurrentEnemyHitToken;

            // 목표 지점: 아군 중앙
            Vector3 end = GetAlliesCenterPosition();

            // y 오프셋은 Relay가 최종 보정
            relay.SetThrowContext(this, token, end);
        }

        if (hud)
            hud.AppendLog($"{enemyActor.data.displayName} : 바위 던지기!(전체공격)");

        AudioManager.I?.PlaySFX2D(SFXKey.Golem_GrabRock);

        // ─────────────────────────────────────────────
        // Attack01 애니 트리거 (Golem Throw)
        // ─────────────────────────────────────────────
        TriggerAnim(enemyActor, "Golem_Throw");

        // ─────────────────────────────────────────────
        // Relay → Release → Impact Notify 대기 (Release 기준으로 타이밍 시작)
        // ─────────────────────────────────────────────
        float flight = (relay != null) ? relay.flyTime : 0.35f;

        // 1) Release 이벤트가 올 때까지 기다림 (Release 전에는 fallback 금지)
        float releaseWaitMax = 4.0f; // 애니 준비 동작이 길 수 있으니 넉넉히
        float rt = 0f;

        while (!_throwReleaseSeen && rt < releaseWaitMax)
        {
            rt += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!_throwReleaseSeen)
        {
            Debug.LogWarning("[Battle] Throw Release 이벤트가 오지 않았습니다. (Attack01 클립의 OnThrowReleaseRock 확인)");
            // Release 자체가 없으면 정상적으로 던질 수 없으니 안전 fallback 1회
            NotifyEnemyAttackHit(CurrentEnemyHitToken);
        }
        else
        {
            // 2) Release 이후: 예상 Impact 시간(Release+flyTime)까지 기다림
            float grace = 0.25f;   // 프레임 경합/오차 버퍼
            float hardMax = 3.0f;  // 무한 대기 방지
            float it = 0f;

            while (!IsEnemyHitFired && it < hardMax)
            {
                // 예상 Impact 시간이 지났으면 탈출해서 fallback 여부 결정
                if (Time.unscaledTime >= (_throwExpectedImpactTime + grace))
                    break;

                it += Time.unscaledDeltaTime;
                yield return null;
            }

            // 3) 그래도 안 오면 fallback (이때만!)
            if (!IsEnemyHitFired)
            {
                if (hitTimingDelay > 0f)
                    yield return new WaitForSeconds(hitTimingDelay);

                NotifyEnemyAttackHit(CurrentEnemyHitToken);
            }
        }

        // 공격 애니 마무리 연출 유지
        if (afterAttackAnimHold > 0f)
            yield return new WaitForSeconds(afterAttackAnimHold);

        ClearEnemyHit();

        // ─────────────────────────────────────────────
        // 카메라 복구
        // ─────────────────────────────────────────────
        if (useFixedThrowCamera)
            PopThrowCameraPose();

        cameraFrozen = false;

        // 다음 턴을 위한 상태 복구
        SetAllVisible(true);
    }

    void PushThrowCameraPose()
    {
        Camera cam = Camera.main;
        if (cam == null || throwCamPose == null) return;

        _throwCamActive = true;

        // 백업
        _camPosBackup = cam.transform.position;
        _camRotBackup = cam.transform.rotation;
        _camHasBackup = true;

        // 컨트롤러 끄기(가장 확실하게 강제)
        _camControllerWasEnabled = (cameraController != null && cameraController.enabled);
        if (cameraController != null) cameraController.enabled = false;

        // 강제 포즈
        cam.transform.SetPositionAndRotation(throwCamPose.position, throwCamPose.rotation);

        // Look 타겟이 있으면 바라보게(원치 않으면 주석 처리)
        if (throwCamLook != null)
            cam.transform.LookAt(throwCamLook.position);
    }

    void PopThrowCameraPose()
    {
        if (!_throwCamActive) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        // 원복
        if (_camHasBackup)
            cam.transform.SetPositionAndRotation(_camPosBackup, _camRotBackup);

        // 컨트롤러 복구
        if (cameraController != null)
            cameraController.enabled = _camControllerWasEnabled;

        _camHasBackup = false;
        _throwCamActive = false;
    }

    EncounterData.Candidate PickWeightedCandidate(EncounterData.Candidate[] candidates)
    {
        if (candidates == null || candidates.Length == 0) return null;

        float total = 0f;
        for (int i = 0; i < candidates.Length; i++)
        {
            var c = candidates[i];
            if (c == null || c.enemy == null) continue;
            total += Mathf.Max(0f, c.weight);
        }

        if (total <= 0f) return null;

        float r = Random.value * total;
        float acc = 0f;

        for (int i = 0; i < candidates.Length; i++)
        {
            var c = candidates[i];
            if (c == null || c.enemy == null) continue;

            acc += Mathf.Max(0f, c.weight);
            if (r <= acc)
                return c;
        }

        // 부동소수 오차 대비
        for (int i = candidates.Length - 1; i >= 0; i--)
            if (candidates[i] != null && candidates[i].enemy != null)
                return candidates[i];

        return null;
    }

    IEnumerator ExecuteSingleStrongHit(
        BattleActorRuntime caster,
        SkillData skill,
        BattleActorRuntime target,
        Transform casterTf,
        Transform targetTf)
    {
        if (caster == null || casterTf == null)
            yield break;

        if (skill == null)
        {
            if (hud) hud.AppendLog("[Skill] SkillData가 비어 있습니다.");
            yield break;
        }

        if (target == null || target.IsDead || targetTf == null)
        {
            if (hud) hud.AppendLog("[Skill] 타겟이 없습니다.");
            yield break;
        }

        // ─────────────────────────────────────────────
        // [추가] 드래곤 선방어: 스킬이 들어가기 전에 먼저 Defend 가능
        // ─────────────────────────────────────────────
        EnemyData targetSrc = null;
        if (target != null && target.isEnemy)
            TryGetEnemySrc(target, out targetSrc);

        // 확률 체크 + Defend 트리거 + 이번 액션에서 감산 플래그 세팅(_dragonDefendedThisAction 등)
        yield return TryDragonPreDefend(targetSrc, target);

        // Defend가 발동했을 때 화면에 "먼저 자세 잡는 느낌"을 주고 싶으면 아주 짧게 양보
        // (너무 길면 템포가 느려지니 0~0.08 정도 추천)
        if (_dragonDefendedThisAction)
            yield return null;

        // 스킬 연출을 더 길게 보여주기(원하면 값만 조절)
        float preHitHold = 0.25f;
        float postHitHold = 0.55f;

        // 1) 스킬 애니 트리거
        TriggerAnim(caster, string.IsNullOrEmpty(skill.animTrigger) ? "Skill" : skill.animTrigger);

        // 2) 모션을 조금 보여준 다음
        if (preHitHold > 0f)
            yield return new WaitForSeconds(preHitHold);

        // 3) 히트 타이밍(애니 이벤트 or fallback)
        yield return WaitHitByAnimEventOrFallback(casterTf, hitTimingDelay);

        // 4) VFX 생성(기존 로직 유지)
        Vector3 localOffset = new Vector3(-1f, 1f, -2f);
        Vector3 spawnPos = casterTf.TransformPoint(localOffset);
        Quaternion spawnRot = Quaternion.Euler(0f, 0f, 0f);

        if (skill.vfxPrefab != null)
        {
            var fx = Instantiate(skill.vfxPrefab, spawnPos, spawnRot);
            if (skill.vfxLifeTime > 0f)
                Destroy(fx, skill.vfxLifeTime);
        }

        // 5) 데미지 계산/적용
        int damage = CalcDamage(caster, target, skill, out bool isCritical);

        if (hud)
        {
            if (isCritical) hud.AppendLog($"CRITICAL! {caster.data.displayName} ▶ {skill.displayName} ▶ {target.data.displayName}");
            else hud.AppendLog($"{caster.data.displayName} ▶ {skill.displayName} ▶ {target.data.displayName}");
        }

        // 감산은 여기서 "한 번만" 적용 (선방어 여부는 ApplyDragonDefenseIfNeeded 내부 플래그로 판단)
        int finalDmg = ApplyDragonDefenseIfNeeded(caster, target, damage);
        ApplyDamageAndPopup(target, targetTf, finalDmg, isCritical);

        bool willDie = target.hp <= 0;

        // 방어 중이면 Hit 애니는 스킵 (Die는 허용)
        if (willDie)
        {
            TriggerAnim(target, animTriggerDie);
        }
        else
        {
            if (!IsDragonDefending(target))
                TriggerAnim(target, animTriggerHit);
        }

        // 6) 타격 후 연출 유지
        if (postHitHold > 0f)
            yield return new WaitForSeconds(postHitHold);

        // ─────────────────────────────────────────────
        // [추가] 이번 플레이어 액션(스킬) 끝났으니 드래곤 방어 자세 원복
        // ─────────────────────────────────────────────
        EndDragonDefendIfNeeded(target);
    }

    IEnumerator ExecuteAoEAllEnemies(BattleActorRuntime caster, SkillData skill, Transform casterTf)
    {
        // 1) 애니
        TriggerAnim(caster, string.IsNullOrEmpty(skill.animTrigger) ? "Skill" : skill.animTrigger);

        // 2) 히트 타이밍
        yield return WaitHitByAnimEventOrFallback(casterTf, hitTimingDelay);

        // 3) VFX: 캐스터의 AoeVfxAnchor(없으면 SkillVfxAnchor, 그것도 없으면 casterTf)
        if (skill.vfxPrefab != null)
        {
            var anchor = GetVfxAnchorOrFallback(
                casterTf,
                "AoeVfxAnchor",
                GetVfxAnchorOrFallback(casterTf, "SkillVfxAnchor", casterTf)
            );

            var fx = Instantiate(skill.vfxPrefab, anchor.position, anchor.rotation);
            if (skill.vfxLifeTime > 0f) Destroy(fx, skill.vfxLifeTime);
        }

        var aliveEnemies = enemies.Select(e => e.actor).Where(a => a != null && !a.IsDead).ToList();
        if (aliveEnemies.Count == 0) yield break;

        if (hud)
            hud.AppendLog($"{caster.data.displayName} ▶ {skill.displayName} : 적 전체 공격");

        for (int i = 0; i < aliveEnemies.Count; i++)
        {
            var victim = aliveEnemies[i];
            if (!actorViews.TryGetValue(victim, out var victimTf) || !victimTf) continue;

            int damage = CalcDamage(caster, victim, skill, out bool isCritical);
            int finalDmg = ApplyDragonDefenseIfNeeded(caster, victim, damage);
            ApplyDamageAndPopup(victim, victimTf, finalDmg, isCritical);

            bool willDie = victim.hp <= 0;
            TriggerAnim(victim, willDie ? animTriggerDie : animTriggerHit);
        }

        if (afterAttackAnimHold > 0f)
            yield return new WaitForSeconds(afterAttackAnimHold);

        // 핵심: StellarWitch AoE 연출을 원하는 시간만큼 "턴을 못 넘기게" 유지
        if (stellarWitchAoEExtraHold > 0f)
            yield return new WaitForSeconds(stellarWitchAoEExtraHold);
    }

    /// <summary>
    /// 공격자/피격자/스킬 배율을 기준으로 최종 데미지를 계산한다.
    /// 크리티컬 여부도 함께 반환한다.
    /// </summary>
    int CalcDamage(BattleActorRuntime attacker, BattleActorRuntime victim, SkillData skill, out bool isCritical)
    {
        if (attacker == null || victim == null)
        {
            isCritical = false;
            return 1;
        }

        // 런타임 ATK 사용
        int atk = attacker.GetEffectiveATK();

        // 런타임 DEF + 임시버프(아군) 사용
        int def = victim.GetEffectiveDEF() + GetTempDefBonus(victim);

        int raw = Mathf.Max(1, atk - def);

        float mul = 1f;
        if (skill != null)
            mul = Mathf.Max(0.01f, skill.power / 100f);

        int scaled = Mathf.Max(1, Mathf.RoundToInt(raw * mul));

        // 크리: 플레이어/적을 분리하려면 여기서 attacker.isEnemy로 분기 추천
        float critChance = attacker.isEnemy ? enemyCritChance : playerCritChance;
        isCritical = Random.value < critChance;

        return isCritical ? Mathf.RoundToInt(scaled * critDamageMul) : scaled;
    }

    private int GetTempDefBonus(BattleActorRuntime victim)
    {
        if (victim == null || victim.isEnemy) return 0;

        if (GameContext.I != null && _allyPartyIndex.TryGetValue(victim, out int partyIdx))
        {
            if (partyIdx >= 0 && partyIdx < GameContext.I.party.Count)
                return GameContext.I.party[partyIdx].tempDefAdd;
        }
        return 0;
    }

    // ─────────────────────────────────────────────
    // Attack Sequence (move → stand → Attack → Hit/Die → hold → return)
    // ─────────────────────────────────────────────
    IEnumerator AttackSequence(
        BattleActorRuntime attacker,
        BattleActorRuntime victim,
        Transform attackerTf,
        Transform victimTf,
        int damagePreview,
        bool isCritical,
        System.Action onHitInstant,
        string attackTriggerOverride = null,
        bool enemyHitViaNotify = false,
        bool moveToTarget = true,
        bool triggerVictimReaction = true // 피해자 Hit/Die 트리거를 여기서 할지 여부
    )
    {
        if (attackerTf == null || victimTf == null || attacker == null || victim == null)
            yield break;

        // 1) 앞으로 이동 (옵션)
        if (moveToTarget)
        {
            yield return MoveToAttackPoint(attacker, attackerTf, victimTf);

            // 2) 도착 후 잠깐 서있기
            if (beforeAttackPause > 0f)
                yield return new WaitForSeconds(beforeAttackPause);
        }
        else
        {
            // 이동 안 하면, 공격 직전에 움직임 파라미터만 정리
            SetBattleMoveState(attacker, moving: false, sprinting: false);
        }

        // 3) Attack 트리거
        SetBattleMoveState(attacker, moving: false, sprinting: false);

        string trig = string.IsNullOrEmpty(attackTriggerOverride) ? animTriggerAttack : attackTriggerOverride;
        TriggerAnim(attacker, trig);

        // 4) 히트 타이밍
        if (enemyHitViaNotify && attacker.isEnemy)
        {
            // 적 공격: "여기서는 절대 데미지를 터뜨리지 않는다"
            //   - AnimationEvent(EnemyAttackEventRelay) → BattleController.NotifyEnemyAttackHit(token)
            //   - fallback은 EnemyTurnRoutine에서만 처리
            float t = 0f;
            while (!IsEnemyHitFired && t < hitEventTimeout)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            if (logEnemyHitEventTimeout)
                Debug.Log("[EnemyAttack] Hit AnimationEvent not fired within AttackSequence wait. (fallback handled in EnemyTurnRoutine)");
        }
        else
        {
            // 기존 방식: 애니 이벤트(AttackEventReceiver) 또는 딜레이 fallback
            yield return WaitHitByAnimEventOrFallback(attackerTf, hitTimingDelay);

            // 데미지 적용
            onHitInstant?.Invoke();
        }

        // 5) 피격 애니 트리거 (단일 타겟일 때만)
        // 중요: enemyHitViaNotify 방식에서는 EnemyTurnRoutine의 ArmEnemyHit() 안에서
        // Hit/Die 트리거를 이미 쏠 가능성이 큼 → 중복 방지용 플래그로 제어
        // AttackSequence() 5) 피격 애니 트리거 부분 교체
        if (triggerVictimReaction)
        {
            if (victim != null)
            {
                bool willDie = (victim.hp <= 0);

                if (willDie)
                {
                    TriggerAnim(victim, animTriggerDie);
                }
                else
                {
                    // 방어 중이면 Hit 스킵
                    if (!IsDragonDefending(victim))
                        TriggerAnim(victim, animTriggerHit);
                }
            }
        }

        // 6) 공격 애니 홀드
        if (afterAttackAnimHold > 0f)
            yield return new WaitForSeconds(afterAttackAnimHold);

        if (afterAttackPause > 0f)
            yield return new WaitForSeconds(afterAttackPause);

        // 7) 원위치 복귀 (이동했을 때만)
        if (moveToTarget)
        {
            yield return ReturnToBase(attacker, attackerTf);

            if (forceSnapToBasePosition)
            {
                SnapActorToBase(attacker);
                if (victim != null) SnapActorToBase(victim);
            }
        }
        else
        {
            // 제자리 공격은 base 스냅만(옵션)
            if (forceSnapToBasePosition)
            {
                SnapActorToBase(attacker);
                if (victim != null) SnapActorToBase(victim);
            }
        }
    }

    EnemyAttackEventRelay GetEnemyAttackRelayFrom(Transform attackerTf)
    {
        if (attackerTf == null) return null;

        // Animator가 붙은 오브젝트에서 이벤트가 호출되므로,
        // "Animator가 있는 Transform"을 먼저 찾는 게 가장 안전함
        var anim = attackerTf.GetComponentInChildren<Animator>(true);
        if (anim != null)
        {
            // Animator가 붙은 바로 그 GO에 Relay가 있어야 이벤트를 100% 받는다
            var relayOnAnimatorGO = anim.GetComponent<EnemyAttackEventRelay>();
            if (relayOnAnimatorGO != null) return relayOnAnimatorGO;

            // 혹시 같은 계층 다른 곳에 붙였을 가능성 대비(차선)
            var relayChild = anim.GetComponentInChildren<EnemyAttackEventRelay>(true);
            if (relayChild != null) return relayChild;
        }

        // 최후 fallback
        return attackerTf.GetComponentInChildren<EnemyAttackEventRelay>(true);
    }

    void ApplyDamageAndPopup(BattleActorRuntime victim, Transform victimTf, int damage, bool isCritical)
    {
        if (victim == null) return;

        int beforeHp = victim.hp;

        victim.hp -= damage;
        if (victim.hp < 0) victim.hp = 0;

        // 방금 죽은 순간 체크 (적만)
        if (victim.isEnemy && beforeHp > 0 && victim.hp == 0)
        {
            SetAnimBoolSafe(victim, "IsDead", true);
            TriggerAnim(victim, animTriggerDie);
            MarkEnemyDyingAndHideLater(victim);

            // ★ 여기 추가: 마지막 적이면 엔딩 연출
            bool allEnemiesDead = enemies.Count == 0 || enemies.All(e => e.actor == null || e.actor.IsDead);
            if (allEnemiesDead && !_endingBattle)
            {
                StartCoroutine(CoEndBattleFadeThenExit());
            }
        }

        if (DamagePopupSpawner.I != null && victimTf != null)
        {
            Vector3 popupPos = victimTf.position + Vector3.up * 1.8f;
            DamagePopupSpawner.I.SpawnPopup(damage, popupPos, isCritical);

            if (isCritical)
            {
                Vector3 fxPos = victimTf.position + Vector3.up * 1.6f;
                DamagePopupSpawner.I.SpawnCriticalFlash(fxPos);
            }
        }

        if (hud)
        {
            hud.RefreshHPBars(allies.ToArray(), enemies.Select(e => e.actor).ToArray());
            hud.AppendLog($"{victim.data.displayName} : -{damage} HP");
        }
    }

   

    IEnumerator MoveToAttackPoint(BattleActorRuntime attacker, Transform attackerTf, Transform victimTf)
    {
        SetBattleMoveState(attacker, moving: true, sprinting: true);

        Vector3 startPos = attackerTf.position;

        Vector3 toTarget = victimTf.position - startPos;
        toTarget.y = 0f;

        Vector3 dir = (toTarget.sqrMagnitude > 0.0001f) ? toTarget.normalized : attackerTf.forward;
        float distanceToTarget = toTarget.magnitude;

        float moveDist = distanceToTarget - Mathf.Max(0f, stopDistance);
        if (minMoveDistance > 0f) moveDist = Mathf.Max(moveDist, minMoveDistance);
        moveDist = Mathf.Clamp(moveDist, 0f, maxMoveDistance);

        if (moveDist <= 0.001f)
            yield break;

        Vector3 attackPos = startPos + dir * moveDist;

        float t = 0f;
        while (t < attackMoveTime)
        {
            t += Time.deltaTime;
            float lerp = Mathf.Clamp01(t / Mathf.Max(0.0001f, attackMoveTime));
            attackerTf.position = Vector3.Lerp(startPos, attackPos, lerp);
            yield return null;
        }

        attackerTf.position = attackPos;

        SetBattleMoveState(attacker, moving: false, sprinting: false);
    }

    IEnumerator ReturnToBase(BattleActorRuntime attacker, Transform attackerTf)
    {
        if (attacker == null || attackerTf == null) yield break;
        if (!actorBasePositions.TryGetValue(attacker, out var basePos)) yield break;

        SetBattleMoveState(attacker, moving: true, sprinting: true);

        Vector3 from = attackerTf.position;
        Vector3 to = basePos;

        float t = 0f;
        while (t < attackReturnTime)
        {
            t += Time.deltaTime;
            float lerp = Mathf.Clamp01(t / Mathf.Max(0.0001f, attackReturnTime));
            attackerTf.position = Vector3.Lerp(from, to, lerp);
            yield return null;
        }

        attackerTf.position = to;

        SetBattleMoveState(attacker, moving: false, sprinting: false);
    }

    // ─────────────────────────────────────────────
    // Mini Cinematic
    // ─────────────────────────────────────────────
    IEnumerator PlayMiniCinematic()
    {
        if (cameraController != null && miniZoomDuration > 0f && Mathf.Abs(miniZoomZDelta) > 0.001f)
            cameraController.PlayMiniCinematicZoom(miniZoomZDelta, miniZoomDuration);

        if (cameraShaker != null && miniShakeIntensity > 0f && miniShakeDuration > 0f)
            cameraShaker.Shake(miniShakeIntensity, miniShakeDuration);

        if (miniSlowDuration > 0f && miniSlowTimeScale < 0.999f)
        {
            float prev = Time.timeScale;
            Time.timeScale = miniSlowTimeScale;

            float t = 0f;
            while (t < miniSlowDuration)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            Time.timeScale = prev;
        }
        else yield return null;
    }

    /// <summary>
    /// 전투 시작 시 준비된 Secret Art를 1회만 적용한다.
    /// 적용이 끝나면 각 캐릭터의 secretArtReady는 false로 초기화된다.
    /// </summary>
    void ApplySecretArtAtBattleStartOnce()
    {
        // 이미 적용했으면 중복 적용 방지
        if (secretArtApplied) return;
        secretArtApplied = true;

        var g = GameContext.I;
        if (g == null || g.party == null || g.party.Count == 0) return;

        bool anyReady = false;
        int totalHealAppliedLog = 0;
        int totalDefAppliedLog = 0;
        int maxDefTurns = 0;

        for (int i = 0; i < g.party.Count; i++)
        {
            var cr = g.party[i];
            if (cr == null || cr.data == null) continue;
            if (!cr.secretArtReady) continue;

            anyReady = true;

            var cd = cr.data;

            // 준비된 비술 타입에 따라 전투 시작 효과를 적용한다.
            switch (cd.secretArtType)
            {
                case SecretArtType.HealParty:
                    {
                        float p = Mathf.Clamp01(cd.secretArtHealPercent);
                        if (p > 0f)
                        {
                            for (int a = 0; a < allies.Count; a++)
                            {
                                var ally = allies[a];
                                if (ally == null || ally.IsDead) continue;

                                int amount = Mathf.RoundToInt(ally.maxHp * p);
                                if (amount <= 0) amount = 1;

                                int before = ally.hp;
                                ally.hp = Mathf.Min(ally.maxHp, ally.hp + amount);
                                totalHealAppliedLog += Mathf.Max(0, ally.hp - before);
                            }
                        }
                        break;
                    }

                case SecretArtType.DefBuffParty:
                    {
                        float p = Mathf.Clamp01(cd.secretArtDefPercent);
                        int turns = Mathf.Max(1, cd.secretArtDefTurns);

                        if (p > 0f)
                        {
                            for (int a = 0; a < allies.Count; a++)
                            {
                                var ally = allies[a];
                                if (ally == null || ally.IsDead) continue;

                                int baseDef = (ally.data != null ? ally.data.baseDEF : 0);

                                int add = Mathf.RoundToInt(baseDef * p);
                                if (add <= 0) add = 1;

                                ally.AddDefBonus(add, turns);

                                totalDefAppliedLog += add;
                                maxDefTurns = Mathf.Max(maxDefTurns, turns);
                            }
                        }
                        break;
                    }

                case SecretArtType.GainBattleSP:
                    {
                        int add = 2;

                        // 데이터로 조절하는 방식(위에서 필드 추가했다면)
                        add = Mathf.Max(0, cd.secretArtGainBattleSP);

                        if (add > 0 && GameContext.I != null)
                        {
                            GameContext.I.AddBattleSkillPoints(add);

                            if (hud)
                                hud.AppendLog($"[비술] 전투 시작: 스킬 포인트 +{add}");
                        }
                        break;
                    }
            }

            // Secret Art는 전투 시작 시점 1회성 효과이므로 적용 후 ready 상태를 해제한다.
            cr.secretArtReady = false;
        }

        if (!anyReady) return;

        if (hud)
        {
            if (totalHealAppliedLog > 0)
                hud.AppendLog($"[비술] 전투 시작: 파티 회복 +{totalHealAppliedLog}");

            if (totalDefAppliedLog > 0)
                hud.AppendLog($"[비술] 전투 시작: 파티 DEF 버프(+{totalDefAppliedLog} 합산, {maxDefTurns}턴)");

            hud.RefreshHPBars(allies.ToArray(), enemies.Select(e => e.actor).ToArray());
        }
    }

    // ─────────────────────────────────────────────
    // Animation helper (safe trigger)
    // ─────────────────────────────────────────────
    void TriggerAnim(BattleActorRuntime actor, string triggerName)
    {
        if (string.IsNullOrEmpty(triggerName)) return;
        if (actor == null) return;
        if (!actorViews.TryGetValue(actor, out var tf) || !tf) return;

        var anim = tf.GetComponentInChildren<Animator>(true);
        if (!anim)
        {
            if (!ignoreMissingAnimatorParams)
                Debug.LogWarning($"[BattleController] Animator가 없습니다: {actor.data?.displayName}");
            return;
        }

        if (HasTrigger(anim, triggerName))
        {
            anim.ResetTrigger(triggerName);
            anim.SetTrigger(triggerName);
        }
        else
        {
            if (!ignoreMissingAnimatorParams)
                Debug.LogWarning($"[BattleController] Animator Trigger '{triggerName}' does not exist on {actor.data?.displayName}");
        }
    }

    bool HasTrigger(Animator anim, string triggerName)
    {
        if (!anim) return false;

        var ps = anim.parameters;
        for (int i = 0; i < ps.Length; i++)
        {
            if (ps[i].type == AnimatorControllerParameterType.Trigger && ps[i].name == triggerName)
                return true;
        }
        return false;
    }

    // ─────────────────────────────────────────────
    // Battle Locomotion helper (for BlendTree Run)
    // ─────────────────────────────────────────────
    [Header("Battle Locomotion (Animator Params)")]
    public string animParamSpeed = "Speed";
    public string animParamIsMoving = "IsMoving";
    public string animParamIsSprinting = "IsSprinting";
    public string animParamGrounded = "Grounded";

    [Tooltip("Run에 해당하는 Speed 값(BlendTree 임계값에 맞게)")]
    public float battleRunSpeedValue = 1.0f;

    [Tooltip("Idle에 해당하는 Speed 값")]
    public float battleIdleSpeedValue = 0.0f;

    Animator GetAnimatorOf(BattleActorRuntime actor)
    {
        if (actor == null) return null;
        if (!actorViews.TryGetValue(actor, out var tf) || !tf) return null;
        return tf.GetComponentInChildren<Animator>(true);
    }

    bool HasParam(Animator anim, string name, AnimatorControllerParameterType type)
    {
        if (!anim || string.IsNullOrEmpty(name)) return false;

        var ps = anim.parameters;
        for (int i = 0; i < ps.Length; i++)
            if (ps[i].name == name && ps[i].type == type)
                return true;

        return false;
    }

    void SetAnimBoolSafe(BattleActorRuntime actor, string param, bool value)
    {
        var anim = GetAnimatorOf(actor);
        if (!anim) return;
        if (HasParam(anim, param, AnimatorControllerParameterType.Bool))
            anim.SetBool(param, value);
    }

    void SetAnimFloatSafe(BattleActorRuntime actor, string param, float value)
    {
        var anim = GetAnimatorOf(actor);
        if (!anim) return;
        if (HasParam(anim, param, AnimatorControllerParameterType.Float))
            anim.SetFloat(param, value);
    }

    /// <summary>
    /// 전투 이동 상태를 BlendTree에 반영 (Run/Idle)
    /// </summary>
    void SetBattleMoveState(BattleActorRuntime actor, bool moving, bool sprinting)
    {
        // 전투는 보통 지면 위, grounded는 true로 고정해도 안전
        SetAnimBoolSafe(actor, animParamGrounded, true);

        SetAnimBoolSafe(actor, animParamIsMoving, moving);
        SetAnimBoolSafe(actor, animParamIsSprinting, sprinting);

        if (moving)
        {
            // Run (너의 BlendTree 임계값에 맞게 battleRunSpeedValue 조절)
            // 네 임계값 기준: Walk=2, Run=5
            float v = sprinting ? 5f : 2f;      // sprint면 Run, 아니면 Walk
            SetAnimFloatSafe(actor, animParamSpeed, v);
        }
        else
        {
            // Idle
            SetAnimFloatSafe(actor, animParamSpeed, battleIdleSpeedValue);
        }
    }

    /// <summary>
    /// 전투 중 변경된 HP / MaxHP / SP 상태를
    /// 원본 GameContext.party(CharacterRuntime) 쪽에 반영한다.
    /// 전투 종료 후 탐험 씬으로 복귀할 때 사용하는 동기화 함수이다.
    /// </summary>
    private void SyncBattlePartyStateToGameContext()
    {
        if (GameContext.I == null || GameContext.I.party == null)
            return;

        for (int i = 0; i < allies.Count; i++)
        {
            var br = allies[i];
            if (br == null) continue;

            if (!_allyPartyIndex.TryGetValue(br, out int partyIdx)) continue;
            if (partyIdx < 0 || partyIdx >= GameContext.I.party.Count) continue;

            var cr = GameContext.I.party[partyIdx];
            if (cr == null) continue;

            cr.hp = Mathf.Max(0, br.hp);
            cr.maxHp = Mathf.Max(1, br.maxHp);
            cr.sp = Mathf.Max(0, br.sp);
        }
    }

    // ─────────────────────────────────────────────
    // Snap base position
    // ─────────────────────────────────────────────
    void SnapActorToBase(BattleActorRuntime actor)
    {
        if (actor == null) return;
        if (!actorViews.TryGetValue(actor, out var tf) || !tf) return;
        if (!actorBasePositions.TryGetValue(actor, out var basePos)) return;

        tf.position = basePos;

        if (actorBaseRotations.TryGetValue(actor, out var baseRot))
            tf.rotation = baseRot;
    }

    // ─────────────────────────────────────────────
    // Visibility
    // ─────────────────────────────────────────────
    void SetAllVisible(bool on)
    {
        foreach (var a in allies)
            SetActorVisible(a, on && a != null && !a.IsDead);

        foreach (var e in enemies.Select(x => x.actor))
        {
            if (e == null) continue;

            bool keepVisibleEvenIfDead = _dyingEnemies.Contains(e);
            SetActorVisible(e, on && (!e.IsDead || keepVisibleEvenIfDead));
        }
    }

    void HideOtherAlliesExcept(BattleActorRuntime keep)
    {
        foreach (var a in allies)
        {
            if (a == null) continue;
            bool on = (a == keep) && !a.IsDead;
            SetActorVisible(a, on);
        }

        foreach (var e in enemies.Select(x => x.actor))
        {
            if (e == null) continue;

            bool keepVisibleEvenIfDead = _dyingEnemies.Contains(e);
            SetActorVisible(e, !e.IsDead || keepVisibleEvenIfDead);
        }
    }

    void SetActorVisible(BattleActorRuntime actor, bool on)
    {
        if (actor == null) return;
        if (!actorViews.TryGetValue(actor, out var tf) || !tf) return;

        var renderers = tf.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].enabled = on;
    }

    public void NotifyThrowReleased(int token, float flyTime)
    {
        _throwReleaseSeen = true;
        _throwReleaseToken = token;

        // Release 순간부터 flyTime 뒤에 Impact가 “정상”
        _throwExpectedImpactTime = Time.unscaledTime + Mathf.Max(0.05f, flyTime);

#if UNITY_EDITOR
        Debug.Log($"[Battle] Throw Released token={token}, expectedImpactAt={_throwExpectedImpactTime:0.00}");
#endif
    }

    void MarkEnemyDyingAndHideLater(BattleActorRuntime enemy)
    {
        if (enemy == null || !enemy.isEnemy) return;
        if (_dyingEnemies.Contains(enemy)) return;

        _dyingEnemies.Add(enemy);
        StartCoroutine(CoHideEnemyAfterDie(enemy, enemyDieVisibleTime));
    }

    IEnumerator CoHideEnemyAfterDie(BattleActorRuntime enemy, float t)
    {
        // Die 애니가 보일 시간 확보
        yield return new WaitForSeconds(t);

        _dyingEnemies.Remove(enemy);

        // 최종적으로 숨김
        SetActorVisible(enemy, false);
    }

    public void ApplyBreathDamageFromCollider(Collider hit, int damage)
    {
        if (hit == null) return;

        Transform t = hit.transform;
        while (t != null)
        {
            if (viewToActor.TryGetValue(t, out var actor) && actor != null && !actor.IsDead)
            {
                if (!actor.isEnemy)
                {
                    if (actorViews.TryGetValue(actor, out var tf) && tf != null)
                        ApplyDamageAndPopup(actor, tf, Mathf.Max(1, damage), false);
                }
                return;
            }
            t = t.parent;
        }
    }

    bool TryGetEnemySrc(BattleActorRuntime enemyActor, out EnemyData src)
    {
        src = null;
        if (enemyActor == null) return false;

        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i].actor == enemyActor)
            {
                src = enemies[i].src;
                return src != null;
            }
        }
        return false;
    }

    void PlayBattleBgmForCurrentEncounter()
    {
        bool useBossBgm = false;

        for (int i = 0; i < enemies.Count; i++)
        {
            var src = enemies[i].src;
            if (src == null) continue;

            // 준보스(Elite) / 보스(Boss)는 Boss BGM
            if (src.rank == EnemyRank.Elite || src.rank == EnemyRank.Boss)
            {
                useBossBgm = true;
                break;
            }
        }

        if (useBossBgm)
            AudioManager.I?.PlayBGM(BGMKey.BattleBoss);
        else
            AudioManager.I?.PlayBGM(BGMKey.BattleNormal);
    }

    string GetPlayerBasicAttackSfxKey(BattleActorRuntime actor)
    {
        if (actor == null || actor.data == null) return null;

        string n = actor.data.name;

        if (n.Contains("Kisora")) return SFXKey.Basic_Kisora;
        if (n.Contains("StellarWitch")) return SFXKey.Basic_StellarWitch;
        if (n.Contains("Tribi")) return SFXKey.Basic_Tribi;

        return null;
    }

    string GetPlayerSkillSfxKey(BattleActorRuntime actor)
    {
        if (actor == null || actor.data == null) return null;

        string n = actor.data.name;

        if (!string.IsNullOrEmpty(n))
        {
            if (n.Contains("Kisora")) return SFXKey.Skill_Kisora;
            if (n.Contains("StellarWitch")) return SFXKey.Skill_StellarWitch;
            if (n.Contains("Tribi")) return SFXKey.Skill_Tribi;
        }

        return null;
    }

    bool IsFinalBossBattle()
    {
        for (int i = 0; i < enemies.Count; i++)
        {
            var src = enemies[i].src;
            if (src == null) continue;

            bool isDragon =
                src.rank == EnemyRank.Boss &&
                !string.IsNullOrEmpty(src.displayName) &&
                src.displayName.Contains("드래곤");

            if (isDragon)
                return true;
        }

        return false;
    }

}
