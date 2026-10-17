using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Registro de lo que ha recogido el jugador: ids recogidos y cantidad por tipo.
/// Recoge automáticamente todos los <see cref="Collectable"/> de la escena. No es un inventario:
/// solo guarda datos para que otros sistemas (herramientas, progresión) los consulten.
/// </summary>
public class PlayerCollection : MonoBehaviour
{
    readonly HashSet<string> collectedIds = new();
    readonly Dictionary<CollectableDefinition, int> counts = new();
    Collectable[] collectables = Array.Empty<Collectable>();

    /// <summary>Se lanza tras registrar un objeto recogido.</summary>
    public event Action<Collectable> ItemCollected;

    public bool HasCollected(string collectableId) => collectedIds.Contains(collectableId);
    public int Count(CollectableDefinition definition) =>
        definition != null && counts.TryGetValue(definition, out var count) ? count : 0;
    public bool Has(CollectableDefinition definition) => Count(definition) > 0;

    void Awake()
    {
        collectables = FindObjectsByType<Collectable>(FindObjectsInactive.Include);

        var ids = new HashSet<string>();
        foreach (var collectable in collectables)
        {
            if (string.IsNullOrWhiteSpace(collectable.Id))
                Debug.LogError($"'{collectable.name}' no tiene collectableId.", collectable);
            else if (!ids.Add(collectable.Id))
                Debug.LogError($"collectableId duplicado: '{collectable.Id}'.", collectable);

            collectable.Collected += OnCollected;
        }
    }

    void OnDestroy()
    {
        foreach (var collectable in collectables)
            if (collectable != null)
                collectable.Collected -= OnCollected;
    }

    void OnCollected(Collectable collectable)
    {
        var id = string.IsNullOrWhiteSpace(collectable.Id) ? collectable.name : collectable.Id;
        if (!collectedIds.Add(id))
            return;

        var definition = collectable.Definition;
        if (definition != null)
            counts[definition] = Count(definition) + 1;

        var label = definition != null ? definition.DisplayName : collectable.name;
        Debug.Log($"Recogido: {label} ({id}). Total de este tipo: {Count(definition)}", collectable);
        ItemCollected?.Invoke(collectable);
    }
}
