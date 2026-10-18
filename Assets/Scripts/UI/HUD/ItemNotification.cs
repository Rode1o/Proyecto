using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Aviso "objeto recogido" a la izquierda de la pantalla: icono y nombre en una barra que entra
/// deslizando desde fuera, se queda un momento y vuelve a salir. Los avisos seguidos se encolan.
/// Escucha a PlayerCollection; <see cref="Show"/> sirve para cualquier otro aviso.
/// </summary>
public class ItemNotification : MonoBehaviour
{
    [Tooltip("Vacío = el PlayerCollection de la escena.")]
    [SerializeField] PlayerCollection collection;
    [Tooltip("Barra que se anima (pivote a la izquierda).")]
    [SerializeField] RectTransform bar;
    [SerializeField] CanvasGroup group;
    [SerializeField] Image icon;
    [SerializeField] TMP_Text label;

    [Header("Animación")]
    [Tooltip("Posición X de la barra cuando está a la vista.")]
    [SerializeField] float shownX = 60f;
    [SerializeField] float enterDuration = 0.45f;
    [SerializeField] float holdDuration = 2.2f;
    [Tooltip("Tiempo a la vista cuando hay más avisos esperando.")]
    [SerializeField] float queuedHoldDuration = 1f;
    [SerializeField] float exitDuration = 0.3f;
    [SerializeField] float iconPopScale = 1.35f;

    enum Phase { Hidden, Entering, Holding, Exiting }

    readonly Queue<(Sprite icon, string text)> pending = new();
    Phase phase = Phase.Hidden;
    float time;

    float HiddenX => -bar.rect.width - 20f;

    void Awake()
    {
        SetPose(0f);
        bar.gameObject.SetActive(false);
    }

    void Start()
    {
        if (collection == null)
            collection = FindAnyObjectByType<PlayerCollection>();
        if (collection != null)
            collection.ItemCollected += OnItemCollected;
    }

    void OnDestroy()
    {
        if (collection != null)
            collection.ItemCollected -= OnItemCollected;
    }

    void OnItemCollected(Collectable collectable)
    {
        var definition = collectable.Definition;
        Show(definition != null ? definition.Icon : null,
            definition != null ? definition.DisplayName : collectable.name);
    }

    public void Show(Sprite iconSprite, string text)
    {
        pending.Enqueue((iconSprite, text));
        if (phase == Phase.Hidden)
            ShowNext();
    }

    void ShowNext()
    {
        var (iconSprite, text) = pending.Dequeue();
        icon.sprite = iconSprite;
        icon.gameObject.SetActive(iconSprite != null);
        label.text = text;

        bar.gameObject.SetActive(true);
        phase = Phase.Entering;
        time = 0f;
    }

    void Update()
    {
        if (phase == Phase.Hidden)
            return;

        time += Time.unscaledDeltaTime;
        switch (phase)
        {
            case Phase.Entering:
                var t = Mathf.Clamp01(time / enterDuration);
                SetPose(EaseOutBack(t));
                group.alpha = Mathf.Clamp01(t * 2f);
                // El icono salta al final de la entrada.
                var pop = Mathf.Clamp01((t - 0.5f) * 2f);
                var scale = 1f + (iconPopScale - 1f) * Mathf.Sin(pop * Mathf.PI);
                icon.rectTransform.localScale = new Vector3(scale, scale, 1f);
                if (t >= 1f)
                    Next(Phase.Holding);
                break;

            case Phase.Holding:
                if (time >= (pending.Count > 0 ? queuedHoldDuration : holdDuration))
                    Next(Phase.Exiting);
                break;

            case Phase.Exiting:
                var e = Mathf.Clamp01(time / exitDuration);
                SetPose(1f - e * e);
                group.alpha = 1f - e;
                if (e >= 1f)
                {
                    phase = Phase.Hidden;
                    bar.gameObject.SetActive(false);
                    if (pending.Count > 0)
                        ShowNext();
                }
                break;
        }
    }

    void Next(Phase value)
    {
        phase = value;
        time = 0f;
    }

    // 0 = fuera de pantalla, 1 = a la vista (puede pasarse un poco con el rebote).
    void SetPose(float shown)
    {
        var position = bar.anchoredPosition;
        position.x = Mathf.LerpUnclamped(HiddenX, shownX, shown);
        bar.anchoredPosition = position;
    }

    static float EaseOutBack(float t)
    {
        const float c1 = 1.4f;
        const float c3 = c1 + 1f;
        var x = t - 1f;
        return 1f + c3 * x * x * x + c1 * x * x;
    }
}
