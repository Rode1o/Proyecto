using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Mueve al jugador con clic izquierdo sobre el terreno.
/// El NavMeshAgent calcula la ruta sobre el NavMesh horneado y rodea los obstáculos.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class ClickToMove : MonoBehaviour
{
    [SerializeField] Camera viewCamera;
    [Tooltip("Capas sobre las que se puede hacer clic para caminar.")]
    [SerializeField] LayerMask groundMask = ~0;
    [SerializeField] float maxRayDistance = 200f;
    [Tooltip("Distancia máxima para ajustar el clic al punto del NavMesh más cercano.")]
    [SerializeField] float maxSnapDistance = 1.5f;

    [Header("Opcional")]
    [Tooltip("Marcador que se coloca en el destino y se oculta al llegar.")]
    [SerializeField] Transform destinationMarker;

    NavMeshAgent agent;

    public bool HasArrived =>
        !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (viewCamera == null)
            viewCamera = Camera.main;
    }

    void Start()
    {
        SetMarkerVisible(false);
    }

    void Update()
    {
        if (destinationMarker != null && destinationMarker.gameObject.activeSelf && HasArrived)
            SetMarkerVisible(false);

        var mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
            return;

        // Un clic sobre la UI no debe mover al jugador.
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        var ray = viewCamera.ScreenPointToRay(mouse.position.ReadValue());

        // Los clics sobre objetos interactivos los gestiona PlayerInteractor.
        if (Interactable.FindUnderRay(ray, maxRayDistance) != null)
            return;

        if (Physics.Raycast(ray, out var hit, maxRayDistance, groundMask, QueryTriggerInteraction.Ignore))
            MoveTo(hit.point);
    }

    /// <summary>
    /// Envía al jugador al punto del NavMesh más cercano. Devuelve false si no hay ruta.
    /// </summary>
    public bool MoveTo(Vector3 point)
    {
        if (!NavMesh.SamplePosition(point, out var navHit, maxSnapDistance, agent.areaMask))
            return false;

        var path = new NavMeshPath();
        if (!agent.CalculatePath(navHit.position, path) || path.status != NavMeshPathStatus.PathComplete)
            return false;

        agent.SetPath(path);

        if (destinationMarker != null)
        {
            destinationMarker.position = navHit.position;
            SetMarkerVisible(true);
        }

        return true;
    }

    public void Stop()
    {
        if (agent.isOnNavMesh)
            agent.ResetPath();
        SetMarkerVisible(false);
    }

    void SetMarkerVisible(bool visible)
    {
        if (destinationMarker != null)
            destinationMarker.gameObject.SetActive(visible);
    }
}
