using System;
using FMODUnity;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Une un <see cref="Interactable"/> con un minijuego: al completar la interacción abre el minijuego;
/// si se resuelve, notifica <see cref="Solved"/> y <c>onSolved</c>; si se cierra sin resolver,
/// el Interactable vuelve a quedar disponible.
/// Cada tipo de minijuego tiene su subclase con la configuración propia del mecanismo.
/// </summary>
[RequireComponent(typeof(Interactable))]
public abstract class PuzzleMechanism : MonoBehaviour
{
    [Tooltip("Identificador único del mecanismo en la escena (lo valida ProgressionManager).")]
    [SerializeField] string mechanismId;
    [Tooltip("Se invoca una vez al resolver el puzle.")]
    [SerializeField] UnityEvent onSolved;
    [Tooltip("Opcional: suena al resolver el puzle.")]
    [SerializeField] EventReference solvedSound;

    public event Action<PuzzleMechanism> Solved;

    public string Id => mechanismId;
    public bool IsSolved { get; private set; }

    protected abstract Minigame Minigame { get; }

    /// <summary>Abre el minijuego con la configuración de este mecanismo.</summary>
    protected abstract void OpenMinigame();

    Interactable interactable;

    void Awake()
    {
        interactable = GetComponent<Interactable>();
    }

    void OnEnable()
    {
        interactable.Completed += OnInteractionCompleted;
    }

    void OnDisable()
    {
        interactable.Completed -= OnInteractionCompleted;
        if (Minigame != null)
            Minigame.Finished -= OnMinigameFinished;
    }

    void OnInteractionCompleted(Interactable _)
    {
        // Solo ocurre si el Interactable es repetible: el puzle ya resuelto no se vuelve a abrir.
        if (IsSolved)
        {
            Debug.Log($"'{Id}' ya está completado; no se vuelve a abrir el puzle.", this);
            return;
        }

        if (Minigame == null || Minigame.IsOpen)
        {
            if (Minigame == null)
                Debug.LogError($"'{name}' no tiene minijuego asignado.", this);
            interactable.ResetInteraction();
            return;
        }

        Minigame.Finished += OnMinigameFinished;
        OpenMinigame();
    }

    void OnMinigameFinished(bool solved)
    {
        Minigame.Finished -= OnMinigameFinished;

        if (!solved)
        {
            interactable.ResetInteraction();
            return;
        }

        IsSolved = true;
        Debug.Log($"Puzle resuelto: {name}", this);
        if (!solvedSound.IsNull)
            RuntimeManager.PlayOneShot(solvedSound);
        Solved?.Invoke(this);
        onSolved.Invoke();
    }
}
