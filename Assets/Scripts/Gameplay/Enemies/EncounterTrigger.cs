using UnityEngine;

/// <summary>
/// Condición de inicio del encuentro de una isla: cuando se han recogido todos los collectables
/// indicados, pide a la oveja el encuentro de esa isla (<see cref="SheepEnemy.RequestEncounter"/>).
/// Usa un EncounterTrigger por isla. Para otras condiciones, se puede llamar a StartEncounter
/// desde cualquier UnityEvent (onCompleted de un Interactable, onSolved de un PuzzleMechanism...).
/// </summary>
public class EncounterTrigger : MonoBehaviour
{
    [SerializeField] SheepEnemy encounter;
    [Tooltip("Casa de la isla cuyo encuentro se inicia.")]
    [SerializeField] SheepHouse island;
    [Tooltip("El encuentro empieza cuando todos estos objetos se han recogido.")]
    [SerializeField] Collectable[] afterCollecting;

    void OnEnable()
    {
        foreach (var collectable in afterCollecting)
            if (collectable != null)
                collectable.Collected += OnCollected;
    }

    void OnDisable()
    {
        foreach (var collectable in afterCollecting)
            if (collectable != null)
                collectable.Collected -= OnCollected;
    }

    void OnCollected(Collectable _)
    {
        foreach (var collectable in afterCollecting)
            if (collectable != null && collectable.CurrentState == Collectable.State.Available)
                return;

        if (encounter == null)
            return;

        if (island != null)
            encounter.RequestEncounter(island);
        else
            encounter.StartEncounter();
    }
}
