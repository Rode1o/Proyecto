using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

/// <summary>
/// Cámara que gira 360° en horizontal alrededor de un objetivo mientras se arrastra con clic derecho.
/// La inclinación es fija; la distancia se acorta si hay geometría entre el objetivo y la cámara.
/// </summary>
public class OrbitCamera : MonoBehaviour
{
    [Header("Objetivo")]
    [Tooltip("Punto de interés que la cámara sigue y orbita (por defecto, el jugador).")]
    [FormerlySerializedAs("pivot")]
    [SerializeField] Transform target;
    [Tooltip("Desplazamiento del punto de mira respecto al objetivo, en espacio mundo.")]
    [SerializeField] Vector3 targetOffset = new(0f, 1f, 0f);
    [Tooltip("Suavizado del seguimiento del objetivo en segundos (0 = pegado al objetivo).")]
    [SerializeField] float followSmoothTime = 0.15f;

    [Header("Encuadre")]
    [SerializeField, Min(1f)] float distance = 20f;
    [SerializeField, Range(10f, 85f)] float pitch = 45f;
    [Tooltip("Ángulo horizontal inicial.")]
    [SerializeField] float yaw;

    [Header("Control")]
    [Tooltip("Grados por píxel de arrastre.")]
    [SerializeField] float sensitivity = 0.3f;
    [SerializeField] bool invert;
    [Tooltip("Suavizado del giro en segundos (0 = sin suavizado).")]
    [SerializeField] float smoothTime = 0.08f;

    [Header("Colisión")]
    [Tooltip("Capas que bloquean la cámara. El objetivo no debe tener collider en estas capas.")]
    [SerializeField] LayerMask collisionMask = ~((1 << 2) | (1 << 5)); // Todo menos Ignore Raycast y UI
    [Tooltip("Radio de la esfera usada para detectar geometría.")]
    [SerializeField] float collisionRadius = 0.3f;
    [Tooltip("Distancia mínima al objetivo aunque haya geometría.")]
    [SerializeField] float minDistance = 2f;
    [Tooltip("Velocidad con la que la cámara vuelve a su distancia al dejar de estar bloqueada.")]
    [SerializeField] float returnSpeed = 10f;

    Vector3 focus;
    Vector3 focusVelocity;
    float currentYaw;
    float yawVelocity;
    float currentDistance;
    bool dragging;

    void Start()
    {
        focus = TargetPoint();
        currentYaw = yaw;
        currentDistance = distance;
        Apply();
    }

    void LateUpdate()
    {
        UpdateInput();

        currentYaw = smoothTime > 0f
            ? Mathf.SmoothDampAngle(currentYaw, yaw, ref yawVelocity, smoothTime)
            : yaw;

        // Se sigue en LateUpdate para que el NavMeshAgent ya haya movido al jugador este frame.
        focus = followSmoothTime > 0f
            ? Vector3.SmoothDamp(focus, TargetPoint(), ref focusVelocity, followSmoothTime)
            : TargetPoint();

        Apply();
    }

    void UpdateInput()
    {
        var mouse = Mouse.current;
        if (mouse == null)
        {
            dragging = false;
            return;
        }

        // Solo el botón derecho gira. Un arrastre que empieza sobre la UI se ignora hasta soltar.
        if (mouse.rightButton.wasPressedThisFrame)
            dragging = EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject();
        else if (!mouse.rightButton.isPressed)
            dragging = false;

        if (!dragging)
            return;

        var deltaX = mouse.delta.ReadValue().x * sensitivity;
        yaw = Mathf.Repeat(yaw + (invert ? -deltaX : deltaX), 360f);
    }

    // Al perder el foco (alt-tab, clic fuera del canvas WebGL) se corta el arrastre.
    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
            dragging = false;
    }

    void OnDisable()
    {
        dragging = false;
    }

    Vector3 TargetPoint() => target != null ? target.position + targetOffset : transform.position;

    void Apply()
    {
        if (target == null)
            return;

        var rotation = Quaternion.Euler(pitch, currentYaw, 0f);
        var back = rotation * Vector3.back;

        var wanted = distance;
        if (Physics.SphereCast(focus, collisionRadius, back, out var hit, distance, collisionMask, QueryTriggerInteraction.Ignore))
            wanted = Mathf.Max(minDistance, hit.distance);

        // Acercarse es inmediato para no atravesar nada; alejarse es suave para evitar saltos.
        currentDistance = wanted < currentDistance || !Application.isPlaying
            ? wanted
            : Mathf.MoveTowards(currentDistance, wanted, returnSpeed * Time.deltaTime);

        transform.SetPositionAndRotation(focus + back * currentDistance, rotation);
    }

#if UNITY_EDITOR
    // Previsualiza el encuadre en la escena al cambiar valores en el Inspector.
    void OnValidate()
    {
        if (Application.isPlaying)
            return;
        focus = TargetPoint();
        currentYaw = yaw;
        currentDistance = distance;
        Apply();
    }
#endif
}
