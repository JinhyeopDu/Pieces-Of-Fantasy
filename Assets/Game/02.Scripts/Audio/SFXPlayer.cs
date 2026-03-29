// Assets/Game/02.Scripts/Audio/SFXPlayer.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class SFXPlayer : MonoBehaviour
{
    [Header("Mixer")]
    public AudioMixer mixer;
    public string masterParam = "MasterVol";
    public string sfxParam = "SFXVol";

    [Header("2D OneShot Source")]
    public AudioSource oneShot2D;

    // 간단한 연타 제한용
    private readonly Dictionary<string, float> _cooldownUntil = new();

    private void Awake()
    {
        if (oneShot2D == null) oneShot2D = gameObject.AddComponent<AudioSource>();
        oneShot2D.playOnAwake = false;
        oneShot2D.loop = false;
        oneShot2D.spatialBlend = 0f;
    }

    public void ApplyMixerVolumes(float masterLinear, float sfxLinear, bool masterMute, bool sfxMute)
    {
        if (mixer == null) return;

        float m = masterMute ? 0f : masterLinear;
        float v = sfxMute ? 0f : sfxLinear;

        mixer.SetFloat(masterParam, AudioUtil.LinearToDb(m));
        mixer.SetFloat(sfxParam, AudioUtil.LinearToDb(v));
    }

    public void Play2D(string key, AudioClip clip, float volume = 1f, float cooldown = 0f)
    {
        if (clip == null) return;

        if (cooldown > 0f)
        {
            float now = Time.unscaledTime;
            if (_cooldownUntil.TryGetValue(key, out float until) && now < until) return;
            _cooldownUntil[key] = now + cooldown;
        }

        oneShot2D.PlayOneShot(clip, Mathf.Clamp01(volume));
    }
}