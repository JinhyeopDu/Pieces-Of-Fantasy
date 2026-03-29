using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager I { get; private set; }

    [Header("Mixer")]
    public AudioMixer mixer;

    [Header("Controllers")]
    public BGMController bgm;
    public SFXPlayer sfx;

    [Header("Clips (Temporary simple library)")]
    public List<Entry> bgmClips = new();
    public List<Entry> sfxClips = new();

    private Dictionary<string, AudioClip> _bgmMap;
    private Dictionary<string, AudioClip> _sfxMap;

    private bool _settingsBound = false;

    [System.Serializable]
    public struct Entry
    {
        public string key;
        public AudioClip clip;
    }

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        DontDestroyOnLoad(gameObject);

        BuildMaps();

        if (bgm == null) bgm = GetComponent<BGMController>();
        if (sfx == null) sfx = GetComponent<SFXPlayer>();

        if (bgm != null) bgm.mixer = mixer;
        if (sfx != null) sfx.mixer = mixer;

        // 1차 시도
        TryBindAudioSettings();
    }

    private void Start()
    {
        // 2차 시도 (Awake 순서 문제 보완)
        if (!_settingsBound)
            TryBindAudioSettings();

        // 아직도 못 찾았으면 잠깐 재시도
        if (!_settingsBound)
            StartCoroutine(CoRetryBindAudioSettings());
    }

    private void OnDestroy()
    {
        if (I == this && AudioSettings.I != null && _settingsBound)
            AudioSettings.I.OnChanged -= ApplyFromSettings;
    }

    private IEnumerator CoRetryBindAudioSettings()
    {
        float timeout = 2f;
        float t = 0f;

        while (!_settingsBound && t < timeout)
        {
            TryBindAudioSettings();

            if (_settingsBound)
                yield break;

            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!_settingsBound)
            Debug.LogWarning("[AudioManager] Failed to bind AudioSettings after retry.");
    }

    private void TryBindAudioSettings()
    {
        var st = AudioSettings.I;
        if (st == null) return;

        if (_settingsBound)
        {
            ApplyFromSettings();
            return;
        }

        st.OnChanged -= ApplyFromSettings;
        st.OnChanged += ApplyFromSettings;
        _settingsBound = true;

        Debug.Log("[AudioManager] AudioSettings bound successfully.");

        // 바인딩 즉시 현재 저장값 강제 적용
        ApplyFromSettings();
    }

    private void BuildMaps()
    {
        _bgmMap = new Dictionary<string, AudioClip>(128);
        _sfxMap = new Dictionary<string, AudioClip>(256);

        foreach (var e in bgmClips)
            if (!string.IsNullOrWhiteSpace(e.key) && e.clip != null)
                _bgmMap[e.key] = e.clip;

        foreach (var e in sfxClips)
            if (!string.IsNullOrWhiteSpace(e.key) && e.clip != null)
                _sfxMap[e.key] = e.clip;
    }

    private void ApplyFromSettings()
    {
        var st = AudioSettings.I;
        if (st == null) return;

        Debug.Log($"[AudioManager] ApplyFromSettings MasterVol={st.MasterVol}, BgmVol={st.BgmVol}, SfxVol={st.SfxVol}, MasterMute={st.MasterMute}, BgmMute={st.BgmMute}, SfxMute={st.SfxMute}");

        if (bgm != null)
            bgm.ApplyMixerVolumes(st.MasterVol, st.BgmVol, st.MasterMute, st.BgmMute);

        if (sfx != null)
            sfx.ApplyMixerVolumes(st.MasterVol, st.SfxVol, st.MasterMute, st.SfxMute);
    }

    public void PlayBGM(string key)
    {
        if (bgm == null) return;

        if (_bgmMap != null && _bgmMap.TryGetValue(key, out var clip))
            bgm.Play(clip);
        else
            Debug.LogWarning($"[AudioManager] BGM key not found: {key}");
    }

    public void PlaySFX2D(string key, float volume = 1f, float cooldown = 0f)
    {
        if (sfx == null) return;

        if (_sfxMap != null && _sfxMap.TryGetValue(key, out var clip))
            sfx.Play2D(key, clip, volume, cooldown);
        else
            Debug.LogWarning($"[AudioManager] SFX key not found: {key}");
    }

    public void ForceApplyCurrentSettings()
    {
        TryBindAudioSettings();
        ApplyFromSettings();
    }


}