using UnityEngine;
using UnityEngine.UI;

public class TitleQuitPanelController : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject panelRoot;

    [Header("Buttons")]
    [SerializeField] private Button btnYes;
    [SerializeField] private Button btnNo;

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

    private void Awake()
    {
        if (btnYes != null)
        {
            btnYes.onClick.RemoveListener(OnClickQuit);
            btnYes.onClick.AddListener(OnClickQuit);
        }

        if (btnNo != null)
        {
            btnNo.onClick.RemoveListener(Close);
            btnNo.onClick.AddListener(Close);
        }

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    public void Open()
    {
        if (panelRoot == null) return;

        panelRoot.SetActive(true);

        AudioManager.I?.PlaySFX2D(SFXKey.UI_Open);
    }

    public void Close()
    {
        if (panelRoot == null) return;

        panelRoot.SetActive(false);

        AudioManager.I?.PlaySFX2D(SFXKey.UI_Close);
    }

    private void OnClickQuit()
    {
        AudioManager.I?.PlaySFX2D(SFXKey.UI_Click);

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}