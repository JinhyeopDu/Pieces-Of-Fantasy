// Assets/Game/02.Scripts/UI/SettingsPanelController.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SettingsPanelController : MonoBehaviour
{
    [Header("Root")]
    public GameObject panelRoot; // 보통 = PF_SettingsPanel 자기 자신

    [Header("Sliders")]
    public Slider masterSlider;
    public Slider bgmSlider;
    public Slider sfxSlider;

    [Header("Toggles")]
    public Toggle masterMuteToggle;
    public Toggle bgmMuteToggle;
    public Toggle sfxMuteToggle;

    [Header("Checkmark Images (Direct Control)")]
    [Tooltip("Toggle_MasterMute/Background/Checkmark 의 Image")]
    public Image masterMuteCheckmark;
    [Tooltip("Toggle_BGMMute/Background/Checkmark 의 Image")]
    public Image bgmMuteCheckmark;
    [Tooltip("Toggle_SFXMute/Background/Checkmark 의 Image")]
    public Image sfxMuteCheckmark;

    [Header("Buttons")]
    public Button closeButton;

    [Header("Safety")]
    [Tooltip("열자마자 같은 클릭으로 닫히는 현상 방지용 시간(초)")]
    public float closeGuardSeconds = 0.12f;

    private bool _binding;
    private float _openedAt = -999f;
    private bool _hooked;

    private void Awake()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Close);
            closeButton.onClick.AddListener(Close);
        }

        HookUIEventsOnce();
    }

    private void Start()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
            ApplyClosePolicy();
        }

        // 시작 시 1회 강제 동기화
        BindFromSettings();
    }

    private void OnEnable()
    {
        BindFromSettings();

        if (AudioSettings.I != null)
            AudioSettings.I.OnChanged += BindFromSettings;
    }

    private void OnDisable()
    {
        if (AudioSettings.I != null)
            AudioSettings.I.OnChanged -= BindFromSettings;
    }

    public void Toggle()
    {
        if (panelRoot == null) return;

        bool willOpen = !panelRoot.activeSelf;
        panelRoot.SetActive(willOpen);

        if (willOpen)
        {
            _openedAt = Time.unscaledTime;
            BindFromSettings();
            ApplyOpenPolicy();
            AudioManager.I?.PlaySFX2D(SFXKey.UI_Open, 1f, 0.05f);
        }
        else
        {
            ApplyClosePolicy();
            AudioManager.I?.PlaySFX2D(SFXKey.UI_Close, 1f, 0.05f);
        }
    }

    public void Open()
    {
        if (panelRoot == null) return;
        if (panelRoot.activeSelf) return;

        panelRoot.SetActive(true);
        _openedAt = Time.unscaledTime;

        BindFromSettings();
        ApplyOpenPolicy();

        AudioManager.I?.PlaySFX2D(SFXKey.UI_Open, 1f, 0.05f);
    }

    public void Close()
    {
        if (panelRoot == null) return;
        if (!panelRoot.activeSelf) return;

        if (Time.unscaledTime - _openedAt < closeGuardSeconds)
            return;

        ApplyClosePolicy();
        panelRoot.SetActive(false);
        AudioManager.I?.PlaySFX2D(SFXKey.UI_Close, 1f, 0.05f);
    }

    private void BindFromSettings()
    {
        var st = AudioSettings.I;
        if (st == null) return;

        Debug.Log($"[Bind] Master={st.MasterMute}, Bgm={st.BgmMute}, Sfx={st.SfxMute}");

        _binding = true;

        // Slider 값 동기화
        if (masterSlider != null) masterSlider.SetValueWithoutNotify(st.MasterVol);
        if (bgmSlider != null) bgmSlider.SetValueWithoutNotify(st.BgmVol);
        if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(st.SfxVol);

        // Toggle 값 동기화
        if (masterMuteToggle != null) masterMuteToggle.SetIsOnWithoutNotify(st.MasterMute);
        if (bgmMuteToggle != null) bgmMuteToggle.SetIsOnWithoutNotify(st.BgmMute);
        if (sfxMuteToggle != null) sfxMuteToggle.SetIsOnWithoutNotify(st.SfxMute);

        // Checkmark 직접 ON/OFF
        ApplyCheckmarkState(masterMuteCheckmark, st.MasterMute);
        ApplyCheckmarkState(bgmMuteCheckmark, st.BgmMute);
        ApplyCheckmarkState(sfxMuteCheckmark, st.SfxMute);

        _binding = false;
    }

    private void HookUIEventsOnce()
    {
        if (_hooked) return;
        _hooked = true;

        if (masterSlider != null) masterSlider.onValueChanged.AddListener(OnMasterSlider);
        if (bgmSlider != null) bgmSlider.onValueChanged.AddListener(OnBgmSlider);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnSfxSlider);

        if (masterMuteToggle != null) masterMuteToggle.onValueChanged.AddListener(OnMasterMute);
        if (bgmMuteToggle != null) bgmMuteToggle.onValueChanged.AddListener(OnBgmMute);
        if (sfxMuteToggle != null) sfxMuteToggle.onValueChanged.AddListener(OnSfxMute);
    }

    private void OnMasterSlider(float v)
    {
        if (_binding) return;
        AudioSettings.I?.SetMasterVol(v);
    }

    private void OnBgmSlider(float v)
    {
        if (_binding) return;
        AudioSettings.I?.SetBgmVol(v);
    }

    private void OnSfxSlider(float v)
    {
        if (_binding) return;
        AudioSettings.I?.SetSfxVol(v);
        AudioManager.I?.PlaySFX2D(SFXKey.UI_Click, 0.6f, 0.08f);
    }

    private void OnMasterMute(bool on)
    {
        if (_binding) return;

        Debug.Log($"[SettingsPanel] MasterMute Toggle Changed => {on}");

        AudioSettings.I?.SetMasterMute(on);
        ApplyCheckmarkState(masterMuteCheckmark, on);
    }

    private void OnBgmMute(bool on)
    {
        if (_binding) return;

        Debug.Log($"[SettingsPanel] BgmMute Toggle Changed => {on}");

        AudioSettings.I?.SetBgmMute(on);
        ApplyCheckmarkState(bgmMuteCheckmark, on);
    }

    private void OnSfxMute(bool on)
    {
        if (_binding) return;

        Debug.Log($"[SettingsPanel] SfxMute Toggle Changed => {on}");

        AudioSettings.I?.SetSfxMute(on);
        ApplyCheckmarkState(sfxMuteCheckmark, on);
    }

    private void ApplyCheckmarkState(Image checkmark, bool on)
    {
        if (checkmark == null) return;

        // Image 자체를 확실히 켜고 끔
        checkmark.enabled = on;

        // 혹시 GameObject가 비활성이라면 활성화
        if (checkmark.gameObject.activeSelf != true)
            checkmark.gameObject.SetActive(true);

        // 알파가 0으로 되어 있을 경우 대비
        Color c = checkmark.color;
        c.a = on ? 1f : 0f;
        checkmark.color = c;
    }

    public bool IsPanelOpen()
    {
        return panelRoot != null && panelRoot.activeSelf;
    }

    private void ApplyOpenPolicy()
    {
        GameContext.I?.SetUIBlockingLook(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void ApplyClosePolicy()
    {
        GameContext.I?.SetUIBlockingLook(false);

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