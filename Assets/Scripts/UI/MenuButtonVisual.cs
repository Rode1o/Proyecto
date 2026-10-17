using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Dos estados visuales para los botones del menú: inactivo y activo.
/// El botón está activo cuando está seleccionado o fijado (Pinned: la pestaña o el panel abierto).
/// Pasar el ratón por encima lo selecciona, así el ratón, el teclado y el mando se comportan igual.
/// </summary>
[RequireComponent(typeof(Selectable))]
public class MenuButtonVisual : MonoBehaviour,
    IPointerEnterHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] Graphic inactiveFrame;
    [SerializeField] Graphic activeFrame;
    [SerializeField] TMP_Text label;

    [Tooltip("Icono que solo cambia de color entre estados (opcional).")]
    [SerializeField] Graphic icon;

    [Tooltip("Iconos distintos por estado, p. ej. versión blanca y negra (opcional).")]
    [SerializeField] Graphic inactiveIcon;
    [SerializeField] Graphic activeIcon;

    [Header("Colores")]
    [SerializeField] Color labelInactiveColor = new Color32(243, 230, 207, 255);
    [SerializeField] Color labelActiveColor = new Color32(43, 26, 16, 255);
    [SerializeField] Color iconInactiveColor = new Color(1f, 1f, 1f, 0.45f);
    [SerializeField] Color iconActiveColor = Color.white;

    [Header("Animación")]
    [SerializeField] float activeScale = 1.04f;
    [SerializeField] float pressedScale = 0.97f;
    [SerializeField] float transitionDuration = 0.12f;

    Selectable selectable;
    bool selected;
    bool pinned;
    bool pressed;
    float blend;

    /// <summary>Mantiene el estado activo aunque el botón no esté seleccionado.</summary>
    public bool Pinned
    {
        get => pinned;
        set => pinned = value;
    }

    bool IsActive => selected || pinned;

    void Awake()
    {
        selectable = GetComponent<Selectable>();
    }

    void OnEnable()
    {
        selected = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject;
        pressed = false;
        blend = IsActive ? 1f : 0f;
        Apply();
    }

    void Update()
    {
        var target = IsActive ? 1f : 0f;
        if (Mathf.Approximately(blend, target))
            return;

        blend = transitionDuration > 0f
            ? Mathf.MoveTowards(blend, target, Time.unscaledDeltaTime / transitionDuration)
            : target;
        Apply();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (selectable.IsInteractable() && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(gameObject);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        pressed = true;
        Apply();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        pressed = false;
        Apply();
    }

    public void OnSelect(BaseEventData eventData) => selected = true;

    public void OnDeselect(BaseEventData eventData) => selected = false;

    void Apply()
    {
        // SetAlpha en el CanvasRenderer no reconstruye la malla.
        if (activeFrame != null)
            activeFrame.canvasRenderer.SetAlpha(blend);
        if (inactiveFrame != null)
            inactiveFrame.canvasRenderer.SetAlpha(1f - blend);
        if (activeIcon != null)
            activeIcon.canvasRenderer.SetAlpha(blend);
        if (inactiveIcon != null)
            inactiveIcon.canvasRenderer.SetAlpha(1f - blend);
        if (label != null)
            label.color = Color.Lerp(labelInactiveColor, labelActiveColor, blend);
        if (icon != null)
            icon.color = Color.Lerp(iconInactiveColor, iconActiveColor, blend);

        var scale = Mathf.Lerp(1f, activeScale, blend) * (pressed ? pressedScale : 1f);
        transform.localScale = new Vector3(scale, scale, 1f);
    }
}
