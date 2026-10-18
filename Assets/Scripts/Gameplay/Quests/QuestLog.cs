using System;
using UnityEngine;

/// <summary>
/// Progreso de los encargos de la isla actual. Va en la escena de cada isla con su asset de
/// <see cref="IslandQuests"/>. Avanza solo con los collectables (PlayerCollection) y los mecanismos
/// (PuzzleMechanism) indicados en cada encargo; el resto se avanza con <see cref="AddProgress"/>.
/// </summary>
public class QuestLog : MonoBehaviour
{
    [SerializeField] IslandQuests quests;
    [Tooltip("Vacío = el PlayerCollection de la escena.")]
    [SerializeField] PlayerCollection collection;

    int[] progress = Array.Empty<int>();
    PuzzleMechanism[] mechanisms = Array.Empty<PuzzleMechanism>();

    /// <summary>Se lanza cuando cambia el progreso de un encargo (índice en <see cref="Quests"/>).</summary>
    public event Action<int> QuestChanged;
    /// <summary>Se lanza una vez cuando un encargo llega a su meta.</summary>
    public event Action<QuestDefinition> QuestCompleted;

    public IslandQuests Data => quests;
    public QuestDefinition[] Quests => quests != null ? quests.Quests : Array.Empty<QuestDefinition>();
    public int Count => Quests.Length;

    public int GetProgress(int index) => index >= 0 && index < progress.Length ? progress[index] : 0;
    public bool IsCompleted(int index) => index >= 0 && index < Count && GetProgress(index) >= Quests[index].Goal;
    public bool AllCompleted
    {
        get
        {
            for (var i = 0; i < Count; i++)
                if (!IsCompleted(i))
                    return false;
            return true;
        }
    }

    void Awake()
    {
        progress = new int[Count];

        if (collection == null)
            collection = FindAnyObjectByType<PlayerCollection>();
        if (collection != null)
            collection.ItemCollected += OnItemCollected;

        mechanisms = FindObjectsByType<PuzzleMechanism>(FindObjectsInactive.Include);
        foreach (var mechanism in mechanisms)
            mechanism.Solved += OnMechanismSolved;
    }

    void OnDestroy()
    {
        if (collection != null)
            collection.ItemCollected -= OnItemCollected;
        foreach (var mechanism in mechanisms)
            if (mechanism != null)
                mechanism.Solved -= OnMechanismSolved;
    }

    /// <summary>Suma progreso al encargo con ese id (p. ej. "reparar_cerca").</summary>
    public void AddProgress(string questId, int amount = 1)
    {
        var index = IndexOf(questId);
        if (index < 0)
            Debug.LogWarning($"QuestLog: no hay ningún encargo con id '{questId}'.", this);
        else
            SetProgress(index, GetProgress(index) + amount);
    }

    public void Complete(string questId)
    {
        var index = IndexOf(questId);
        if (index >= 0)
            SetProgress(index, Quests[index].Goal);
    }

    public int IndexOf(string questId)
    {
        for (var i = 0; i < Count; i++)
            if (Quests[i] != null && Quests[i].Id == questId)
                return i;
        return -1;
    }

    void SetProgress(int index, int value)
    {
        var goal = Quests[index].Goal;
        value = Mathf.Clamp(value, 0, goal);
        if (value == progress[index])
            return;

        var wasCompleted = IsCompleted(index);
        progress[index] = value;
        QuestChanged?.Invoke(index);
        if (!wasCompleted && IsCompleted(index))
            QuestCompleted?.Invoke(Quests[index]);
    }

    void OnItemCollected(Collectable collectable)
    {
        var definition = collectable.Definition;
        if (definition == null)
            return;

        for (var i = 0; i < Count; i++)
            if (Quests[i].Collectable == definition)
                SetProgress(i, collection.Count(definition));
    }

    void OnMechanismSolved(PuzzleMechanism mechanism)
    {
        for (var i = 0; i < Count; i++)
            if (!string.IsNullOrEmpty(Quests[i].MechanismId) && Quests[i].MechanismId == mechanism.Id)
                SetProgress(i, Quests[i].Goal);
    }
}
