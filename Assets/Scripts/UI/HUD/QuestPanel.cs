using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Panel "Encargos" del HUD: una fila por encargo de la isla actual, sacados del QuestLog de la escena.
/// El fondo tiene sitio para pocas filas; si hay más encargos, se ocultan primero los completados.
/// </summary>
public class QuestPanel : MonoBehaviour
{
    [Tooltip("Vacío = el QuestLog de la escena.")]
    [SerializeField] QuestLog questLog;
    [Tooltip("Fila de ejemplo (desactivada) que se copia por cada encargo.")]
    [SerializeField] QuestRow rowTemplate;
    [SerializeField] Transform rowContainer;
    [SerializeField, Min(1)] int maxRows = 3;
    [Tooltip("Se oculta si la isla no tiene encargos.")]
    [SerializeField] GameObject root;

    readonly List<QuestRow> rows = new();
    readonly List<int> visible = new();

    void Start()
    {
        rowTemplate.gameObject.SetActive(false);
        if (questLog == null)
            questLog = FindAnyObjectByType<QuestLog>();
        if (questLog != null)
            questLog.QuestChanged += OnQuestChanged;
        Rebuild(-1);
    }

    void OnDestroy()
    {
        if (questLog != null)
            questLog.QuestChanged -= OnQuestChanged;
    }

    void OnQuestChanged(int index) => Rebuild(index);

    void Rebuild(int changedIndex)
    {
        var count = questLog != null ? questLog.Count : 0;
        if (root != null)
            root.SetActive(count > 0);

        visible.Clear();
        for (var i = 0; i < count; i++)
            visible.Add(i);
        // Sobran filas: fuera los completados (los más antiguos primero) y luego los últimos.
        for (var i = 0; i < visible.Count && visible.Count > maxRows;)
        {
            if (questLog.IsCompleted(visible[i]))
                visible.RemoveAt(i);
            else
                i++;
        }
        if (visible.Count > maxRows)
            visible.RemoveRange(maxRows, visible.Count - maxRows);

        while (rows.Count < visible.Count)
            rows.Add(Instantiate(rowTemplate, rowContainer, false));

        for (var i = 0; i < rows.Count; i++)
        {
            var active = i < visible.Count;
            rows[i].gameObject.SetActive(active);
            if (!active)
                continue;

            var index = visible[i];
            rows[i].Show(questLog.Quests[index], questLog.GetProgress(index), questLog.IsCompleted(index),
                animate: index == changedIndex);
        }
    }
}
