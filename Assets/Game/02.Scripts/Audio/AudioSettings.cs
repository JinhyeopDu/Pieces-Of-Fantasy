// Assets/Game/02.Scripts/Audio/AudioSettings.cs
using System;
using UnityEngine;

public class AudioSettings : MonoBehaviour
{
    public static AudioSettings I { get; private set; }

    public event Action OnChanged;

    private const string PP_MasterVol = "Audio.MasterVol";
    private const string PP_BgmVol = "Audio.BgmVol";
    private const string PP_SfxVol = "Audio.SfxVol";
    private const string PP_MasterMute = "Audio.MasterMute";
    private const string PP_BgmMute = "Audio.BgmMute";
    private const string PP_SfxMute = "Audio.SfxMute";

    [Range(0f, 1f)][SerializeField] private float masterVol = 1f;
    [Range(0f, 1f)][SerializeField] private float bgmVol = 1f;
    [Range(0f, 1f)][SerializeField] private float sfxVol = 1f;

    [SerializeField] private bool masterMute = false;
    [SerializeField] private bool bgmMute = false;
    [SerializeField] private bool sfxMute = false;

    public float MasterVol => masterVol;
    public float BgmVol => bgmVol;
    public float SfxVol => sfxVol;

    public bool MasterMute => masterMute;
    public bool BgmMute => bgmMute;
    public bool SfxMute => sfxMute;

    private void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        Load();
        RaiseChanged(); // 초기 1회 반영
    }

    public void SetMasterVol(float v) { masterVol = Mathf.Clamp01(v); Save(); RaiseChanged(); }
    public void SetBgmVol(float v) { bgmVol = Mathf.Clamp01(v); Save(); RaiseChanged(); }
    public void SetSfxVol(float v) { sfxVol = Mathf.Clamp01(v); Save(); RaiseChanged(); }

    public void SetMasterMute(bool on) { masterMute = on; Save(); RaiseChanged(); }
    public void SetBgmMute(bool on) { bgmMute = on; Save(); RaiseChanged(); }
    public void SetSfxMute(bool on) { sfxMute = on; Save(); RaiseChanged(); }

    public void Load()
    {
        masterVol = PlayerPrefs.GetFloat(PP_MasterVol, 1f);
        bgmVol = PlayerPrefs.GetFloat(PP_BgmVol, 1f);
        sfxVol = PlayerPrefs.GetFloat(PP_SfxVol, 1f);

        masterMute = PlayerPrefs.GetInt(PP_MasterMute, 0) == 1;
        bgmMute = PlayerPrefs.GetInt(PP_BgmMute, 0) == 1;
        sfxMute = PlayerPrefs.GetInt(PP_SfxMute, 0) == 1;
    }

    public void Save()
    {
        PlayerPrefs.SetFloat(PP_MasterVol, masterVol);
        PlayerPrefs.SetFloat(PP_BgmVol, bgmVol);
        PlayerPrefs.SetFloat(PP_SfxVol, sfxVol);

        PlayerPrefs.SetInt(PP_MasterMute, masterMute ? 1 : 0);
        PlayerPrefs.SetInt(PP_BgmMute, bgmMute ? 1 : 0);
        PlayerPrefs.SetInt(PP_SfxMute, sfxMute ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void RaiseChanged() => OnChanged?.Invoke();
}