using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Gestiona la interacción del jugador con objetos <see cref="Interactable"/>:
/// clic izquierdo sobre el objeto → si está lejos, camina hasta él con ClickToMove →
/// ya en rango, mantener el clic llena el progreso.
/// Política de cancelación: soltar el clic, pulsar el derecho o alejarse reinicia el progreso a 0.
/// </summary>
[RequireComponent(typeof(ClickToMove))]
public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] Camera viewCamera;
    [SerializeField] float maxRayDistance = 200f;
    [Tooltip("Margen sobre el radio de activación antes de cancelar, para evitar cortes por pequeñas variaciones.")]
    [SerializeField] float cancelMargin = 0.5f;

    ClickToMove mover;
    Interactable target;
    bool approaching;
    // El clic izquierdo se pulsó sobre el objetivo y sigue mantenido.
    bool pressHeld;

    public Interactable Target => target;
    public bool IsHolding => target != null && target.Progress > 0f;

    void Awake()
    {
        mover = GetComponent<ClickToMove>();
        if (viewCamera == null)
            viewCamera = Camera.main;
    }

    void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null)
            return;

        if (mouse.leftButton.wasPressedThisFrame)
            OnLeftPressed(mouse);
        if (!mouse.leftButton.isPressed)
            pressHeld = false;

        if (target == null)
            return;
        if (!target.CanInteract)
        {
            Clear();
            return;
        }

        var distance = target.HorizontalDistance(transform.position);

        if (approaching)
        {
            if (distance <= target.ActivationRadius)
            {
                approaching = false;
                mover.Stop();
            }
            else if (mover.HasArrived)
            {
                Debug.LogWarning($"No se alcanza el radio de activación de '{target.name}'. Aumenta su radio o asigna un interactionPoint sobre el NavMesh.", target);
                Clear();
            }
            return;
        }

        if (distance > target.ActivationRadius + cancelMargin)
        {
            Clear();
            return;
        }

        if (!pressHeld || mouse.rightButton.isPressed)
        {
            target.CancelHold();
            return;
        }

        if (target.AdvanceHold(Time.deltaTime))
            Clear();
    }

    void OnLeftPressed(Mouse mouse)
    {
        // Mientras se gira la cámara no se inicia ninguna interacción.
        if (mouse.rightButton.isPressed)
            return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        var ray = viewCamera.ScreenPointToRay(mouse.position.ReadValue());
        var clicked = Interactable.FindUnderRay(ray, maxRayDistance);

        // Clic en el terreno u otro objeto: ClickToMove se encarga del movimiento.
        if (clicked == null)
        {
            Clear();
            return;
        }

        if (clicked != target)
        {
            Clear();
            target = clicked;
        }

        pressHeld = true;

        if (target.IsInRange(transform.position))
        {
            approaching = false;
            mover.Stop();
            return;
        }

        approaching = mover.MoveTo(target.GetApproachPoint(transform.position));
        if (!approaching)
            Clear();
    }

    void Clear()
    {
        if (target != null)
            target.CancelHold();
        target = null;
        approaching = false;
        pressHeld = false;
    }

    void OnDisable()
    {
        Clear();
    }
}
