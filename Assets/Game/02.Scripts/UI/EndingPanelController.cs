using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EndingPanelController : MonoBehaviour
{
    [Header("Root")]
    [Tooltip("엔딩 패널 전체 루트. 비워두면 자기 자신 사용")]
    public GameObject panelRoot;

    [Header("Texts")]
    public TMP_Text titleText;
    public TMP_Text messageText;

    [Header("Buttons")]
    public Button titleButton;
    public Button quitButton;

    [Header("Scene")]
    [Tooltip("타이틀 버튼 클릭 시 이동할 씬 이름")]
    public string titleSceneName = "Title";

    [Header("Default Text")]
    [TextArea]
    public string defaultTitle = "The End";

    [TextArea]
    public string defaultMessage = "마침내 드래곤을 쓰러뜨렸습니다.\nPoF의 첫 번째 여정이 여기서 끝납니다.";

    [Header("Options")]
    [Tooltip("시작 시 패널 숨김")]
    public bool hideOnStart = true;

    private bool _hooked;
    //private bool _shownOnce;

    private void Awake()
    {
        if (panelRoot == null)
            panelRoot = gameObject;

        HookButtonsOnce();
    }

    private void Start()
    {
        if (hideOnStart)
            HideImmediate();

        // 자동 표시 기능은 사용하지 않는다.
        // 엔딩 패널은 마지막 퀘스트 보상 수령 시 QuestPanelController에서 직접 ShowEnding() 호출한다.
    }

    private void HookButtonsOnce()
    {
        if (_hooked) return;
        _hooked = true;

        if (titleButton != null)
        {
            titleButton.onClick.RemoveListener(OnClickTitle);
            titleButton.onClick.AddListener(OnClickTitle);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(OnClickQuit);
            quitButton.onClick.AddListener(OnClickQuit);
        }
    }

    public void ShowEnding()
    {
        ShowEnding(defaultTitle, defaultMessage);
    }

    public void ShowEnding(string title, string message)
    {
        if (panelRoot == null)
            panelRoot = gameObject;

        if (titleText != null)
            titleText.text = string.IsNullOrWhiteSpace(title) ? defaultTitle : title;

        if (messageText != null)
            messageText.text = string.IsNullOrWhiteSpace(message) ? defaultMessage : message;

        panelRoot.SetActive(true);
        //_shownOnce = true;

        ApplyOpenPolicy();

        Debug.Log("[EndingPanel] ShowEnding");
    }

    public void HideImmediate()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

        ApplyClosePolicy();
    }

    private void OnClickTitle()
    {
        Debug.Log("[EndingPanel] Title button clicked");

        if (GameContext.I != null)
        {
            GameContext.I.endingShown = false;
            GameContext.I.SetUIBlockingLook(false);

            // 마지막 퀘스트 완료 상태를 저장한 뒤 타이틀로 이동
            GameContext.I.SaveGame();
        }

        HideImmediate();

        if (SceneFader.I != null)
            SceneFader.I.LoadSceneWithFade(titleSceneName);
        else
            SceneManager.LoadScene(titleSceneName);
    }

    private void OnClickQuit()
    {
        Debug.Log("[EndingPanel] Quit button clicked");

        if (GameContext.I != null)
        {
            GameContext.I.endingShown = false;
            GameContext.I.SetUIBlockingLook(false);

            GameContext.I.SaveGame();
        }

        HideImmediate();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
    }

    private void ApplyOpenPolicy()
    {
        if (GameContext.I != null)
            GameContext.I.SetUIBlockingLook(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void ApplyClosePolicy()
    {
        if (GameContext.I != null)
            GameContext.I.SetUIBlockingLook(false);

        string sceneName = SceneManager.GetActiveScene().name;

        if (sceneName == "Exploration")
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}