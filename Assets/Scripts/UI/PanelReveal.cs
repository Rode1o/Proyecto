using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Abre y cierra un panel desplegándolo de izquierda a derecha, como un pergamino.
/// El RectMask2D recorta el contenido; su padding derecho se anima de "todo tapado" a 0.
/// </summary>
[RequireComponent(typeof(RectMask2D), typeof(CanvasGroup))]
public class PanelReveal : MonoBehaviour
{
    [SerializeField] float openDuration = 0.45f;
    [SerializeField] float closeDuration = 0.25f;
    [SerializeField] int edgeSoftness = 60;

    RectMask2D mask;
    CanvasGroup group;
    RectTransform rect;
    float progress;
    int direction;

    public bool IsOpen { get; private set; }

    void Awake()
    {
        EnsureInitialized();
    }

    // Show/Hide pueden llamarse con el panel aún inactivo (antes de su Awake).
    void EnsureInitialized()
    {
        if (mask != null)
            return;

        mask = GetComponent<RectMask2D>();
        group = GetComponent<CanvasGroup>();
        rect = (RectTransform)transform;
        mask.softness = new Vector2Int(edgeSoftness, 0);
    }

    public void Show()
    {
        if (IsOpen)
            return;

        EnsureInitialized();
        IsOpen = true;
        direction = 1;
        gameObject.SetActive(true);
        group.interactable = true;
        group.blocksRaycasts = true;
        Apply();
    }

    public void Hide(bool instant = false)
    {
        if (!IsOpen && !instant)
            return;

        EnsureInitialized();
        IsOpen = false;
        direction = -1;
        group.interactable = false;
        group.blocksRaycasts = false;

        if (instant || !gameObject.activeInHierarchy)
        {
            progress = 0f;
            direction = 0;
            Apply();
            gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (direction == 0)
            return;

        var target = direction > 0 ? 1f : 0f;
        var duration = direction > 0 ? openDuration : closeDuration;
        progress = duration > 0f ? Mathf.MoveTowards(progress, target, Time.unscaledDeltaTime / duration) : target;
        Apply();

        if (Mathf.Approximately(progress, target))
        {
            if (direction < 0)
                gameObject.SetActive(false);
            direction = 0;
        }
    }

    void Apply()
    {
        // Ease-out cúbico: arranca rápido y se asienta suave.
        var eased = 1f - Mathf.Pow(1f - progress, 3f);
        var width = rect.rect.width + edgeSoftness;
        mask.padding = new Vector4(-edgeSoftness, 0f, width * (1f - eased), 0f);
        group.alpha = Mathf.Clamp01(progress * 4f);
    }
}
