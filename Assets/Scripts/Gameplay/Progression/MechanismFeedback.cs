using UnityEngine;

/// <summary>
/// Respuesta visual de un mecanismo al resolverse: cambia materiales y activa objetos
/// (una luz, partículas...). Va en el mismo GameObject que el PuzzleMechanism.
/// </summary>
public class MechanismFeedback : MonoBehaviour
{
    [SerializeField] Renderer[] renderers;
    [SerializeField] Material solvedMaterial;
    [Tooltip("Objetos ocultos hasta resolver el puzle.")]
    [SerializeField] GameObject[] activateOnSolved;

    PuzzleMechanism mechanism;

    void Awake()
    {
        mechanism = GetComponent<PuzzleMechanism>();
        if (mechanism == null)
            Debug.LogError($"'{name}' necesita un PuzzleMechanism en el mismo GameObject.", this);

        foreach (var go in activateOnSolved)
            if (go != null)
                go.SetActive(false);
    }

    void OnEnable()
    {
        if (mechanism != null)
            mechanism.Solved += OnSolved;
    }

    void OnDisable()
    {
        if (mechanism != null)
            mechanism.Solved -= OnSolved;
    }

    void OnSolved(PuzzleMechanism _)
    {
        if (solvedMaterial != null)
            foreach (var r in renderers)
                if (r != null)
                    r.sharedMaterial = solvedMaterial;

        foreach (var go in activateOnSolved)
            if (go != null)
                go.SetActive(true);
    }
}
