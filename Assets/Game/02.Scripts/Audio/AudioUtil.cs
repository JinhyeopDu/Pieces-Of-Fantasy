// Assets/Game/02.Scripts/Audio/AudioUtil.cs
using UnityEngine;

public static class AudioUtil
{
    // 0..1 -> dB (-80..0)
    public static float LinearToDb(float linear)
    {
        linear = Mathf.Clamp01(linear);
        if (linear <= 0.0001f) return -80f;
        return Mathf.Log10(linear) * 20f;
    }
}