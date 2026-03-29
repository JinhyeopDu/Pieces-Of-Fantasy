// Assets/Game/02.Scripts/Audio/BGMController.cs
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class BGMController : MonoBehaviour
{
    [Header("Mixer")]
    public AudioMixer mixer;
    public string masterParam = "MasterVol";
    public string bgmParam = "BGMVol";

    [Header("Sources (2 for crossfade)")]
    public AudioSource a;
    public AudioSource b;

    [Header("Fade")]
    public float fadeTime = 0.6f;

    private AudioSource _cur;
    private AudioSource _next;
    private Coroutine _fadeCo;

    private void Awake()
    {
        // 자동 연결(없으면 생성)
        if (a == null) a = gameObject.AddComponent<AudioSource>();
        if (b == null) b = gameObject.AddComponent<AudioSource>();

        SetupSource(a);
        SetupSource(b);

        _cur = a;
        _next = b;
    }

    private void SetupSource(AudioSource s)
    {
        s.playOnAwake = false;
        s.loop = true;
        s.spatialBlend = 0f; // BGM은 2D
        s.volume = 1f;       // 실제 볼륨은 Mixer로 제어
    }

    public void ApplyMixerVolumes(float masterLinear, float bgmLinear, bool masterMute, bool bgmMute)
    {
        if (mixer == null) return;

        float m = masterMute ? 0f : masterLinear;
        float v = bgmMute ? 0f : bgmLinear;

        mixer.SetFloat(masterParam, AudioUtil.LinearToDb(m));
        mixer.SetFloat(bgmParam, AudioUtil.LinearToDb(v));
    }

    public void Play(AudioClip clip)
    {
        if (clip == null) return;

        // 동일 클립이면 무시
        if (_cur.isPlaying && _cur.clip == clip) return;

        if (_fadeCo != null) StopCoroutine(_fadeCo);
        _fadeCo = StartCoroutine(CrossFadeTo(clip));
    }

    private IEnumerator CrossFadeTo(AudioClip clip)
    {
        _next.clip = clip;
        _next.volume = 0f;
        _next.Play();

        float t = 0f;
        float dur = Mathf.Max(0.05f, fadeTime);

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);

            _cur.volume = 1f - k;
            _next.volume = k;

            yield return null;
        }

        _cur.Stop();
        _cur.volume = 1f;
        _next.volume = 1f;

        // swap
        var temp = _cur;
        _cur = _next;
        _next = temp;

        _fadeCo = null;
    }
}