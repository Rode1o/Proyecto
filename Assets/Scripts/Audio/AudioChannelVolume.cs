using UnityEngine;

/// <summary>
/// Ajusta el volumen de un AudioSource según el canal (Música, Efectos o Voces) elegido en la configuración.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class AudioChannelVolume : MonoBehaviour
{
    [SerializeField] AudioChannel channel = AudioChannel.Sfx;
    [SerializeField, Range(0f, 1f)] float baseVolume = 1f;

    AudioSource source;

    void Awake()
    {
        source = GetComponent<AudioSource>();
    }

    void OnEnable()
    {
        GameAudioSettings.VolumeChanged += OnVolumeChanged;
        Apply();
    }

    void OnDisable()
    {
        GameAudioSettings.VolumeChanged -= OnVolumeChanged;
    }

    void OnVolumeChanged(AudioChannel changed, float volume)
    {
        if (changed == channel)
            Apply();
    }

    void Apply()
    {
        // El volumen general ya lo aplica AudioListener.volume; no se multiplica dos veces.
        var channelVolume = channel == AudioChannel.Master ? 1f : GameAudioSettings.GetVolume(channel);
        source.volume = baseVolume * channelVolume;
    }
}
