using UnityEngine;

/// <summary>
/// Mecanismo que abre el minijuego de conectar puntos con su propia secuencia y pista.
/// Varios mecanismos pueden compartir el mismo <see cref="ConnectDotsMinigame"/>.
/// </summary>
public class ConnectDotsMechanism : PuzzleMechanism
{
    [SerializeField] ConnectDotsMinigame minigame;
    [Tooltip("Orden correcto. Mínimo dos pasos y sin el mismo nodo dos veces seguidas.")]
    [SerializeField] DotNode[] sequence = { DotNode.A, DotNode.B, DotNode.C };
    [SerializeField, TextArea] string hint = "Conecta los puntos en el orden correcto.";

    protected override Minigame Minigame => minigame;

    protected override void OpenMinigame() => minigame.Open(sequence, hint);

    void OnValidate()
    {
        if (sequence == null || sequence.Length < 2)
        {
            Debug.LogWarning($"'{name}': la secuencia necesita al menos dos nodos.", this);
            return;
        }

        for (int i = 1; i < sequence.Length; i++)
        {
            if (sequence[i] == sequence[i - 1])
            {
                Debug.LogWarning($"'{name}': la secuencia repite {sequence[i]} seguido en la posición {i}; no se podrá resolver.", this);
                return;
            }
        }
    }
}
