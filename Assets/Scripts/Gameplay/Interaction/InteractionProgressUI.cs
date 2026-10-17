using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Indicador radial que sigue en pantalla al objeto con el que interactúa el jugador
/// y se llena mientras se mantiene el clic. Va en un Canvas Screen Space - Overlay.
/// </summary>
public class InteractionProgressUI : MonoBehaviour
{
    [SerializeField] PlayerInteractor interactor;
    [SerializeField] Camera viewCamera;
    [Tooltip("Raíz del indicador; se activa y desactiva según haya progreso.")]
    [SerializeField] RectTransform indicator;
    [Tooltip("Image con Image Type = Filled (Radial 360).")]
    [SerializeField] Image fill;

    void Awake()
    {
        if (viewCamera == null)
            viewCamera = Camera.main;
        indicator.gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        var target = interactor != null ? interactor.Target : null;
        var visible = target != null && target.Progress > 0f;

        Vector3 screen = default;
        if (visible)
        {
            screen = viewCamera.WorldToScreenPoint(target.IndicatorPosition);
            visible = screen.z > 0f; // Detrás de la cámara no se dibuja.
        }

        if (indicator.gameObject.activeSelf != visible)
            indicator.gameObject.SetActive(visible);
        if (!visible)
            return;

        indicator.position = screen;
        fill.fillAmount = target.Progress;
    }
}
