using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

/// <summary>
/// Oveja "amable" que ofrece un atajo al jugador. Máquina de estados:
/// Waiting → Approaching → Talking → (acepta) LeadingHome → Finished (derrota)
///                                  → (rechaza) Angry → Retreating → Sulking (enfadada en su casa).
/// En Waiting y Sulking, si el jugador cambia de isla, pasa a Relocating: entra en su casa,
/// se teletransporta al interior de la casa de la nueva isla (<see cref="SheepHouse"/>) y sale por la puerta.
/// Hay un encuentro por isla (texto en cada SheepHouse): se pide con <see cref="RequestEncounter"/> o
/// <see cref="StartEncounter"/> y cada uno ocurre una sola vez. En una isla con el encuentro pendiente
/// la oveja espera amable (Waiting); en una ya rechazada, sigue enfadada (Sulking).
/// Bloquea las entradas desactivando los componentes de inputsToDisable (ClickToMove, PlayerInteractor);
/// mientras el diálogo está abierto, además, su bloqueador de UI frena los clics al mundo.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class SheepEnemy : MonoBehaviour
{
    public enum State
    {
        Waiting,
        Approaching,
        Talking,
        LeadingHome,
        Angry,
        Retreating,
        Sulking,
        Relocating,
        Finished
    }

    [Header("Referencias")]
    [SerializeField] GameSession session;
    [SerializeField] DialogBox dialog;
    [SerializeField] ClickToMove player;
    [Tooltip("Entradas del jugador que se desactivan durante el encuentro.")]
    [SerializeField] Behaviour[] inputsToDisable;
    [Tooltip("Opcional: muestra la respuesta de la oveja al rechazarla.")]
    [SerializeField] HudMessage hudMessage;

    [Header("Casas")]
    [Tooltip("Casa en la que empieza.")]
    [SerializeField] SheepHouse startHouse;
    [Tooltip("Una casa por isla; la oveja se muda a la de la isla del jugador.")]
    [SerializeField] SheepHouse[] houses;
    [Tooltip("Capas del suelo de las islas, para saber en qué isla está el jugador.")]
    [SerializeField] LayerMask groundMask = ~0;
    [SerializeField] float relocationCheckInterval = 0.5f;
    [Tooltip("Segundos para entrar o salir de la casa.")]
    [SerializeField] float doorwayDuration = 0.6f;

    [Header("Diálogo (los textos de cada isla están en su SheepHouse)")]
    [Tooltip("Índice de la opción que acepta la propuesta; el resto la rechazan.")]
    [SerializeField] int acceptOptionIndex;
    [SerializeField, TextArea] string defeatMessage = "Dijiste que sí... y la oveja te encerró en su casa.\nAprende a decir no.";

    [Header("Movimiento")]
    [SerializeField] float talkDistance = 2f;
    [Tooltip("Si no llega hasta el jugador en este tiempo, habla igualmente.")]
    [SerializeField] float approachTimeout = 5f;
    [Tooltip("Si el jugador no llega a la casa en este tiempo, se aplica la derrota igualmente.")]
    [SerializeField] float leadTimeout = 12f;
    [SerializeField] float angryDuration = 1f;

    [Header("Eventos")]
    [SerializeField] UnityEvent onAccepted;
    [Tooltip("Más adelante: empeorar el clima y cambiar el aspecto de la oveja.")]
    [SerializeField] UnityEvent onRejected;

    public event Action<SheepEnemy> Accepted;
    public event Action<SheepEnemy> Rejected;

    public State CurrentState { get; private set; } = State.Waiting;

    public SheepHouse CurrentHouse { get; private set; }

    NavMeshAgent agent;
    Minigame[] minigames = Array.Empty<Minigame>();
    readonly HashSet<SheepHouse> requestedEncounters = new();
    readonly HashSet<SheepHouse> resolvedEncounters = new();
    float stateTime;

    // Mudanza entre casas
    enum RelocationStep { WalkToDoor, Entering, Exiting }
    RelocationStep relocationStep;
    State stateAfterRelocation;
    SheepHouse targetHouse;
    float relocationCheckTimer;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        minigames = FindObjectsByType<Minigame>(FindObjectsInactive.Include);
        CurrentHouse = startHouse != null ? startHouse : (houses.Length > 0 ? houses[0] : null);
    }

    void Start()
    {
        if (CurrentHouse != null && CurrentHouse.Door != null)
        {
            agent.Warp(CurrentHouse.Door.position);
            FaceAwayFromHouse(CurrentHouse);
        }
    }

    /// <summary>Pide el encuentro de la isla donde está ahora la oveja (útil desde UnityEvents).</summary>
    public void StartEncounter() => RequestEncounter(CurrentHouse);

    /// <summary>
    /// Pide el encuentro de una isla. Se ignora si ya ocurrió; si la oveja aún no está en esa isla
    /// o hay un minijuego abierto, empieza en cuanto pueda.
    /// </summary>
    public void RequestEncounter(SheepHouse house)
    {
        if (house != null && !resolvedEncounters.Contains(house))
            requestedEncounters.Add(house);
    }

    public bool IsEncounterResolved(SheepHouse house) => resolvedEncounters.Contains(house);

    void Update()
    {
        stateTime += Time.deltaTime;

        switch (CurrentState)
        {
            case State.Waiting:
                if (requestedEncounters.Contains(CurrentHouse) && !AnyMinigameOpen() && (session == null || !session.IsDefeated))
                    BeginApproach();
                else
                    CheckRelocation();
                break;

            case State.Sulking:
                CheckRelocation();
                break;

            case State.Relocating:
                UpdateRelocation();
                break;

            case State.Approaching:
                if (Vector3.Distance(transform.position, player.transform.position) <= talkDistance || stateTime >= approachTimeout)
                    BeginTalking();
                else if (agent.isOnNavMesh)
                    agent.SetDestination(player.transform.position);
                break;

            case State.LeadingHome:
                if (player.HasArrived || stateTime >= leadTimeout)
                {
                    SetState(State.Finished);
                    if (session != null)
                        session.TriggerDefeat(defeatMessage);
                }
                break;

            case State.Angry:
                // Pequeño temblor de enfado antes de irse.
                transform.Rotate(0f, Mathf.Sin(stateTime * 40f) * 4f, 0f);
                if (stateTime >= angryDuration)
                    BeginRetreat();
                break;

            case State.Retreating:
                if (HasArrived() || stateTime >= leadTimeout)
                {
                    // Se queda enfadada delante de su casa, visible.
                    SetState(State.Sulking);
                    if (CurrentHouse != null)
                        FaceAwayFromHouse(CurrentHouse);
                }
                break;
        }
    }

    void BeginApproach()
    {
        SetState(State.Approaching);
        SetPlayerInput(false);
        player.Stop();
    }

    void BeginTalking()
    {
        SetState(State.Talking);
        if (agent.isOnNavMesh)
            agent.ResetPath();
        FaceTowards(player.transform.position);

        if (dialog == null)
        {
            Debug.LogError("SheepEnemy sin DialogBox: se trata como rechazo.", this);
            OnOptionChosen(-1);
            return;
        }
        dialog.Show(CurrentHouse.Offer, OnOptionChosen);
    }

    void OnOptionChosen(int index)
    {
        var accepted = index == acceptOptionIndex;
        requestedEncounters.Remove(CurrentHouse);
        resolvedEncounters.Add(CurrentHouse);
        if (session != null)
            session.RecordDecision(CurrentHouse.DecisionId, accepted);

        if (accepted)
        {
            SetState(State.LeadingHome);
            if (CurrentHouse != null && agent.isOnNavMesh)
                agent.SetDestination(CurrentHouse.Door.position);
            // Las entradas siguen bloqueadas: el jugador camina solo hacia la casa.
            if (CurrentHouse == null || !player.MoveTo(CurrentHouse.PlayerSpot.position))
                stateTime = leadTimeout;

            Accepted?.Invoke(this);
            onAccepted.Invoke();
        }
        else
        {
            SetState(State.Angry);
            SetPlayerInput(true);
            if (hudMessage != null)
                hudMessage.Show(CurrentHouse.RejectReply);

            Rejected?.Invoke(this);
            onRejected.Invoke();
        }
    }

    void BeginRetreat()
    {
        SetState(State.Retreating);
        if (CurrentHouse == null || !agent.isOnNavMesh || !agent.SetDestination(CurrentHouse.Door.position))
            SetState(State.Sulking);
    }

    void CheckRelocation()
    {
        relocationCheckTimer -= Time.deltaTime;
        if (relocationCheckTimer > 0f)
            return;
        relocationCheckTimer = relocationCheckInterval;

        var house = HouseUnderPlayer();
        if (house == null || house == CurrentHouse || CurrentHouse == null)
            return;

        targetHouse = house;
        // En una isla con el encuentro pendiente vuelve a esperar amable; si ya se rechazó, sigue enfadada.
        stateAfterRelocation = resolvedEncounters.Contains(house) ? State.Sulking : State.Waiting;
        SetState(State.Relocating);
        relocationStep = RelocationStep.WalkToDoor;
        if (!agent.isOnNavMesh || !agent.SetDestination(CurrentHouse.Door.position))
            stateTime = leadTimeout;
    }

    void UpdateRelocation()
    {
        switch (relocationStep)
        {
            case RelocationStep.WalkToDoor:
                if (HasArrived() || stateTime >= leadTimeout)
                {
                    // Desde la puerta entra caminando hasta el interior, donde las paredes la tapan.
                    agent.enabled = false;
                    relocationStep = RelocationStep.Entering;
                    stateTime = 0f;
                }
                break;

            case RelocationStep.Entering:
                if (MoveThroughDoorway(CurrentHouse.Door.position, CurrentHouse.Interior.position))
                {
                    // Teletransporte oculto: de un interior a otro.
                    CurrentHouse = targetHouse;
                    transform.position = CurrentHouse.Interior.position;
                    relocationStep = RelocationStep.Exiting;
                    stateTime = 0f;
                }
                break;

            case RelocationStep.Exiting:
                if (MoveThroughDoorway(CurrentHouse.Interior.position, CurrentHouse.Door.position))
                {
                    agent.enabled = true;
                    agent.Warp(CurrentHouse.Door.position);
                    FaceAwayFromHouse(CurrentHouse);
                    SetState(stateAfterRelocation);
                }
                break;
        }
    }

    /// <summary>Desplaza la oveja de from a to en doorwayDuration. Devuelve true al terminar.</summary>
    bool MoveThroughDoorway(Vector3 from, Vector3 to)
    {
        var t = doorwayDuration > 0f ? Mathf.Clamp01(stateTime / doorwayDuration) : 1f;
        transform.position = Vector3.Lerp(from, to, t);
        FaceTowards(to + (to - from));
        return t >= 1f;
    }

    SheepHouse HouseUnderPlayer()
    {
        if (!Physics.Raycast(player.transform.position + Vector3.up * 0.5f, Vector3.down, out var hit, 10f, groundMask, QueryTriggerInteraction.Ignore))
            return null;

        foreach (var house in houses)
            if (house != null && house.Island == hit.collider)
                return house;
        return null;
    }

    bool HasArrived() => agent.isOnNavMesh && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f;

    void FaceAwayFromHouse(SheepHouse house)
    {
        if (house.Interior != null)
            FaceTowards(transform.position + (house.Door.position - house.Interior.position));
    }

    void SetState(State state)
    {
        CurrentState = state;
        stateTime = 0f;
    }

    void SetPlayerInput(bool enabled)
    {
        foreach (var input in inputsToDisable)
            if (input != null)
                input.enabled = enabled;
    }

    bool AnyMinigameOpen()
    {
        foreach (var minigame in minigames)
            if (minigame != null && minigame.IsOpen)
                return true;
        return false;
    }

    void FaceTowards(Vector3 point)
    {
        var direction = point - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(direction);
    }
}
