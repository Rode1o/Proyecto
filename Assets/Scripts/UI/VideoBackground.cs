using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Fondo de vídeo en bucle, pensado también para WebGL:
/// - El vídeo se lee por URL desde StreamingAssets (en WebGL no se pueden usar VideoClip).
/// - Se dibuja en el plano lejano de la cámara: sin RenderTexture extra.
/// - Sin audio, así el navegador permite reproducirlo sin interacción del usuario.
/// - Mientras carga se ve la imagen de respaldo (el primer fotograma); cuando arranca, se desvanece.
/// </summary>
[RequireComponent(typeof(VideoPlayer))]
public class VideoBackground : MonoBehaviour
{
    [Tooltip("Ruta del vídeo dentro de StreamingAssets.")]
    [SerializeField] string videoFile = "Video/IslaFlotante.mp4";
    [SerializeField] Camera targetCamera;
    [Tooltip("Imagen que se ve mientras carga el vídeo o si falla.")]
    [SerializeField] Graphic poster;
    [SerializeField] float posterFadeDuration = 0.4f;

    VideoPlayer player;
    float fade = -1f;

    void Awake()
    {
        player = GetComponent<VideoPlayer>();
        player.playOnAwake = false;
        player.source = VideoSource.Url;
        // En WebGL streamingAssetsPath es una URL: siempre "/", nunca Path.Combine (en Windows mete "\").
        player.url = Application.streamingAssetsPath + "/" + videoFile;
        player.isLooping = true;
        player.skipOnDrop = true;
        player.waitForFirstFrame = true;
        player.audioOutputMode = VideoAudioOutputMode.None;
        player.renderMode = VideoRenderMode.CameraFarPlane;
        player.targetCamera = targetCamera != null ? targetCamera : Camera.main;
        player.aspectRatio = VideoAspectRatio.FitOutside;

        player.prepareCompleted += OnPrepared;
        player.started += OnStarted;
        player.errorReceived += OnError;
    }

    void OnEnable()
    {
        player.Prepare();
    }

    void OnDisable()
    {
        player.Stop();
    }

    void OnDestroy()
    {
        player.prepareCompleted -= OnPrepared;
        player.started -= OnStarted;
        player.errorReceived -= OnError;
    }

    void OnPrepared(VideoPlayer source) => source.Play();

    void OnStarted(VideoPlayer source)
    {
        if (poster != null && fade < 0f)
            fade = 0f;
    }

    void OnError(VideoPlayer source, string message)
    {
        // Se queda la imagen de respaldo.
        Debug.LogWarning($"No se pudo reproducir el fondo de vídeo ({player.url}): {message}", this);
    }

    void Update()
    {
        if (fade < 0f || poster == null)
            return;

        fade = posterFadeDuration > 0f ? Mathf.MoveTowards(fade, 1f, Time.unscaledDeltaTime / posterFadeDuration) : 1f;
        poster.canvasRenderer.SetAlpha(1f - fade);

        if (fade >= 1f)
        {
            poster.enabled = false;
            fade = -1f;
        }
    }
}
