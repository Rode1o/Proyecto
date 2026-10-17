using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Página de gráficos: modo de pantalla, resolución, calidad, VSync y límite de FPS.
/// Cada cambio se aplica al momento y se guarda.
/// </summary>
public class GraphicsSettingsMenu : MonoBehaviour
{
    [SerializeField] OptionSelector displayMode;
    [SerializeField] OptionSelector resolution;
    [SerializeField] OptionSelector quality;
    [SerializeField] OptionSelector vSync;
    [SerializeField] OptionSelector frameRate;

    List<Vector2Int> resolutions;

    void Awake()
    {
        displayMode.ValueChanged += GameGraphicsSettings.SetDisplayMode;
        resolution.ValueChanged += i => GameGraphicsSettings.SetResolution(resolutions[i]);
        quality.ValueChanged += GameGraphicsSettings.SetQualityLevel;
        vSync.ValueChanged += i => GameGraphicsSettings.SetVSync(i == 1);
        frameRate.ValueChanged += GameGraphicsSettings.SetFrameRateLimit;
    }

    void OnEnable()
    {
        displayMode.SetOptions(GameGraphicsSettings.DisplayModeNames, GameGraphicsSettings.DisplayModeIndex);

        resolutions = GameGraphicsSettings.Resolutions;
        var names = resolutions.ConvertAll(r => $"{r.x} × {r.y}");
        resolution.SetOptions(names, Mathf.Max(0, resolutions.IndexOf(GameGraphicsSettings.CurrentResolution)));

        quality.SetOptions(GameGraphicsSettings.QualityLevelNames, GameGraphicsSettings.QualityLevel);
        vSync.SetOptions(new[] { "Desactivada", "Activada" }, GameGraphicsSettings.VSync ? 1 : 0);

        var limits = new List<string>();
        foreach (var limit in GameGraphicsSettings.FrameRateLimits)
            limits.Add(limit < 0 ? "Sin límite" : $"{limit} FPS");
        frameRate.SetOptions(limits, GameGraphicsSettings.FrameRateLimitIndex);
    }

    void OnDisable()
    {
        GameGraphicsSettings.Save();
    }
}
