using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Diario del jugador: el botón del HUD lo abre y muestra todos los encargos de la isla con su
/// descripción y estado. Se cierra con el botón, la X, Escape o haciendo clic fuera.
/// </summary>
public class DiaryPanel : MonoBehaviour
{
    [Tooltip("Vacío = el QuestLog de la escena.")]
    [SerializeField] QuestLog questLog;
    [SerializeField] Button openButton;
    [Tooltip("Raíz del diario abierto (fondo oscuro + pergamino).")]
    [SerializeField] GameObject panel;
    [SerializeField] Button closeButton;
    [Tooltip("Fondo oscuro: un clic fuera del pergamino cierra el diario.")]
    [SerializeField] Button backdrop;
    [SerializeField] TMP_Text islandLabel;
    [Tooltip("Fila de ejemplo (desactivada) que se copia por cada encargo.")]
    [SerializeField] QuestRow rowTemplate;
    [SerializeField] Transform rowContainer;

    readonly List<QuestRow> rows = new();

    public bool IsOpen => panel.activeSelf;

    void Awake()
    {
        rowTemplate.gameObject.SetActive(false);
        panel.SetActive(false);
        openButton.onClick.AddListener(Toggle);
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
        if (backdrop != null)
            backdrop.onClick.AddListener(Close);
    }

    void Start()
    {
        if (questLog == null)
            questLog = FindAnyObjectByType<QuestLog>();
        if (questLog != null)
            questLog.QuestChanged += OnQuestChanged;
    }

    void OnDestroy()
    {
        if (questLog != null)
            questLog.QuestChanged -= OnQuestChanged;
    }

    void Update()
    {
        if (IsOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            Close();
    }

    public void Toggle()
    {
        if (IsOpen)
            Close();
        else
            Open();
    }

    public void Open()
    {
        panel.SetActive(true);
        Refresh(-1);
    }

    public void Close() => panel.SetActive(false);

    void OnQuestChanged(int index)
    {
        if (IsOpen)
            Refresh(index);
    }

    void Refresh(int changedIndex)
    {
        var count = questLog != null ? questLog.Count : 0;
        if (islandLabel != null)
            islandLabel.text = questLog != null && questLog.Data != null ? questLog.Data.IslandName : "";

        while (rows.Count < count)
            rows.Add(Instantiate(rowTemplate, rowContainer, false));

        for (var i = 0; i < rows.Count; i++)
        {
            var active = i < count;
            rows[i].gameObject.SetActive(active);
            if (active)
                rows[i].Show(questLog.Quests[i], questLog.GetProgress(i), questLog.IsCompleted(i),
                    animate: i == changedIndex);
        }
    }
}
