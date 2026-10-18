using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FMODUnity;

public enum DotNode
{
    A,
    B,
    C
}

/// <summary>
/// Minijuego "conectar los puntos": el jugador pulsa los nodos en orden y se dibuja una línea
/// entre cada par. Al completar tantas selecciones como pasos tiene la secuencia, se comprueba:
/// incorrecta → líneas en rojo y reinicio automático; correcta → confirmación y cierre como resuelto.
/// La secuencia y la pista las aporta cada <see cref="ConnectDotsMechanism"/> al abrirlo.
/// </summary>
public class ConnectDotsMinigame : Minigame
{
    [Header("Tablero")]
    [Tooltip("Contenedor de las líneas; debe ir antes que los nodos en la jerarquía para quedar debajo.")]
    [SerializeField] RectTransform linesContainer;
    [Tooltip("Botones de los nodos en orden A, B, C.")]
    [SerializeField] Button[] nodes = new Button[3];

    [Header("Textos y botones")]
    [SerializeField] TMP_Text hintText;
    [SerializeField] TMP_Text resultText;
    [SerializeField] Button restartButton;
    [SerializeField] Button closeButton;

    [Header("Aspecto")]
    [SerializeField] float lineThickness = 8f;
    [SerializeField] Color idleColor = new(0.95f, 0.92f, 0.85f);
    [SerializeField] Color selectedColor = new(1f, 0.8f, 0.3f);
    [Tooltip("Color de las líneas mientras se traza la secuencia.")]
    [SerializeField] Color lineColor = new Color32(0x92, 0x70, 0xC7, 0xFF);
    [SerializeField] Color errorColor = new(0.9f, 0.3f, 0.25f);
    [SerializeField] Color successColor = new(0.45f, 0.85f, 0.4f);

    [Header("Tiempos")]
    [SerializeField] float errorResetDelay = 1f;
    [SerializeField] float successCloseDelay = 1.2f;

    [Header("Sonidos (FMOD)")]
    [SerializeField] EventReference sonidoError;

    readonly List<int> selection = new();
    readonly List<Image> lines = new();
    int[] expected = { 0, 1, 2 };
    bool locked;
    bool solved;

    protected override void Awake()
    {
        base.Awake();

        for (int i = 0; i < nodes.Length; i++)
        {
            int index = i;
            nodes[i].onClick.AddListener(() => SelectNode(index));
        }
        restartButton.onClick.AddListener(Restart);
        closeButton.onClick.AddListener(Close);
    }

    public void Open(IReadOnlyList<DotNode> sequence, string hint)
    {
        expected = new int[sequence.Count];
        for (int i = 0; i < sequence.Count; i++)
            expected[i] = (int)sequence[i];

        hintText.text = hint;
        Show();
        Restart();
    }

    /// <summary>Borra la selección y las líneas. No disponible tras resolver.</summary>
    public void Restart()
    {
        if (solved)
            return;

        StopAllCoroutines();
        ClearBoard();
        locked = false;
        resultText.text = string.Empty;
    }

    // Tras resolver, cerrar no anula el resultado.
    public override void Close() => Finish(solved);

    protected override void OnClosed()
    {
        StopAllCoroutines();
        ClearBoard();
        locked = false;
        solved = false;
        resultText.text = string.Empty;
    }

    public void SelectNode(int index)
    {
        if (!IsOpen || locked)
            return;

        if (selection.Count > 0 && selection[^1] == index)
        {
            resultText.text = "No puedes elegir el mismo punto dos veces seguidas.";
            return;
        }

        if (selection.Count > 0)
            lines.Add(DrawLine(selection[^1], index));

        selection.Add(index);
        SetNodeColor(index, selectedColor);
        resultText.text = string.Empty;

        if (selection.Count < expected.Length)
            return;

        StartCoroutine(IsCorrect() ? Succeed() : Fail());
    }

    bool IsCorrect()
    {
        for (int i = 0; i < expected.Length; i++)
            if (selection[i] != expected[i])
                return false;
        return true;
    }

    IEnumerator Fail()
    {
        locked = true;

        if (!sonidoError.IsNull)
        RuntimeManager.PlayOneShot(sonidoError);
        
        Paint(errorColor);
        resultText.text = "Secuencia incorrecta. Inténtalo de nuevo.";
        yield return new WaitForSecondsRealtime(errorResetDelay);
        Restart();
    }

    IEnumerator Succeed()
    {
        locked = true;
        solved = true;
        restartButton.interactable = false;
        Paint(successColor);
        resultText.text = "¡Correcto! El mecanismo se ha activado.";
        yield return new WaitForSecondsRealtime(successCloseDelay);
        Finish(true);
    }

    Image DrawLine(int from, int to)
    {
        Vector2 a = linesContainer.InverseTransformPoint(nodes[from].transform.position);
        Vector2 b = linesContainer.InverseTransformPoint(nodes[to].transform.position);
        var delta = b - a;

        var line = new GameObject("Line", typeof(RectTransform), typeof(Image));
        var rect = (RectTransform)line.transform;
        rect.SetParent(linesContainer, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.localPosition = a;
        rect.sizeDelta = new Vector2(delta.magnitude, lineThickness);
        rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

        var image = line.GetComponent<Image>();
        image.color = lineColor;
        image.raycastTarget = false;
        return image;
    }

    void Paint(Color color)
    {
        foreach (var line in lines)
            line.color = color;
        foreach (var index in selection)
            SetNodeColor(index, color);
    }

    void SetNodeColor(int index, Color color) => nodes[index].targetGraphic.color = color;

    void ClearBoard()
    {
        foreach (var line in lines)
            Destroy(line.gameObject);
        lines.Clear();
        selection.Clear();

        for (int i = 0; i < nodes.Length; i++)
            SetNodeColor(i, idleColor);
        restartButton.interactable = true;
    }
}
