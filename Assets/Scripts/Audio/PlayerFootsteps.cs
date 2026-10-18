using FMODUnity;
using UnityEngine;

/// <summary>
/// Lanza el evento de pasos de FMOD cada cierta distancia recorrida en el suelo.
/// La superficie se lee del FootstepSurface que haya bajo los pies y se pasa como parámetro del evento.
/// </summary>
public class PlayerFootsteps : MonoBehaviour
{
    [SerializeField] EventReference footstepEvent;
    [Tooltip("Nombre del parámetro de superficie en FMOD Studio (0 = Grass, 1 = Dirt, 2 = Stone).")]
    [SerializeField] string surfaceParameter = "Parameter 1";
    [Tooltip("Superficie que suena si debajo no hay ningún FootstepSurface.")]
    [SerializeField] SurfaceType defaultSurface = SurfaceType.Grass;

    [Header("Cadencia")]
    [Tooltip("Metros recorridos entre un paso y el siguiente.")]
    [SerializeField] float strideLength = 0.7f;
    [Tooltip("Velocidad mínima (m/s) para considerar que el jugador camina.")]
    [SerializeField] float minSpeed = 0.2f;

    [Header("Detección del suelo")]
    [SerializeField] LayerMask groundMask = ~0;
    [SerializeField] float groundCheckDistance = 0.4f;

    Vector3 lastPosition;
    float distanceSinceStep;

    void OnEnable()
    {
        lastPosition = transform.position;
        distanceSinceStep = 0f;
    }

    void Update()
    {
        var position = transform.position;
        var delta = position - lastPosition;
        delta.y = 0f;
        lastPosition = position;

        var distance = delta.magnitude;
        if (Time.deltaTime <= 0f || distance / Time.deltaTime < minSpeed)
            return;

        if (!TryGetSurface(out var surface))
            return;

        distanceSinceStep += distance;
        if (distanceSinceStep < strideLength)
            return;

        // Con el módulo, un salto grande (p. ej. un teletransporte) da un solo paso en vez de acumular deuda.
        distanceSinceStep %= strideLength;
        PlayStep(surface);
    }

    bool TryGetSurface(out SurfaceType surface)
    {
        surface = defaultSurface;
        var origin = transform.position + Vector3.up * 0.1f;
        if (!Physics.Raycast(origin, Vector3.down, out var hit, groundCheckDistance + 0.1f, groundMask, QueryTriggerInteraction.Ignore))
            return false;

        var marker = hit.collider.GetComponentInParent<FootstepSurface>();
        if (marker != null)
            surface = marker.GetSurface(hit);
        return true;
    }

    void PlayStep(SurfaceType surface)
    {
        if (footstepEvent.IsNull)
            return;

        var instance = RuntimeManager.CreateInstance(footstepEvent);
        instance.set3DAttributes(RuntimeUtils.To3DAttributes(transform.position));
        instance.setParameterByName(surfaceParameter, (float)surface);
        instance.start();
        instance.release();
    }
}
