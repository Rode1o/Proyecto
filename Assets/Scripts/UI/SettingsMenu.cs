using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel de configuración. De momento solo sonido: un slider por canal de audio.
/// </summary>
public class SettingsMenu : MonoBehaviour
{
    [Serializable]
    class VolumeRow
    {
        public AudioChannel channel;
        public Slider slider;
        public TMP_Text valueLabel;
    }

    [SerializeField] VolumeRow[] volumeRows = Array.Empty<VolumeRow>();

    void Awake()
    {
        foreach (var row in volumeRows)
        {
            var current = row;
            current.slider.minValue = 0f;
            current.slider.maxValue = 1f;
            current.slider.onValueChanged.AddListener(value =>
            {
                GameAudioSettings.SetVolume(current.channel, value);
                UpdateLabel(current, value);
            });
        }
    }

    void OnEnable()
    {
        foreach (var row in volumeRows)
        {
            var volume = GameAudioSettings.GetVolume(row.channel);
            row.slider.SetValueWithoutNotify(volume);
            UpdateLabel(row, volume);
        }
    }

    static void UpdateLabel(VolumeRow row, float value)
    {
        if (row.valueLabel != null)
            row.valueLabel.text = $"{Mathf.RoundToInt(value * 100f)}%";
    }
}
