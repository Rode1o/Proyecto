using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Coloca la placa del widget de girar a partir del tamaño de la brújula: cambiando el tamaño
/// de Compass en el prefab, la placa, el ratón, el texto y el contenedor se ajustan solos.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class IslandRotateLayout : MonoBehaviour
{
    [SerializeField] RectTransform compass;
    [SerializeField] Image plaque;
    [SerializeField] LayoutElement mouseHint;
    [SerializeField] TMP_Text label;

    [Header("Proporciones (respecto a la brújula / placa)")]
    [Tooltip("Ancho de la placa respecto al de la brújula.")]
    [SerializeField] float plaqueWidthRatio = 1.18f;
    [Tooltip("Margen transparente sobre la franja de la placa, en fracción de su alto.")]
    [SerializeField] float plaqueTopMargin = 0.24f;
    [Tooltip("Margen transparente bajo la brújula, en fracción de su tamaño.")]
    [SerializeField] float compassBottomMargin = 0.025f;
    [SerializeField] float mouseSizeRatio = 0.68f;
    [SerializeField] float fontSizeRatio = 0.255f;

    float appliedSize = -1f;

    void OnEnable() => Refresh();

    void OnValidate() => appliedSize = -1f;

    void LateUpdate()
    {
        if (compass != null && !Mathf.Approximately(compass.rect.width, appliedSize))
            Refresh();
    }

    public void Refresh()
    {
        if (compass == null || plaque == null || plaque.sprite == null)
            return;

        // La brújula es cuadrada: el ancho manda.
        var size = compass.rect.width;
        SetSize(compass, new Vector2(size, size));

        var sprite = plaque.sprite.rect;
        var plaqueWidth = size * plaqueWidthRatio;
        var plaqueHeight = plaqueWidth * sprite.height / sprite.width;
        SetSize(plaque.rectTransform, new Vector2(plaqueWidth, plaqueHeight));

        var overlap = plaqueHeight * plaqueTopMargin + size * compassBottomMargin;
        SetSize((RectTransform)transform, new Vector2(Mathf.Max(size, plaqueWidth), size + plaqueHeight - overlap));

        var mouseSize = plaqueHeight * mouseSizeRatio;
        if (mouseHint != null && !Mathf.Approximately(mouseHint.preferredWidth, mouseSize))
        {
            mouseHint.minWidth = mouseHint.preferredWidth = mouseSize;
            mouseHint.minHeight = mouseHint.preferredHeight = mouseSize;
        }

        var fontSize = plaqueHeight * fontSizeRatio;
        if (label != null && !Mathf.Approximately(label.fontSize, fontSize))
            label.fontSize = fontSize;

        appliedSize = size;
    }

    // Solo se escribe si cambia: en el editor cada asignación marca la escena como modificada.
    static void SetSize(RectTransform rect, Vector2 size)
    {
        if (rect.sizeDelta != size)
            rect.sizeDelta = size;
    }
}
