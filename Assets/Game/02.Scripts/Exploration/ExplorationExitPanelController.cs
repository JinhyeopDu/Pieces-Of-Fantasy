using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ExplorationExitPanelController : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject panelRoot;

    [Header("Buttons")]
    [SerializeField] private Button btnExit;
    [SerializeField] private Button btnCancel;

    [Header("Optional UI refs to check before opening")]
    [SerializeField] private SettingsPanelController settingsPanel;
    [SerializeField] private InventoryController inventoryController;
    [SerializeField] private CharacterScreenController characterScreenController;
    [SerializeField] private MiniMapController miniMapController;
    [SerializeField] private QuestPanelController questPanel;
    [SerializeField] private ExplorationUIHotkeys explorationUIHotkeys;

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

    private void Awake()
    {
        if (settingsPanel == null)
            settingsPanel = FindFirstObjectByType<SettingsPanelController>(FindObjectsInactive.Include);

        if (inventoryController == null)
            inventoryController = FindFirstObjectByType<InventoryController>();

        if (characterScreenController == null)
            characterScreenController = FindFirstObjectByType<CharacterScreenController>();

        if (miniMapController == null)
            miniMapController = FindFirstObjectByType<MiniMapController>(FindObjectsInactive.Include);

        if (questPanel == null)
            questPanel = FindFirstObjectByType<QuestPanelController>(FindObjectsInactive.Include);

        if (explorationUIHotkeys == null)
            explorationUIHotkeys = FindFirstObjectByType<ExplorationUIHotkeys>();

        if (btnExit != null)
        {
            btnExit.onClick.RemoveListener(OnClickExit);
            btnExit.onClick.AddListener(OnClickExit);
        }

        if (btnCancel != null)
        {
            btnCancel.onClick.RemoveListener(Close);
            btnCancel.onClick.AddListener(Close);
        }

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void Update()
    {
        if (Keyboard.current == null) return;
        if (!Keyboard.current.escapeKey.wasPressedThisFrame) return;

        // 같은 프레임에 다른 UI가 ESC를 소비했으면 종료 패널은 열지 않음
        if (explorationUIHotkeys != null && explorationUIHotkeys.ConsumedEscapeThisFrame)
            return;

        // 1) 설정창이 열려 있으면 종료 패널은 열지 않음
        if (settingsPanel != null && settingsPanel.IsPanelOpen())
            return;

        // 2) 인벤이 열려 있으면 종료 패널은 열지 않음
        if (inventoryController != null && inventoryController.IsOpen)
            return;

        // 3) 캐릭터창이 열려 있으면 종료 패널은 열지 않음
        if (characterScreenController != null && characterScreenController.IsOpen)
            return;

        // 4) 퀘스트창이 열려 있으면 종료 패널은 열지 않음
        if (questPanel != null && questPanel.IsOpen)
            return;

        // 5) 월드맵이 열려 있으면 종료 패널은 열지 않음
        if (miniMapController != null && miniMapController.IsWorldMapOpen)
            return;

        // 6) 아무 UI도 안 열려 있을 때만 탐험 종료 패널 토글
        if (IsOpen) Close();
        else Open();
    }

    public void Open()
    {
        if (panelRoot == null || panelRoot.activeSelf) return;

        panelRoot.SetActive(true);
        AudioManager.I?.PlaySFX2D(SFXKey.UI_Open);

        GameContext.I?.SetUIBlockingLook(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Close()
    {
        if (panelRoot == null || !panelRoot.activeSelf) return;

        panelRoot.SetActive(false);
        AudioManager.I?.PlaySFX2D(SFXKey.UI_Close);

        GameContext.I?.SetUIBlockingLook(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OnClickExit()
    {
        AudioManager.I?.PlaySFX2D(SFXKey.UI_Click);

        GameContext.I?.SaveGame();

        // 1차 포트폴리오 기준: 타이틀로 복귀
        if (SceneFader.I != null)
            SceneFader.I.LoadSceneWithFade("Title");
        else
            SceneManager.LoadScene("Title");
    }
}