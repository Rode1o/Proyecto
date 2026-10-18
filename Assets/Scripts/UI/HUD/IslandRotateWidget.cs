using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Widget del HUD para girar la isla: las flechas giran la cámara orbital por pasos
/// y la pista del ratón (clic derecho) crece mientras el jugador arrastra para girar.
/// </summary>
public class IslandRotateWidget : MonoBehaviour
{
    [Tooltip("Cámara a girar. Vacío = la OrbitCamera de la cámara principal.")]
    [SerializeField] OrbitCamera orbitCamera;
    [SerializeField] Button rotateLeftButton;
    [SerializeField] Button rotateRightButton;
    [Tooltip("Grados que gira la cámara con cada clic en una flecha.")]
    [SerializeField, Range(1f, 180f)] float stepDegrees = 45f;

    [Header("Pista del ratón")]
    [SerializeField] RectTransform mouseHint;
    [SerializeField] float draggingScale = 1.2f;
    [SerializeField] float transitionDuration = 0.12f;

    float blend;

    void Awake()
    {
        if (rotateLeftButton != null)
            rotateLeftButton.onClick.AddListener(() => Rotate(-stepDegrees));
        if (rotateRightButton != null)
            rotateRightButton.onClick.AddListener(() => Rotate(stepDegrees));
    }

    void Update()
    {
        if (mouseHint == null)
            return;

        var camera = ResolveCamera();
        var target = camera != null && camera.IsDragging ? 1f : 0f;
        if (Mathf.Approximately(blend, target))
            return;

        blend = transitionDuration > 0f
            ? Mathf.MoveTowards(blend, target, Time.unscaledDeltaTime / transitionDuration)
            : target;
        var scale = Mathf.Lerp(1f, draggingScale, blend);
        mouseHint.localScale = new Vector3(scale, scale, 1f);
    }

    void Rotate(float degrees)
    {
        var camera = ResolveCamera();
        if (camera != null)
            camera.Rotate(degrees);
    }

    // El HUD es un prefab que se reutiliza entre escenas: la cámara se busca si no está asignada.
    OrbitCamera ResolveCamera()
    {
        if (orbitCamera == null && Camera.main != null)
            orbitCamera = Camera.main.GetComponent<OrbitCamera>();
        return orbitCamera;
    }
}
