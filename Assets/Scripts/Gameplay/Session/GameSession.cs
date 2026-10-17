using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Estado de la partida en la escena: decisiones del jugador y derrota.
/// Va en un GameObject de la escena y se referencia desde el Inspector (sin singleton).
/// </summary>
public class GameSession : MonoBehaviour
{
    readonly Dictionary<string, bool> decisions = new();

    public bool IsDefeated { get; private set; }

    /// <summary>id de la decisión, true si aceptó.</summary>
    public event Action<string, bool> DecisionRecorded;
    /// <summary>Mensaje de derrota.</summary>
    public event Action<string> Defeated;

    public void RecordDecision(string decisionId, bool accepted)
    {
        decisions[decisionId] = accepted;
        Debug.Log($"Decisión '{decisionId}': {(accepted ? "aceptó" : "rechazó")}", this);
        DecisionRecorded?.Invoke(decisionId, accepted);
    }

    /// <summary>null si aún no se ha tomado.</summary>
    public bool? GetDecision(string decisionId) =>
        decisions.TryGetValue(decisionId, out var accepted) ? accepted : null;

    public void TriggerDefeat(string message)
    {
        if (IsDefeated)
            return;

        IsDefeated = true;
        Debug.Log($"Derrota: {message}", this);
        Defeated?.Invoke(message);
    }
}
