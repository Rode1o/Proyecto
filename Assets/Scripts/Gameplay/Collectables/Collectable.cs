using System;
using System.Collections;
using UnityEngine;
using FMODUnity;

/// <summary>
/// Objeto recogible del mundo. Reutiliza <see cref="Interactable"/> (sin mantener el clic) para
/// la detección del clic, el desplazamiento y el rango; al completarse, se recoge una sola vez,
/// lanza <see cref="Collected"/> y desaparece con un encogimiento breve.
/// </summary>
[RequireComponent(typeof(Interactable))]
public class Collectable : MonoBehaviour
{
    public enum State
    {
        Available,
        Collecting,
        Collected
    }

    [Tooltip("Identificador estable y único en la escena (p. ej. hoja_isla_a_01).")]
    [SerializeField] string collectableId;
    [SerializeField] CollectableDefinition definition;
    [Tooltip("Duración del encogimiento al recogerse (0 = desaparece al instante).")]
    [SerializeField, Min(0f)] float vanishDuration = 0.2f;
    [Header("Sonidos (FMOD)")]
    [SerializeField] EventReference sonidoConfirmacion;
    [SerializeField] EventReference sonidoColision;

    public event Action<Collectable> Collected;

    public string Id => collectableId;
    public CollectableDefinition Definition => definition;
    public State CurrentState { get; private set; } = State.Available;

    Interactable interactable;

    void Awake()
    {
        interactable = GetComponent<Interactable>();
        if (definition == null)
            Debug.LogWarning($"Collectable '{name}' no tiene CollectableDefinition.", this);
    }

    void OnEnable()
    {
        interactable.Completed += OnInteractionCompleted;
    }

    void OnDisable()
    {
        interactable.Completed -= OnInteractionCompleted;
    }

    void OnInteractionCompleted(Interactable _)
    {
        if (CurrentState != State.Available)
            return;

        CurrentState = State.Collecting;
         if (!sonidoConfirmacion.IsNull)
        RuntimeManager.PlayOneShot(sonidoConfirmacion, transform.position);
        if (!sonidoColision.IsNull)
        RuntimeManager.PlayOneShot(sonidoColision, transform.position);
        Collected?.Invoke(this);
        StartCoroutine(Vanish());
    }

    IEnumerator Vanish()
    {
        var startScale = transform.localScale;
        for (float t = 0f; t < vanishDuration; t += Time.deltaTime)
        {
            transform.localScale = startScale * (1f - t / vanishDuration);
            yield return null;
        }

        CurrentState = State.Collected;
        gameObject.SetActive(false);
    }
}
