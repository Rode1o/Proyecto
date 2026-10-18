using System;
using UnityEngine;

/// <summary>
/// Encargos de una isla. Se crea un asset por isla y se asigna al QuestLog de su escena; el HUD
/// y el diario solo leen de aquí, así que sirven en cualquier nivel.
/// Crear: Assets → Create → Gameplay → Island Quests.
/// </summary>
[CreateAssetMenu(menuName = "Gameplay/Island Quests", fileName = "IslandQuests")]
public class IslandQuests : ScriptableObject
{
    [SerializeField] string islandName = "Isla";
    [SerializeField] QuestDefinition[] quests = Array.Empty<QuestDefinition>();

    public string IslandName => islandName;
    public QuestDefinition[] Quests => quests;

#if UNITY_EDITOR
    void OnValidate()
    {
        var ids = new System.Collections.Generic.HashSet<string>();
        foreach (var quest in quests)
            if (quest != null && !string.IsNullOrWhiteSpace(quest.Id) && !ids.Add(quest.Id))
                Debug.LogWarning($"{name}: id de encargo duplicado '{quest.Id}'.", this);
    }
#endif
}

/// <summary>
/// Un encargo. El progreso puede avanzar solo (contando un collectable o al resolver un mecanismo)
/// o desde código con <see cref="QuestLog.AddProgress"/>.
/// </summary>
[Serializable]
public class QuestDefinition
{
    [Tooltip("Identificador único dentro de la isla, para avanzar el encargo desde código.")]
    [SerializeField] string id = "encargo";
    [SerializeField] string title = "Encargo";
    [Tooltip("Texto largo que se ve en el diario.")]
    [SerializeField, TextArea(2, 4)] string description;
    [SerializeField] Sprite icon;
    [Tooltip("Cantidad necesaria para completarlo. Con 1 no se muestra el contador.")]
    [SerializeField, Min(1)] int goal = 1;

    [Header("Progreso automático (opcional)")]
    [Tooltip("Cuenta los collectables recogidos de este tipo.")]
    [SerializeField] CollectableDefinition collectable;
    [Tooltip("Se completa cuando se resuelve el PuzzleMechanism con este mechanismId.")]
    [SerializeField] string mechanismId;

    public string Id => id;
    public string Title => title;
    public string Description => description;
    public Sprite Icon => icon;
    public int Goal => Mathf.Max(1, goal);
    public CollectableDefinition Collectable => collectable;
    public string MechanismId => mechanismId;
}
