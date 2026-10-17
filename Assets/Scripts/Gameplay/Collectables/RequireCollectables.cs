using UnityEngine;

/// <summary>
/// Bloquea un Interactable (p. ej. el mecanismo de una isla) hasta que se hayan recogido
/// todos los collectables indicados: los objetivos de la isla.
/// </summary>
[RequireComponent(typeof(Interactable))]
public class RequireCollectables : MonoBehaviour, IInteractionRequirement
{
    [Tooltip("Collectables que hay que recoger antes de usar este objeto.")]
    [SerializeField] Collectable[] required;
    [Tooltip("{0} = recogidos, {1} = total.")]
    [SerializeField] string lockedMessage = "Recoge todos los objetos de la isla primero ({0}/{1}).";

    public int Total => required.Length;

    public int CollectedCount
    {
        get
        {
            int count = 0;
            foreach (var collectable in required)
                if (collectable == null || collectable.CurrentState != Collectable.State.Available)
                    count++;
            return count;
        }
    }

    public bool IsMet(out string reason)
    {
        var collected = CollectedCount;
        if (collected >= Total)
        {
            reason = null;
            return true;
        }

        reason = string.Format(lockedMessage, collected, Total);
        return false;
    }
}
