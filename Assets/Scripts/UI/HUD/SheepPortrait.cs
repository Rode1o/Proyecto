using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Retrato de la oveja en el HUD. La cara sigue el estado de <see cref="SheepEnemy"/>
/// (feliz esperando, astuta al proponer o al ganar, enfurecida tras un "no") y el relleno se oscurece
/// un paso con cada rechazo: el clima empeora.
/// </summary>
public class SheepPortrait : MonoBehaviour
{
    public enum Mood { Happy, Sly, Angry }

    [Tooltip("Vacío = la SheepEnemy de la escena.")]
    [SerializeField] SheepEnemy sheep;
    [SerializeField] Image character;
    [Tooltip("Relleno con el material UI/Portrait Mood Fill.")]
    [SerializeField] Graphic fill;
    [Tooltip("Otros rellenos que comparten el mismo clima (p. ej. el retrato del cuadro de diálogo).")]
    [SerializeField] Graphic[] linkedFills;

    [Header("Caras")]
    [SerializeField] Sprite happy;
    [SerializeField] Sprite sly;
    [SerializeField] Sprite angry;

    [Header("Clima")]
    [Tooltip("Vacío = el WeatherController de la escena. Si hay uno, el retrato sigue su tormenta.")]
    [SerializeField] WeatherController weather;
    [Tooltip("Sin WeatherController: rechazos necesarios para llegar al relleno más oscuro.")]
    [SerializeField, Min(1)] int rejectionsToDark = 3;
    [Tooltip("Segundos que tarda el relleno en pasar a su nuevo tono.")]
    [SerializeField] float darkenDuration = 1.5f;

    [Header("Cambio de cara")]
    [SerializeField] float punchScale = 1.12f;
    [SerializeField] float punchDuration = 0.25f;

    static readonly int DarknessId = Shader.PropertyToID("_Darkness");

    Material fillMaterial;
    Mood mood;
    int rejections;
    float darkness;
    float punchTime = -1f;

    public Mood CurrentMood => mood;
    public float Darkness => darkness;

    void Awake()
    {
        // Instancia propia: el material del prefab no cambia al jugar.
        if (fill != null && fill.material != null)
        {
            fillMaterial = new Material(fill.material);
            fill.material = fillMaterial;
            foreach (var linked in linkedFills)
                if (linked != null)
                    linked.material = fillMaterial;
        }
        ApplyMood(Mood.Happy, punch: false);
        ApplyDarkness();
    }

    void Start()
    {
        if (sheep == null)
            sheep = FindAnyObjectByType<SheepEnemy>();
        if (sheep != null)
            sheep.Rejected += OnRejected;
        if (weather == null)
            weather = FindAnyObjectByType<WeatherController>();
    }

    void OnDestroy()
    {
        if (sheep != null)
            sheep.Rejected -= OnRejected;
        if (fillMaterial != null)
            Destroy(fillMaterial);
    }

    void Update()
    {
        if (sheep != null)
            SetMood(MoodFor(sheep.CurrentState));

        // Con clima en la escena, el relleno va a la par que el cielo; sin él, cuenta sus rechazos.
        var target = weather != null ? weather.Storm : Mathf.Clamp01((float)rejections / rejectionsToDark);
        if (!Mathf.Approximately(darkness, target))
        {
            darkness = weather != null || darkenDuration <= 0f
                ? target
                : Mathf.MoveTowards(darkness, target, Time.deltaTime / darkenDuration);
            ApplyDarkness();
        }

        if (punchTime >= 0f)
        {
            punchTime += Time.unscaledDeltaTime;
            var t = Mathf.Clamp01(punchTime / punchDuration);
            var scale = 1f + (punchScale - 1f) * Mathf.Sin(t * Mathf.PI);
            character.rectTransform.localScale = new Vector3(scale, scale, 1f);
            if (t >= 1f)
                punchTime = -1f;
        }
    }

    /// <summary>Cambia la cara a mano (sin SheepEnemy en la escena, o para escenas especiales).</summary>
    public void SetMood(Mood value)
    {
        if (value != mood)
            ApplyMood(value, punch: true);
    }

    /// <summary>Suma un rechazo: el relleno se oscurece un paso.</summary>
    public void AddRejection() => rejections++;

    void OnRejected(SheepEnemy _) => AddRejection();

    Mood MoodFor(SheepEnemy.State state) => state switch
    {
        SheepEnemy.State.Approaching or SheepEnemy.State.Talking
            or SheepEnemy.State.LeadingHome or SheepEnemy.State.Finished => Mood.Sly,
        SheepEnemy.State.Angry or SheepEnemy.State.Retreating or SheepEnemy.State.Sulking => Mood.Angry,
        // Al mudarse de casa conserva la cara que tenía (amable o enfadada).
        SheepEnemy.State.Relocating => mood,
        _ => Mood.Happy,
    };

    void ApplyMood(Mood value, bool punch)
    {
        mood = value;
        character.sprite = value switch
        {
            Mood.Sly => sly,
            Mood.Angry => angry,
            _ => happy,
        };
        if (punch)
            punchTime = 0f;
    }

    void ApplyDarkness()
    {
        if (fillMaterial != null)
            fillMaterial.SetFloat(DarknessId, darkness);
    }
}
