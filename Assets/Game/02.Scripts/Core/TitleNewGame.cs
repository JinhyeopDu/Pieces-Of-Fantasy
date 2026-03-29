using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class TitleNewGame : MonoBehaviour
{
    [Header("Starters")]
    [SerializeField] private CharacterData starter;   // 필수(1번 캐릭터)
    [SerializeField] private CharacterData kisora;    // 선택(2번 캐릭터)
    [SerializeField] private CharacterData stellarWitch; // 선택(3번 캐릭터) - 변수명은 소문자 권장

    [SerializeField] private UnityEngine.UI.Button continueButton;

    void Start()
    {
        AudioManager.I?.ForceApplyCurrentSettings();
        AudioManager.I?.PlayBGM(BGMKey.Title);

        if (continueButton != null)
            continueButton.interactable = SaveManager.HasSave();
    }

    /// <summary>
    /// 새 게임을 시작한다.
    /// 스타터 캐릭터를 파티에 넣고 GameContext를 초기화한 뒤 Exploration 씬으로 이동한다.
    /// </summary>
    public void OnClickNewGame()
    {

        EnsureGameContext();

        var starters = new List<CharacterData>(3);
        if (starter != null) starters.Add(starter);
        if (kisora != null) starters.Add(kisora);
        if (stellarWitch != null) starters.Add(stellarWitch);

        // 1) 스타터가 비어있으면 여기서 바로 중단 (가장 흔한 "진입 즉시 튕김" 원인)
        if (starters.Count == 0)
        {
            Debug.LogError("[TitleNewGame] No starter characters assigned. (starter/kisora/stellarWitch are all null)");
            return;
        }

        // 2) 새 게임 세팅
        GameContext.I.ResetForNewGame();
        GameContext.I.StartNewGame(starters);
        GameContext.I.EnsureActiveIsAlive();

        // 새 게임 직후 퀘스트 다시 시작
        QuestManager.I?.EnsureQuestInitialized();

        // 3) 진짜로 hp/레벨이 정상인지 로그로 확정 (원인 추적 핵심)
        if (GameContext.I.party == null || GameContext.I.party.Count == 0)
        {
            Debug.LogError("[TitleNewGame] StartNewGame finished but party is empty/null.");
            return;
        }

#if UNITY_EDITOR
        for (int i = 0; i < GameContext.I.party.Count; i++)
        {
            var cr = GameContext.I.party[i];
            Debug.Log($"[TitleNewGame] party[{i}] {cr?.data?.name} lv={cr?.level} hp={cr?.hp}/{cr?.maxHp} sp={cr?.sp}");
        }
#endif

        // 4) 씬이 Build Settings에 없으면 LoadScene이 실패할 수 있음
        if (!Application.CanStreamedLevelBeLoaded("Exploration"))
        {
            Debug.LogError("[TitleNewGame] Scene 'Exploration' is not in Build Settings (or name mismatch).");
            return;
        }

        SceneManager.LoadScene("Exploration");
    }

    /// <summary>
    /// 저장 데이터를 불러와 이어하기를 시작한다.
    /// 퀘스트 복원 실패 여부를 확인한 뒤 저장된 씬으로 이동한다.
    /// </summary>
    public void OnClickContinue()
    {
        AudioManager.I?.PlaySFX2D(SFXKey.UI_Click);

        EnsureGameContext();

        if (GameContext.I == null)
        {
            Debug.LogError("[TitleNewGame] Continue failed: GameContext.I is null.");
            return;
        }

        bool ok = GameContext.I.LoadGameFromSave();

        if (!ok)
        {
            Debug.LogWarning("[TitleNewGame] No save file found.");
            return;
        }

        var g = GameContext.I;
#if UNITY_EDITOR
        Debug.Log(
            $"[TitleNewGame] After Load | currentQuest={(g.currentQuest != null ? g.currentQuest.questId : "NULL")} | " +
            $"hasProgress={(g.currentQuestProgress != null)} | " +
            $"progressValue={(g.currentQuestProgress != null ? g.currentQuestProgress.currentValue : -1)} | " +
            $"allDone={g.allQuestsCompleted} | " +
            $"questRestoreFailed={g.lastLoadHadQuestRestoreFailure}"
        );
#endif

        // 저장에 퀘스트가 있었는데 복원 실패한 경우: 절대 fallback 금지
        if (g.lastLoadHadQuestRestoreFailure)
        {
            Debug.LogError("[TitleNewGame] Continue aborted: quest restore failed. Fix GameDataRegistry reference before continuing.");
            return;
        }

        // 정말 아무 퀘스트도 없는 경우에만 보정
        if (!g.allQuestsCompleted && g.currentQuest == null && g.currentQuestProgress == null)
        {
#if UNITY_EDITOR
            Debug.Log("[TitleNewGame] No restored quest found. Calling EnsureQuestInitialized as fallback.");
#endif
            QuestManager.I?.EnsureQuestInitialized();
        }

        var data = SaveManager.Load();

        if (data == null || string.IsNullOrEmpty(data.currentSceneName))
        {
            Debug.LogWarning("[TitleNewGame] Save has no currentSceneName. Loading Exploration.");
            SceneManager.LoadScene("Exploration");
            return;
        }

#if UNITY_EDITOR
        Debug.Log($"[TitleNewGame] Loading scene: {data.currentSceneName}");
#endif
        SceneManager.LoadScene(data.currentSceneName);
    }

    /// <summary>
    /// GameContext가 준비되어 있는지 확인한다.
    /// 현재 구조에서는 런타임 생성 대신, Boot 씬에 배치된 GameContext 존재를 전제로 한다.
    /// </summary>
    private void EnsureGameContext()
    {
        // 1) 이미 살아있으면 OK
        if (GameContext.I != null) return;

        // 2) 씬 안에서 찾기
        var existing = Object.FindObjectOfType<GameContext>(includeInactive: true);
        if (existing != null)
        {
            Debug.LogWarning("[TitleNewGame] Found GameContext in scene but GameContext.I is null. Check GameContext.Awake().");
            return;
        }

        // 3) 이제는 런타임 빈 생성 금지
        Debug.LogError("[TitleNewGame] GameContext not found. Please place PF_GameContext in Boot scene and assign dataRegistry.");
    }

    /// <summary>
    /// 게임 종료 버튼 처리.
    /// 에디터에서는 Play Mode를 종료하고, 빌드에서는 애플리케이션을 종료한다.
    /// </summary>
    public void OnClickQuitGame()
    {
        AudioManager.I?.PlaySFX2D(SFXKey.UI_Click);

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
    }
}
