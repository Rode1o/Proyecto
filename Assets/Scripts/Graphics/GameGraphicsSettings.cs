using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ajustes gráficos del juego guardados en PlayerPrefs.
/// Solo se aplican al arrancar si el jugador los cambió alguna vez; si no, se respetan los de Player Settings.
/// </summary>
public static class GameGraphicsSettings
{
    const string ModeKey = "gfx.mode";
    const string WidthKey = "gfx.width";
    const string HeightKey = "gfx.height";
    const string QualityKey = "gfx.quality";
    const string VSyncKey = "gfx.vsync";
    const string FpsKey = "gfx.fps";

    public static readonly FullScreenMode[] DisplayModes =
    {
        FullScreenMode.ExclusiveFullScreen,
        FullScreenMode.FullScreenWindow,
        FullScreenMode.Windowed,
    };

    public static readonly string[] DisplayModeNames = { "Pantalla completa", "Sin bordes", "Ventana" };

    /// <summary>-1 = sin límite.</summary>
    public static readonly int[] FrameRateLimits = { 30, 60, 120, 144, -1 };

    static readonly Dictionary<string, string> QualityNames = new()
    {
        { "Very Low", "Muy baja" },
        { "Low", "Baja" },
        { "Medium", "Media" },
        { "High", "Alta" },
        { "Very High", "Muy alta" },
        { "Ultra", "Ultra" },
        { "Mobile", "Baja" },
        { "PC", "Alta" },
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ApplySaved()
    {
        if (PlayerPrefs.HasKey(QualityKey))
            QualitySettings.SetQualityLevel(Mathf.Clamp(PlayerPrefs.GetInt(QualityKey), 0, QualitySettings.names.Length - 1), true);
        if (PlayerPrefs.HasKey(VSyncKey))
            QualitySettings.vSyncCount = PlayerPrefs.GetInt(VSyncKey);
        if (PlayerPrefs.HasKey(FpsKey))
            Application.targetFrameRate = PlayerPrefs.GetInt(FpsKey);
        if (PlayerPrefs.HasKey(ModeKey) && PlayerPrefs.HasKey(WidthKey))
            Screen.SetResolution(PlayerPrefs.GetInt(WidthKey), PlayerPrefs.GetInt(HeightKey), (FullScreenMode)PlayerPrefs.GetInt(ModeKey));
    }

    // ---------- Modo de pantalla ----------

    public static int DisplayModeIndex
    {
        get
        {
            var index = System.Array.IndexOf(DisplayModes, Screen.fullScreenMode);
            // MaximizedWindow (macOS) se muestra como "Sin bordes".
            return index >= 0 ? index : 1;
        }
    }

    public static void SetDisplayMode(int index)
    {
        var mode = DisplayModes[index];
        var resolution = CurrentResolution;
        Screen.SetResolution(resolution.x, resolution.y, mode);
        PlayerPrefs.SetInt(ModeKey, (int)mode);
        SaveResolution(resolution);
    }

    // ---------- Resolución ----------

    /// <summary>Resoluciones disponibles sin duplicados (ignorando la frecuencia), de mayor a menor.</summary>
    public static List<Vector2Int> Resolutions
    {
        get
        {
            var list = new List<Vector2Int>();
            foreach (var r in Screen.resolutions)
            {
                var size = new Vector2Int(r.width, r.height);
                if (!list.Contains(size))
                    list.Add(size);
            }

            if (list.Count == 0)
                list.Add(CurrentResolution);
            list.Sort((a, b) => (b.x * b.y).CompareTo(a.x * a.y));
            return list;
        }
    }

    public static Vector2Int CurrentResolution =>
        Screen.fullScreenMode == FullScreenMode.Windowed
            ? new Vector2Int(Screen.width, Screen.height)
            : new Vector2Int(Screen.currentResolution.width, Screen.currentResolution.height);

    public static void SetResolution(Vector2Int resolution)
    {
        Screen.SetResolution(resolution.x, resolution.y, Screen.fullScreenMode);
        PlayerPrefs.SetInt(ModeKey, (int)Screen.fullScreenMode);
        SaveResolution(resolution);
    }

    static void SaveResolution(Vector2Int resolution)
    {
        PlayerPrefs.SetInt(WidthKey, resolution.x);
        PlayerPrefs.SetInt(HeightKey, resolution.y);
    }

    // ---------- Calidad ----------

    public static string[] QualityLevelNames
    {
        get
        {
            var names = QualitySettings.names;
            var result = new string[names.Length];
            for (var i = 0; i < names.Length; i++)
                result[i] = QualityNames.TryGetValue(names[i], out var translated) ? translated : names[i];
            return result;
        }
    }

    public static int QualityLevel => QualitySettings.GetQualityLevel();

    public static void SetQualityLevel(int level)
    {
        QualitySettings.SetQualityLevel(level, true);
        PlayerPrefs.SetInt(QualityKey, level);
        // Cambiar de nivel también cambia el vSync; se mantiene el que eligió el jugador.
        if (PlayerPrefs.HasKey(VSyncKey))
            QualitySettings.vSyncCount = PlayerPrefs.GetInt(VSyncKey);
    }

    // ---------- VSync y FPS ----------

    public static bool VSync => QualitySettings.vSyncCount > 0;

    public static void SetVSync(bool enabled)
    {
        QualitySettings.vSyncCount = enabled ? 1 : 0;
        PlayerPrefs.SetInt(VSyncKey, QualitySettings.vSyncCount);
    }

    public static int FrameRateLimitIndex
    {
        get
        {
            var index = System.Array.IndexOf(FrameRateLimits, Application.targetFrameRate);
            return index >= 0 ? index : FrameRateLimits.Length - 1;
        }
    }

    public static void SetFrameRateLimit(int index)
    {
        Application.targetFrameRate = FrameRateLimits[index];
        PlayerPrefs.SetInt(FpsKey, Application.targetFrameRate);
    }

    public static void Save() => PlayerPrefs.Save();
}
