using System;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Clima de la escena según el humor de la oveja: cada "no" (<see cref="SheepEnemy.Rejected"/>)
/// oscurece el cielo, la luz, el ambiente y el color de la imagen un paso; al llegar al último, se pone a llover.
/// Pensado para WebGL: un skybox de degradado, ambiente por colores (sin recalcular GI) y una sola
/// lluvia de partículas sin colisiones. Solo trabaja mientras el clima está cambiando.
/// </summary>
public class WeatherController : MonoBehaviour
{
    [Tooltip("Vacío = la SheepEnemy de la escena.")]
    [SerializeField] SheepEnemy sheep;
    [Tooltip("Material con Skybox/Weather Gradient. Se usa una copia para no modificar el asset.")]
    [SerializeField] Material skybox;
    [Tooltip("Vacío = el sol de Lighting (RenderSettings.sun).")]
    [SerializeField] Light sun;
    [SerializeField] ParticleSystem rain;
    [Tooltip("Volume global con el look de tormenta (color, viñeta); su peso sigue a la tormenta.")]
    [SerializeField] Volume stormVolume;

    [Header("Progresión")]
    [Tooltip("Rechazos hasta la tormenta (normalmente, uno por isla con encuentro).")]
    [SerializeField, Min(1)] int rejectionsToStorm = 2;
    [Tooltip("Segundos que tarda el clima en pasar a su nuevo estado.")]
    [SerializeField] float transitionDuration = 3f;

    [Header("Luz en tormenta")]
    [SerializeField] Color stormSunColor = new Color32(0xA5, 0x9F, 0xAC, 0xFF);
    [SerializeField] float stormSunIntensity = 1f;

    [Header("Ambiente (despejado → tormenta)")]
    [SerializeField] Color clearSky = new(0.62f, 0.68f, 0.78f);
    [SerializeField] Color clearEquator = new(0.55f, 0.55f, 0.52f);
    [SerializeField] Color clearGround = new(0.32f, 0.30f, 0.26f);
    [SerializeField] Color stormSky = new Color32(0x48, 0x36, 0x53, 0xFF);
    [SerializeField] Color stormEquator = new Color32(0x3C, 0x30, 0x48, 0xFF);
    [SerializeField] Color stormGround = new Color32(0x30, 0x26, 0x3A, 0xFF);

    [Header("Lluvia")]
    [SerializeField] float rainRate = 450f;
    [SerializeField] float rainFadeIn = 2.5f;
    [Tooltip("Altura del emisor sobre el punto que mira la cámara.")]
    [SerializeField] float rainHeight = 12f;
    [Tooltip("Distancia aproximada de la cámara al punto que mira.")]
    [SerializeField] float focusDistance = 18f;

    static readonly int StormId = Shader.PropertyToID("_Storm");

    Material skyInstance;
    Color clearSunColor;
    float clearSunIntensity;
    Camera mainCamera;
    int rejections;
    float storm;
    float rainAmount;

    /// <summary>0 = despejado, 1 = tormenta. Lo usa también el retrato de la oveja.</summary>
    public float Storm => storm;
    /// <summary>Nivel al que está yendo el clima (sube de golpe con cada rechazo).</summary>
    public float TargetStorm => Mathf.Clamp01((float)rejections / rejectionsToStorm);
    public bool IsRaining => rejections >= rejectionsToStorm;

    public event Action<float> StormChanged;

    void Awake()
    {
        if (sun == null)
            sun = RenderSettings.sun;
        if (sun != null)
        {
            clearSunColor = sun.color;
            clearSunIntensity = sun.intensity;
        }

        if (skybox != null)
        {
            skyInstance = new Material(skybox);
            RenderSettings.skybox = skyInstance;
        }

        // Ambiente por colores: cambiarlo es gratis, a diferencia de recalcular el del skybox.
        RenderSettings.ambientMode = AmbientMode.Trilight;

        if (rain != null)
        {
            var emission = rain.emission;
            emission.rateOverTime = 0f;
            rain.Play();
        }
        Apply();
    }

    void Start()
    {
        mainCamera = Camera.main;
        if (sheep == null)
            sheep = FindAnyObjectByType<SheepEnemy>();
        if (sheep != null)
            sheep.Rejected += OnRejected;
    }

    void OnDestroy()
    {
        if (sheep != null)
            sheep.Rejected -= OnRejected;
        if (skyInstance != null)
            Destroy(skyInstance);
    }

    void OnRejected(SheepEnemy _) => AddRejection();

    /// <summary>Un paso más hacia la tormenta.</summary>
    public void AddRejection() => rejections = Mathf.Min(rejections + 1, rejectionsToStorm);

    void Update()
    {
        var target = TargetStorm;
        if (!Mathf.Approximately(storm, target))
        {
            storm = transitionDuration > 0f
                ? Mathf.MoveTowards(storm, target, Time.deltaTime / transitionDuration)
                : target;
            Apply();
            StormChanged?.Invoke(storm);
        }

        if (rain == null)
            return;

        var rainTarget = IsRaining && Mathf.Approximately(storm, 1f) ? 1f : 0f;
        if (!Mathf.Approximately(rainAmount, rainTarget))
        {
            rainAmount = Mathf.MoveTowards(rainAmount, rainTarget, Time.deltaTime / rainFadeIn);
            var emission = rain.emission;
            emission.rateOverTime = rainRate * rainAmount;
        }
    }

    void LateUpdate()
    {
        // La lluvia sigue a la cámara solo mientras hay gotas.
        if (rain == null || mainCamera == null || (rainAmount <= 0f && rain.particleCount == 0))
            return;

        var camTransform = mainCamera.transform;
        var forward = camTransform.forward;
        var focus = camTransform.position + forward * focusDistance;
        rain.transform.SetPositionAndRotation(focus + Vector3.up * rainHeight, Quaternion.identity);
    }

    void Apply()
    {
        if (skyInstance != null)
            skyInstance.SetFloat(StormId, storm);

        // El grading se mezcla en la misma pasada final de URP: subir el peso no añade coste.
        if (stormVolume != null)
            stormVolume.weight = storm;

        if (sun != null)
        {
            sun.color = Color.Lerp(clearSunColor, stormSunColor, storm);
            sun.intensity = Mathf.Lerp(clearSunIntensity, stormSunIntensity, storm);
        }

        RenderSettings.ambientSkyColor = Color.Lerp(clearSky, stormSky, storm);
        RenderSettings.ambientEquatorColor = Color.Lerp(clearEquator, stormEquator, storm);
        RenderSettings.ambientGroundColor = Color.Lerp(clearGround, stormGround, storm);
    }
}
