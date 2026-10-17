using System;
using UnityEngine;

public enum AudioChannel
{
    Master,
    Music,
    Sfx,
    Voice
}

/// <summary>
/// Volúmenes del juego por canal, guardados en PlayerPrefs.
/// El canal Master se aplica a AudioListener.volume; Music, Sfx y Voice los aplican los AudioChannelVolume.
/// </summary>
public static class GameAudioSettings
{
    const string KeyPrefix = "audio.volume.";

    static readonly float[] Defaults = { 1f, 0.7f, 1f, 0.8f };
    static readonly float[] Volumes = new float[Defaults.Length];
    static bool loaded;

    public static event Action<AudioChannel, float> VolumeChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize()
    {
        // Con "Enter Play Mode Options" el estado estático puede sobrevivir entre sesiones de Play.
        loaded = false;
        VolumeChanged = null;
        EnsureLoaded();
        AudioListener.volume = Volumes[(int)AudioChannel.Master];
    }

    public static float GetVolume(AudioChannel channel)
    {
        EnsureLoaded();
        return Volumes[(int)channel];
    }

    public static void SetVolume(AudioChannel channel, float volume)
    {
        EnsureLoaded();
        volume = Mathf.Clamp01(volume);
        if (Mathf.Approximately(Volumes[(int)channel], volume))
            return;

        Volumes[(int)channel] = volume;
        PlayerPrefs.SetFloat(KeyPrefix + channel, volume);

        if (channel == AudioChannel.Master)
            AudioListener.volume = volume;

        VolumeChanged?.Invoke(channel, volume);
    }

    public static void Save() => PlayerPrefs.Save();

    static void EnsureLoaded()
    {
        if (loaded)
            return;

        for (var i = 0; i < Volumes.Length; i++)
            Volumes[i] = PlayerPrefs.GetFloat(KeyPrefix + (AudioChannel)i, Defaults[i]);
        loaded = true;
    }
}
