using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Ruta (puente) entre islas que empieza bloqueada y solo la desbloquea ProgressionManager
/// cuando se resuelve <see cref="UnlockedBy"/>.
/// El bloqueo físico lo hacen los objetos de activeWhileLocked (una barrera con NavMeshObstacle
/// en modo Carve): mientras existen, el NavMesh queda cortado y ClickToMove no encuentra ruta.
/// </summary>
public class IslandRoute : MonoBehaviour
{
    [Tooltip("Mecanismo cuyo puzle desbloquea esta ruta.")]
    [SerializeField] PuzzleMechanism unlockedBy;

    [Header("Bloqueo")]
    [Tooltip("Barreras visibles y con NavMeshObstacle (Carve) mientras la ruta está bloqueada.")]
    [SerializeField] GameObject[] activeWhileLocked;

    [Header("Indicador")]
    [Tooltip("Renderers que cambian de material según el estado (tablero del puente, farol...).")]
    [SerializeField] Renderer[] indicatorRenderers;
    [SerializeField] Material lockedMaterial;
    [SerializeField] Material unlockedMaterial;

    [SerializeField] UnityEvent onUnlocked;

    public PuzzleMechanism UnlockedBy => unlockedBy;
    public bool IsUnlocked { get; private set; }

    void Awake()
    {
        Apply();
    }

    public void Unlock()
    {
        if (IsUnlocked)
            return;

        IsUnlocked = true;
        Apply();
        Debug.Log($"Ruta desbloqueada: {name}", this);
        onUnlocked.Invoke();
    }

    void Apply()
    {
        foreach (var go in activeWhileLocked)
            if (go != null)
                go.SetActive(!IsUnlocked);

        var material = IsUnlocked ? unlockedMaterial : lockedMaterial;
        if (material == null)
            return;
        foreach (var r in indicatorRenderers)
            if (r != null)
                r.sharedMaterial = material;
    }
}
