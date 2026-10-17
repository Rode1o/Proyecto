using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Progresión de la escena: registra qué mecanismos se han resuelto y desbloquea solo las rutas
/// asociadas a cada uno. Recoge automáticamente todos los PuzzleMechanism e IslandRoute de la escena.
/// Flujo: Minigame (bool) → PuzzleMechanism.Solved → ProgressionManager → IslandRoute.Unlock().
/// </summary>
public class ProgressionManager : MonoBehaviour
{
    readonly HashSet<string> solvedIds = new();
    PuzzleMechanism[] mechanisms = Array.Empty<PuzzleMechanism>();
    IslandRoute[] routes = Array.Empty<IslandRoute>();

    public event Action<IslandRoute> RouteUnlocked;

    public bool IsSolved(string mechanismId) => solvedIds.Contains(mechanismId);

    void Awake()
    {
        mechanisms = FindObjectsByType<PuzzleMechanism>(FindObjectsInactive.Include);
        routes = FindObjectsByType<IslandRoute>(FindObjectsInactive.Include);

        Validate();
        foreach (var mechanism in mechanisms)
            mechanism.Solved += OnMechanismSolved;
    }

    void OnDestroy()
    {
        foreach (var mechanism in mechanisms)
            if (mechanism != null)
                mechanism.Solved -= OnMechanismSolved;
    }

    void OnMechanismSolved(PuzzleMechanism mechanism)
    {
        if (!solvedIds.Add(mechanism.Id))
        {
            Debug.Log($"'{mechanism.Id}' ya estaba registrado como completado; se ignora.", mechanism);
            return;
        }

        Debug.Log($"Progresión: '{mechanism.Id}' completado.", mechanism);

        foreach (var route in routes)
        {
            if (route.UnlockedBy != mechanism || route.IsUnlocked)
                continue;

            route.Unlock();
            RouteUnlocked?.Invoke(route);
        }
    }

    void Validate()
    {
        var ids = new HashSet<string>();
        foreach (var mechanism in mechanisms)
        {
            if (string.IsNullOrWhiteSpace(mechanism.Id))
                Debug.LogError($"'{mechanism.name}' no tiene mechanismId.", mechanism);
            else if (!ids.Add(mechanism.Id))
                Debug.LogError($"mechanismId duplicado: '{mechanism.Id}'.", mechanism);
        }

        foreach (var route in routes)
            if (route.UnlockedBy == null)
                Debug.LogWarning($"La ruta '{route.name}' no tiene mecanismo asignado; nunca se desbloqueará.", route);
    }
}
