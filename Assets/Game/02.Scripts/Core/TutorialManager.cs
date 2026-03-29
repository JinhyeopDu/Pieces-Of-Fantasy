using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager I { get; private set; }

    [Header("UI")]
    public TutorialPanelController panel;

    [Header("Move Tutorial")]
    [SerializeField] private float requiredMoveTime = 2.0f;
    [SerializeField] private float moveVelocityThreshold = 0.1f;

    private Coroutine _flowCo;
    private Coroutine _endCo;

    private float _moveAccumTime = 0f;
    private PlayerControllerHumanoid _cachedPlayer;

    // 전투 씬에서 돌아온 직후 튜토리얼 가이드를 즉시 재표시하지 않기 위한 플래그
    private bool _suppressGuideOnceAfterBattle = false;

    void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (I == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        TryBindPanelInCurrentScene();
        ResetSceneLocalState();
        CheckAndShowNextTutorial();
    }

    void Update()
    {
        CheckMoveTutorialProgress();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryBindPanelInCurrentScene();
        ResetSceneLocalState();

        // Battle 씬에 들어가면, 복귀 후 Exploration 첫 프레임에서
        // 전투 튜토리얼 화살표를 즉시 다시 세팅하지 않도록 예약
        if (scene.name == "Battle")
        {
            _suppressGuideOnceAfterBattle = true;

            TutorialHighlighter.I?.ClearHighlights();
            TutorialPlayerGuideController.I?.ClearTargets();
        }

        CheckAndShowNextTutorial();
    }

    void TryBindPanelInCurrentScene()
    {
        panel = FindObjectOfType<TutorialPanelController>(true);
    }

    void ResetSceneLocalState()
    {
        _cachedPlayer = null;

        var gc = GameContext.I;
        if (gc == null) return;

        if (!gc.tutorialMoveDone)
            _moveAccumTime = 0f;
    }

    // =========================
    // 이동 튜토리얼 진행도
    // =========================
    void CheckMoveTutorialProgress()
    {
        var gc = GameContext.I;
        if (gc == null) return;

        if (!IsMoveStep()) return;

        if (SceneManager.GetActiveScene().name != "Exploration")
            return;

        var player = GetPlayer();
        if (player == null) return;

        if (IsPlayerActuallyMoving(player))
        {
            _moveAccumTime += Time.deltaTime;

            if (_moveAccumTime >= requiredMoveTime)
                CompleteMoveTutorial();
        }
    }

    PlayerControllerHumanoid GetPlayer()
    {
        if (_cachedPlayer != null && _cachedPlayer.gameObject.activeInHierarchy)
            return _cachedPlayer;

        _cachedPlayer = FindObjectOfType<PlayerControllerHumanoid>();
        return _cachedPlayer;
    }

    bool IsPlayerActuallyMoving(PlayerControllerHumanoid player)
    {
        if (player == null) return false;

        var cc = player.GetComponent<CharacterController>();
        if (cc == null) return false;

        Vector3 v = cc.velocity;
        v.y = 0f;

        return v.magnitude > moveVelocityThreshold;
    }

    // =========================
    // QST_01 상태 헬퍼
    // =========================
    private bool IsFirstQuestActive()
    {
        var gc = GameContext.I;
        if (gc == null) return false;
        if (gc.currentQuest == null) return false;
        if (gc.currentQuestProgress == null) return false;

        return gc.currentQuest.questId == "QST_01";
    }

    private int GetFirstQuestValue()
    {
        var gc = GameContext.I;
        if (gc == null || gc.currentQuestProgress == null) return 0;
        if (!IsFirstQuestActive()) return 0;

        return gc.currentQuestProgress.currentValue;
    }

    private bool IsFirstQuestCompletedButRewardNotClaimed()
    {
        var gc = GameContext.I;
        if (gc == null) return false;
        if (!IsFirstQuestActive()) return false;

        return gc.currentQuestProgress.isCompleted && !gc.currentQuestProgress.rewardClaimed;
    }

    private bool IsFirstQuestRewardClaimed()
    {
        var gc = GameContext.I;
        if (gc == null) return false;

        // 1) 아직 QST_01이 현재 퀘스트라면 현재 진행도에서 직접 확인
        if (gc.currentQuest != null &&
            gc.currentQuest.questId == "QST_01" &&
            gc.currentQuestProgress != null)
        {
            return gc.currentQuestProgress.rewardClaimed;
        }

        // 2) 이미 다음 퀘스트로 넘어갔다면 completedQuestIds로 판단
        return gc.completedQuestIds != null && gc.completedQuestIds.Contains("QST_01");
    }

    // =========================
    // 공통 흐름 제어
    // =========================
    private void StopRunningFlows()
    {
        if (_flowCo != null)
        {
            StopCoroutine(_flowCo);
            _flowCo = null;
        }

        if (_endCo != null)
        {
            StopCoroutine(_endCo);
            _endCo = null;
        }
    }

    private void StartNextTutorialWithDelay()
    {
        if (_flowCo != null)
            StopCoroutine(_flowCo);

        _flowCo = StartCoroutine(CoShowNextWithDelay());
    }

    private IEnumerator CoShowNextWithDelay()
    {
        yield return new WaitForSeconds(3f);
        _flowCo = null;
        CheckAndShowNextTutorial();
    }

    private void ShowCompleteMessage()
    {
        if (panel == null) return;

        string[] messages =
        {
            "Good! 이제 다음 단계로 넘어가볼까요?",
            "잘하셨어요! 다음으로 진행해봅시다.",
            "완벽합니다. 계속 진행해볼까요?"
        };

        string msg = messages[Random.Range(0, messages.Length)];

        TutorialHighlighter.I?.ClearHighlights();
        panel.Show("완료", "좋아요.", msg);
    }

    private IEnumerator CoShowTutorialEndMessage()
    {
        TutorialHighlighter.I?.ClearHighlights();

        if (panel != null)
        {
            panel.Show(
                "튜토리얼 종료",
                "튜토리얼을 종료합니다.",
                "이제 퀘스트 진행에 맞춰 자유롭게 진행해보세요."
            );
        }

        yield return new WaitForSeconds(4f);

        panel?.Hide();
        _endCo = null;
    }

    // =========================
    // 메인 표시 로직
    // =========================
    public void CheckAndShowNextTutorial()
    {
        var gc = GameContext.I;
        if (gc == null || panel == null)
            return;

        string sceneName = SceneManager.GetActiveScene().name;

        if (sceneName != "Exploration" && sceneName != "Battle")
        {
            TutorialHighlighter.I?.ClearHighlights();
            TutorialPlayerGuideController.I?.ClearTargets();
            panel.Hide();
            return;
        }

        // 전투에서 방금 돌아온 직후 Exploration에서는
        // 전투 튜토리얼 화살표를 즉시 다시 띄우지 않는다.
        if (sceneName == "Exploration" && _suppressGuideOnceAfterBattle)
        {
            _suppressGuideOnceAfterBattle = false;

            TutorialHighlighter.I?.ClearHighlights();
            TutorialPlayerGuideController.I?.ClearTargets();

            // 패널은 유지하거나 숨김 중 선택 가능.
            // 지금은 안내 패널도 잠깐 숨기는 쪽이 UX가 더 자연스럽다.
            panel.Hide();
            return;
        }

        // 튜토리얼이 이미 완전히 끝났으면 더 이상 종료 안내를 반복하지 않음
        if (gc.tutorialInventoryDone)
        {
            StopRunningFlows();
            TutorialHighlighter.I?.ClearHighlights();
            TutorialPlayerGuideController.I?.ClearTargets();
            panel.Hide();
            return;
        }

        // 1) 이동
        if (!gc.tutorialMoveDone)
        {
            StopRunningFlows();
            TutorialHighlighter.I?.ClearHighlights();
            TutorialPlayerGuideController.I?.ClearTargets();

            panel.Show(
                "이동",
                "W A S D 키로 캐릭터를 이동할 수 있습니다.",
                $"조금 더 움직여보세요. ({requiredMoveTime:0.#}초 이상 이동)"
            );
            return;
        }

        // 2) 달리기
        if (!gc.tutorialSprintDone)
        {
            StopRunningFlows();
            TutorialHighlighter.I?.ClearHighlights();
            TutorialPlayerGuideController.I?.ClearTargets();

            panel.Show(
                "달리기",
                "Shift 키를 누른 상태로 이동하면 더 빠르게 달릴 수 있습니다.",
                "Shift + 이동 입력으로 한 번 달려보세요."
            );
            return;
        }

        // 3) 퀘스트 창 열기
        if (!gc.tutorialQuestDone)
        {
            StopRunningFlows();
            TutorialHighlighter.I?.ClearHighlights();
            TutorialPlayerGuideController.I?.ClearTargets();

            panel.Show(
                "퀘스트",
                "Q 키로 퀘스트 창을 열 수 있습니다.",
                "첫 번째 퀘스트를 확인해보세요."
            );
            return;
        }

        // 4) QST_01 진행 중 - 광석 20개 미만
        if (IsFirstQuestActive() && GetFirstQuestValue() < 20)
        {
            StopRunningFlows();
            TutorialHighlighter.I?.HighlightTargets(TutorialTargetType.Ore);

            var orePlayer = GetPlayer();
            var oreTarget = (TutorialHighlighter.I != null && orePlayer != null)
                ? TutorialHighlighter.I.GetNearestTargetPoint(
                    TutorialTargetType.Ore,
                    orePlayer.transform.position
                  )
                : null;

            TutorialPlayerGuideController.I?.SetTargets(
              orePlayer != null ? orePlayer.transform : null,
              oreTarget,
              TutorialTargetType.Ore
          );

            panel.Show(
                "광석 채집",
                "첫 번째 퀘스트 목표는 광석 20개 획득입니다.",
                $"광석을 계속 채집해보세요. ({GetFirstQuestValue()}/20)"
            );
            return;
        }

        // 5) QST_01 완료, 보상 미수령
        if (IsFirstQuestCompletedButRewardNotClaimed())
        {
            StopRunningFlows();
            TutorialHighlighter.I?.ClearHighlights();
            TutorialPlayerGuideController.I?.ClearTargets();

            panel.Show(
                "퀘스트 보상",
                "첫 번째 퀘스트 목표를 달성했습니다.",
                "Q 키로 퀘스트 창을 열고 보상을 수령하세요."
            );
            return;
        }

        // 6) QST_01 보상 수령 후 -> 캐릭터 메뉴
        if (IsFirstQuestRewardClaimed() && !gc.tutorialCharacterOpenDone)
        {
            StopRunningFlows();
            TutorialHighlighter.I?.ClearHighlights();
            TutorialPlayerGuideController.I?.ClearTargets();

            panel.Show(
                "캐릭터 메뉴",
                "C 키로 캐릭터 창을 열 수 있습니다.",
                "캐릭터의 상태와 성장 정보를 확인해보세요."
            );
            return;
        }

        // 7) 레벨 업
        if (!gc.tutorialLevelUpDone)
        {
            StopRunningFlows();
            TutorialHighlighter.I?.ClearHighlights();
            TutorialPlayerGuideController.I?.ClearTargets();

            panel.Show(
                "레벨 업",
                "캐릭터 창에서 재료를 선택해 레벨 업을 진행할 수 있습니다.",
                "방금 수집한 재료를 활용해 캐릭터를 성장시켜보세요."
            );
            return;
        }

        // 8) 비술
        if (!gc.tutorialSecretArtDone)
        {
            StopRunningFlows();
            TutorialHighlighter.I?.ClearHighlights();
            TutorialPlayerGuideController.I?.ClearTargets();

            panel.Show(
                "비술",
                "각 캐릭터는 탐험 중 사용할 수 있는 고유 비술을 가지고 있습니다.",
                "2번 키로 키소라로 교체한 뒤, E 키를 눌러 비술을 사용해보세요."
            );
            return;
        }

        // 9) 전투
        if (!gc.tutorialBattleDone)
        {
            StopRunningFlows();
            TutorialHighlighter.I?.HighlightTargets(TutorialTargetType.MushroomEnemy);

            var battlePlayer = GetPlayer();
            var battleTarget = (TutorialHighlighter.I != null && battlePlayer != null)
                ? TutorialHighlighter.I.GetNearestTargetPoint(
                    TutorialTargetType.MushroomEnemy,
                    battlePlayer.transform.position
                  )
                : null;

            TutorialPlayerGuideController.I?.SetTargets(
                battlePlayer != null ? battlePlayer.transform : null,
                battleTarget,
                TutorialTargetType.MushroomEnemy
            );

            panel.Show(
                "전투",
                "적과 접촉하면 전투가 시작됩니다.",
                "처음 전투는 버섯몬스터를 추천합니다."
            );
            return;
        }

        // 10) 인벤토리
        if (!gc.tutorialInventoryDone)
        {
            StopRunningFlows();
            TutorialHighlighter.I?.ClearHighlights();
            TutorialPlayerGuideController.I?.ClearTargets();

            panel.Show(
                "인벤토리",
                "R 키로 인벤토리를 열 수 있습니다.",
                "획득한 아이템과 소모품을 확인해보세요."
            );
            return;
        }

        // 11) 종료
        TutorialHighlighter.I?.ClearHighlights();
        TutorialPlayerGuideController.I?.ClearTargets();

        if (_endCo == null)
        {
            _endCo = StartCoroutine(CoShowTutorialEndMessage());
        }
    }

    // =========================
    // 완료 처리
    // =========================
    public void CompleteMoveTutorial()
    {
        var gc = GameContext.I;
        if (gc == null || !IsMoveStep()) return;

        gc.tutorialMoveDone = true;

        ShowCompleteMessage();
        StartNextTutorialWithDelay();
    }

    public void CompleteQuestTutorial()
    {
        var gc = GameContext.I;
        if (gc == null || !IsQuestOpenStep()) return;

        gc.tutorialQuestDone = true;

        ShowCompleteMessage();
        StartNextTutorialWithDelay();
    }

    public void CompleteCharacterOpenTutorial()
    {
        var gc = GameContext.I;
        if (gc == null || !IsCharacterOpenStep()) return;

        gc.tutorialCharacterOpenDone = true;

        ShowCompleteMessage();
        StartNextTutorialWithDelay();
    }

    public void CompleteLevelUpTutorial()
    {
        var gc = GameContext.I;
        if (gc == null || !IsLevelUpStep()) return;

        gc.tutorialLevelUpDone = true;

        ShowCompleteMessage();
        StartNextTutorialWithDelay();
    }

    public void CompleteBattleTutorialIfNeeded()
    {
        var gc = GameContext.I;
        if (gc == null || !IsBattleStep()) return;

        gc.tutorialBattleDone = true;

        ShowCompleteMessage();
        StartNextTutorialWithDelay();
    }

    public void CompleteInventoryTutorial()
    {
        var gc = GameContext.I;
        if (gc == null || !IsInventoryStep()) return;

        gc.tutorialInventoryDone = true;

        TutorialHighlighter.I?.ClearHighlights();
        StopRunningFlows();

        if (_endCo == null)
            _endCo = StartCoroutine(CoShowTutorialEndMessage());
    }

    public void CompleteSecretArtTutorial()
    {
        var gc = GameContext.I;
        if (gc == null || !IsSecretArtStep()) return;

        gc.tutorialSecretArtDone = true;

        ShowCompleteMessage();
        StartNextTutorialWithDelay();
    }

    public void CompleteSprintTutorial()
    {
        var gc = GameContext.I;
        if (gc == null || !IsSprintStep()) return;

        gc.tutorialSprintDone = true;

        ShowCompleteMessage();
        StartNextTutorialWithDelay();
    }

    // =========================
    // 단계 판정
    // =========================
    private bool IsMoveStep()
    {
        var gc = GameContext.I;
        return gc != null && !gc.tutorialMoveDone;
    }

    private bool IsSprintStep()
    {
        var gc = GameContext.I;
        return gc != null
            && gc.tutorialMoveDone
            && !gc.tutorialSprintDone;
    }

    private bool IsQuestOpenStep()
    {
        var gc = GameContext.I;
        return gc != null
            && gc.tutorialMoveDone
            && gc.tutorialSprintDone
            && !gc.tutorialQuestDone;
    }

    private bool IsCharacterOpenStep()
    {
        var gc = GameContext.I;
        return gc != null
            && gc.tutorialMoveDone
            && gc.tutorialSprintDone
            && gc.tutorialQuestDone
            && IsFirstQuestRewardClaimed()
            && !gc.tutorialCharacterOpenDone;
    }

    private bool IsLevelUpStep()
    {
        var gc = GameContext.I;
        return gc != null
            && gc.tutorialMoveDone
            && gc.tutorialSprintDone
            && gc.tutorialQuestDone
            && IsFirstQuestRewardClaimed()
            && gc.tutorialCharacterOpenDone
            && !gc.tutorialLevelUpDone;
    }

    private bool IsSecretArtStep()
    {
        var gc = GameContext.I;
        return gc != null
            && gc.tutorialMoveDone
            && gc.tutorialSprintDone
            && gc.tutorialQuestDone
            && IsFirstQuestRewardClaimed()
            && gc.tutorialCharacterOpenDone
            && gc.tutorialLevelUpDone
            && !gc.tutorialSecretArtDone;
    }

    private bool IsBattleStep()
    {
        var gc = GameContext.I;
        return gc != null
            && gc.tutorialMoveDone
            && gc.tutorialSprintDone
            && gc.tutorialQuestDone
            && IsFirstQuestRewardClaimed()
            && gc.tutorialCharacterOpenDone
            && gc.tutorialLevelUpDone
            && gc.tutorialSecretArtDone
            && !gc.tutorialBattleDone;
    }

    private bool IsInventoryStep()
    {
        var gc = GameContext.I;
        return gc != null
            && gc.tutorialMoveDone
            && gc.tutorialSprintDone
            && gc.tutorialQuestDone
            && IsFirstQuestRewardClaimed()
            && gc.tutorialCharacterOpenDone
            && gc.tutorialLevelUpDone
            && gc.tutorialBattleDone
            && !gc.tutorialInventoryDone;
    }

}