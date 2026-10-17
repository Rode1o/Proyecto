using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Texto de un diálogo con opciones, editable desde el Inspector.</summary>
[Serializable]
public class DialogContent
{
    public string speaker = "Oveja";
    [TextArea(2, 5)] public string text;
    [Tooltip("Una etiqueta por botón, en orden.")]
    public string[] options = { "Sí", "No" };
}

/// <summary>
/// Cuadro de diálogo modal reutilizable. Muestra un <see cref="DialogContent"/> y devuelve el índice
/// de la opción elegida. El root incluye un bloqueador a pantalla completa con Raycast Target, así
/// ClickToMove, PlayerInteractor y OrbitCamera ignoran los clics mientras está abierto.
/// </summary>
public class DialogBox : MonoBehaviour
{
    [SerializeField] GameObject root;
    [SerializeField] TMP_Text speakerText;
    [SerializeField] TMP_Text bodyText;
    [Tooltip("Botones de opción en orden; se ocultan los que no tengan texto.")]
    [SerializeField] Button[] optionButtons;
    [SerializeField] TMP_Text[] optionLabels;

    Action<int> onChosen;

    public bool IsOpen => root.activeSelf;

    void Awake()
    {
        root.SetActive(false);
        for (int i = 0; i < optionButtons.Length; i++)
        {
            int index = i;
            optionButtons[i].onClick.AddListener(() => Choose(index));
        }
    }

    public void Show(DialogContent content, Action<int> chosen)
    {
        onChosen = chosen;
        speakerText.text = content.speaker;
        bodyText.text = content.text;

        for (int i = 0; i < optionButtons.Length; i++)
        {
            var hasOption = content.options != null && i < content.options.Length && !string.IsNullOrEmpty(content.options[i]);
            optionButtons[i].gameObject.SetActive(hasOption);
            if (hasOption)
                optionLabels[i].text = content.options[i];
        }

        root.SetActive(true);
    }

    public void Choose(int index)
    {
        if (!IsOpen)
            return;

        root.SetActive(false);
        var callback = onChosen;
        onChosen = null;
        callback?.Invoke(index);
    }
}
