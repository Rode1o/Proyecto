using FMOD.Studio;
using FMODUnity;
using UnityEngine;

/// <summary>
/// Música de fondo: un único evento de FMOD que sigue sonando al cambiar de escena, para que el paso
/// del menú a la exploración use la transición musical del evento. Va uno en cada escena con la parte
/// que le toca; el primero se queda vivo y los siguientes solo le dicen a qué parte pasar.
/// Parámetros del evento (locales):
///   NIVELES   0 = menú, 1 = exploración.
///   PUZZLES   capas de la exploración: sube con cada "no" a la oveja.
///   Preguntas 0 (Sí) silencia todas las pistas, 1 (No) las deja sonar: aceptar la oferta es la derrota.
/// El volumen sigue a los canales Master y Música de <see cref="GameAudioSettings"/>.
/// </summary>
public class GameMusic : MonoBehaviour
{
    public enum Part
    {
        Menu = 0,
        Exploration = 1
    }

    const string PartParameter = "NIVELES";
    const string LayersParameter = "PUZZLES";
    const string PlayingParameter = "Preguntas";

    [SerializeField] EventReference music;
    [Tooltip("Parte de la música que suena en esta escena.")]
    [SerializeField] Part part = Part.Menu;
    [Tooltip("Valor máximo de PUZZLES (capas extra al rechazar a la oveja).")]
    [SerializeField, Min(0)] int maxLayers = 2;

    static GameMusic current;

    EventInstance instance;
    SheepEnemy sheep;
    int rejections;

    void Awake()
    {
        // Ya suena la música de otra escena: solo se cambia de parte.
        if (current != null)
        {
            current.EnterScene(part);
            Destroy(gameObject);
            return;
        }

        current = this;
        DontDestroyOnLoad(gameObject);

        try
        {
            instance = RuntimeManager.CreateInstance(music);
        }
        catch (EventNotFoundException)
        {
            Debug.LogWarning("GameMusic: el evento de música no está en los bancos (¿asignado a un banco y construido?).", this);
            return;
        }

        instance.start();
        GameAudioSettings.VolumeChanged += OnVolumeChanged;
        ApplyVolume();
        EnterScene(part);
    }

    void OnDestroy()
    {
        if (current != this)
            return;

        current = null;
        GameAudioSettings.VolumeChanged -= OnVolumeChanged;
        ListenTo(null);
        if (instance.isValid())
        {
            instance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            instance.release();
        }
    }

    /// <summary>Cada escena (o reintento) empieza con la música base de su parte.</summary>
    void EnterScene(Part scenePart)
    {
        rejections = 0;
        SetParameter(PartParameter, (int)scenePart);
        SetParameter(LayersParameter, 0);
        SetParameter(PlayingParameter, 1);
        ListenTo(FindAnyObjectByType<SheepEnemy>());
    }

    void ListenTo(SheepEnemy target)
    {
        if (sheep != null)
        {
            sheep.Rejected -= OnRejected;
            sheep.Accepted -= OnAccepted;
        }

        sheep = target;
        if (sheep != null)
        {
            sheep.Rejected += OnRejected;
            sheep.Accepted += OnAccepted;
        }
    }

    void OnRejected(SheepEnemy _)
    {
        rejections++;
        SetParameter(LayersParameter, Mathf.Min(rejections, maxLayers));
    }

    void OnAccepted(SheepEnemy _) => SetParameter(PlayingParameter, 0);

    void OnVolumeChanged(AudioChannel channel, float _)
    {
        if (channel == AudioChannel.Master || channel == AudioChannel.Music)
            ApplyVolume();
    }

    // El canal Master de GameAudioSettings mueve AudioListener.volume, que no afecta a FMOD.
    void ApplyVolume()
    {
        if (instance.isValid())
            instance.setVolume(GameAudioSettings.GetVolume(AudioChannel.Master) * GameAudioSettings.GetVolume(AudioChannel.Music));
    }

    void SetParameter(string name, float value)
    {
        if (instance.isValid())
            instance.setParameterByName(name, value);
    }
}
