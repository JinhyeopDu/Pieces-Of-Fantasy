using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BattlePausePanelController : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject panelRoot;

    [Header("Buttons")]
    [SerializeField] private Button btnAbortBattle;
    [SerializeField] private Button btnResume;

    [Header("Optional UI refs")]
    [SerializeField] private SettingsPanelController settingsPanel;

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

    private void Awake()
    {
        if (btnAbortBattle != null)
        {
            btnAbortBattle.onClick.RemoveListener(OnClickAbortBattle);
            btnAbortBattle.onClick.AddListener(OnClickAbortBattle);
        }

        if (btnResume != null)
        {
            btnResume.onClick.RemoveListener(Close);
            btnResume.onClick.AddListener(Close);
        }

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void Update()
    {
        if (Keyboard.current == null) return;
        if (!Keyboard.current.escapeKey.wasPressedThisFrame) return;

        // 설정창이 열려 있으면 설정창 먼저 닫기
        if (settingsPanel != null && settingsPanel.IsPanelOpen())
        {
            settingsPanel.Close();
            return;
        }

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
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void OnClickAbortBattle()
    {
        AudioManager.I?.PlaySFX2D(SFXKey.UI_Click);

        var bc = BattleController.Instance;
        if (bc != null)
        {
            bc.ForfeitBattle();
            return;
        }

        // 안전 fallback
        string next = (GameContext.I != null)
            ? GameContext.I.returnExplorationSceneName
            : "Exploration";

        if (SceneFader.I != null)
            SceneFader.I.LoadSceneWithFade(next);
        else
            SceneManager.LoadScene(next);
    }
}