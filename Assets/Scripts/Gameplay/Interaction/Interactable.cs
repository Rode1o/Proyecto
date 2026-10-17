using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

/// <summary>
/// Marca un objeto del escenario como interactuable: el jugador se acerca y mantiene el clic
/// izquierdo durante <see cref="HoldDuration"/> segundos (o, sin requireHold, basta con llegar).
/// Al completarse lanza <see cref="Completed"/>
/// y <c>onCompleted</c>; quien abra el contenido (un minijuego, por ejemplo) se suscribe a esos eventos.
/// Necesita un Collider no trigger en este objeto o en sus hijos.
/// </summary>
[DisallowMultipleComponent]
public class Interactable : MonoBehaviour
{
    [Tooltip("Si está desactivado, la interacción se completa sola al entrar en rango (p. ej. collectables).")]
    [SerializeField] bool requireHold = true;
    [Tooltip("Segundos que hay que mantener pulsado el clic para completar la interacción.")]
    [SerializeField, Min(0f)] float holdDuration = 2f;
    [Tooltip("Distancia horizontal máxima entre el jugador y el punto de interacción.")]
    [SerializeField, Min(0.1f)] float activationRadius = 1.8f;
    [Tooltip("Dónde se coloca el jugador para interactuar. Vacío = alrededor del objeto, por el lado del jugador.")]
    [SerializeField] Transform interactionPoint;
    [Tooltip("Posición del indicador de progreso respecto al objeto.")]
    [SerializeField] Vector3 indicatorOffset = new(0f, 2f, 0f);
    [Tooltip("Permite completar la interacción más de una vez.")]
    [SerializeField] bool repeatable;
    [Tooltip("Para objetos apoyados en suelo transitable (collectables): el jugador camina hasta el propio objeto " +
             "y solo interactúa si hay línea directa sobre el NavMesh, así no se atraviesan muros ni se alcanza otra isla.")]
    [SerializeField] bool onWalkableGround;

    [SerializeField] UnityEvent onCompleted;

    public event Action<Interactable> Completed;

    public bool RequiresHold => requireHold;
    public float HoldDuration => holdDuration;
    public float ActivationRadius => activationRadius;
    public float Progress { get; private set; }
    public bool IsCompleted { get; private set; }
    public bool CanInteract => isActiveAndEnabled && (repeatable || !IsCompleted);
    public Vector3 InteractionPosition => interactionPoint != null ? interactionPoint.position : transform.position;
    public Vector3 IndicatorPosition => transform.position + indicatorOffset;

    public float HorizontalDistance(Vector3 position)
    {
        var offset = position - InteractionPosition;
        offset.y = 0f;
        return offset.magnitude;
    }

    public bool IsInRange(Vector3 position) => HorizontalDistance(position) <= activationRadius;

    IInteractionRequirement[] requirements = Array.Empty<IInteractionRequirement>();

    void Awake()
    {
        requirements = GetComponents<IInteractionRequirement>();
    }

    /// <summary>
    /// Comprueba los <see cref="IInteractionRequirement"/> del objeto (p. ej. recoger los objetos de la isla).
    /// </summary>
    public bool RequirementsMet(out string reason)
    {
        foreach (var requirement in requirements)
            if (!requirement.IsMet(out reason))
                return false;

        reason = null;
        return true;
    }

    /// <summary>
    /// En rango y, si el objeto está sobre suelo transitable, con línea directa sobre el NavMesh
    /// (un muro o el vacío entre islas cortan la línea).
    /// </summary>
    public bool IsReachableFrom(Vector3 position)
    {
        if (!IsInRange(position))
            return false;
        if (!onWalkableGround)
            return true;

        return NavMesh.SamplePosition(position, out var from, 1f, NavMesh.AllAreas)
               && NavMesh.SamplePosition(transform.position, out var to, 1f, NavMesh.AllAreas)
               && !NavMesh.Raycast(from.position, to.position, out _, NavMesh.AllAreas);
    }

    /// <summary>
    /// Punto al que debe caminar el jugador. Sin interactionPoint, se elige dentro del radio
    /// en la dirección desde la que llega el jugador; sobre suelo transitable, el propio objeto.
    /// </summary>
    public Vector3 GetApproachPoint(Vector3 from)
    {
        if (interactionPoint != null)
            return interactionPoint.position;
        if (onWalkableGround)
            return transform.position;

        var direction = from - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
            direction = -transform.forward;

        return transform.position + direction.normalized * (activationRadius * 0.6f);
    }

    /// <summary>
    /// Avanza el progreso de la pulsación. Devuelve true en el frame en que se completa.
    /// </summary>
    public bool AdvanceHold(float deltaTime)
    {
        if (!CanInteract || !RequirementsMet(out _))
            return false;

        Progress = requireHold && holdDuration > 0f ? Mathf.Clamp01(Progress + deltaTime / holdDuration) : 1f;
        if (Progress < 1f)
            return false;

        Complete();
        return true;
    }

    public void CancelHold() => Progress = 0f;

    /// <summary>
    /// Vuelve a dejar el objeto disponible (por ejemplo, si el minijuego se cierra sin resolver).
    /// </summary>
    public void ResetInteraction()
    {
        IsCompleted = false;
        Progress = 0f;
    }

    void Complete()
    {
        IsCompleted = true;
        Progress = 0f;
        Debug.Log($"Interacción completada: {name}", this);
        Completed?.Invoke(this);
        onCompleted.Invoke();
    }

    void OnDisable()
    {
        Progress = 0f;
    }

    /// <summary>
    /// Devuelve el interactuable disponible que golpea primero el rayo, o null.
    /// Lo usan PlayerInteractor y ClickToMove para repartirse el clic izquierdo.
    /// </summary>
    public static Interactable FindUnderRay(Ray ray, float maxDistance)
    {
        if (!Physics.Raycast(ray, out var hit, maxDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            return null;

        var interactable = hit.collider.GetComponentInParent<Interactable>();
        return interactable != null && interactable.CanInteract ? interactable : null;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.8f, 0.2f);
        var center = InteractionPosition;
        const int segments = 32;
        for (int i = 0; i < segments; i++)
        {
            var a = Quaternion.Euler(0f, i * 360f / segments, 0f) * Vector3.forward * activationRadius;
            var b = Quaternion.Euler(0f, (i + 1) * 360f / segments, 0f) * Vector3.forward * activationRadius;
            Gizmos.DrawLine(center + a, center + b);
        }
        Gizmos.DrawWireSphere(IndicatorPosition, 0.15f);
    }
}
