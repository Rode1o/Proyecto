using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using FMODUnity;

/// <summary>
/// Minijuego de memoria: seis cartas boca abajo con tres parejas. Se revelan de dos en dos;
/// si coinciden quedan descubiertas, si no se muestran <c>mismatchDelay</c> segundos y se ocultan.
/// Al encontrar todas las parejas muestra la confirmación y, al cerrar, informa como resuelto.
/// Los símbolos, el reverso y el orden de las cartas los aporta cada <see cref="MemoryPairsMechanism"/>.
/// </summary>
public class MemoryPairsMinigame : Minigame
{
    [Header("Cartas")]
    [Tooltip("Las seis cartas, de izquierda a derecha y de arriba abajo.")]
    [SerializeField] MemoryCardView[] cards = new MemoryCardView[6];

    [Header("Textos y botones")]
    [SerializeField] TMP_Text resultText;
    [SerializeField] UnityEngine.UI.Button restartButton;
    [SerializeField] UnityEngine.UI.Button closeButton;

    [Header("Aspecto")]
    [SerializeField] Color backColor = new(0.3f, 0.5f, 0.3f);
    [SerializeField] Color faceColor = new(0.97f, 0.94f, 0.86f);
    [SerializeField] Color matchedColor = new(0.7f, 0.9f, 0.6f);

    [Header("Tiempos")]
    [Tooltip("Segundos que quedan visibles dos cartas que no coinciden.")]
    [SerializeField] float mismatchDelay = 1f;

    [Header("Sonidos (FMOD)")]
    [SerializeField] EventReference sonidoVoltearDeNuevo;

    int[] layout = new int[0];
    bool[] matched = new bool[0];
    int firstCard = -1;
    int matchedPairs;
    bool resolving;
    bool solved;

    public int CardCount => cards.Length;

    protected override void Awake()
    {
        base.Awake();

        for (int i = 0; i < cards.Length; i++)
        {
            int index = i;
            cards[i].Button.onClick.AddListener(() => SelectCard(index));
        }
        restartButton.onClick.AddListener(Restart);
        closeButton.onClick.AddListener(Close);
    }

    /// <param name="symbols">Un sprite por pareja.</param>
    /// <param name="cardBack">Reverso común de todas las cartas.</param>
    /// <param name="cardLayout">Índice de símbolo de cada carta; cada símbolo aparece exactamente dos veces.</param>
    public void Open(IReadOnlyList<Sprite> symbols, Sprite cardBack, IReadOnlyList<int> cardLayout)
    {
        Show();

        if (!IsValidLayout(cardLayout, symbols.Count, cards.Length, out var error))
        {
            Debug.LogError($"Memoria mal configurada: {error}", this);
            Finish(false);
            return;
        }

        layout = new int[cardLayout.Count];
        for (int i = 0; i < layout.Length; i++)
        {
            layout[i] = cardLayout[i];
            cards[i].Setup(symbols[layout[i]], cardBack);
        }
        matched = new bool[layout.Length];

        Restart();
    }

    /// <summary>Vuelve a poner todas las cartas boca abajo. No disponible tras resolver.</summary>
    public void Restart()
    {
        if (solved)
            return;

        StopAllCoroutines();
        ResetBoard();
    }

    // Tras resolver, cerrar no anula el resultado.
    public override void Close() => Finish(solved);

    protected override void OnClosed()
    {
        StopAllCoroutines();
        solved = false;
        ResetBoard();
    }

    public void SelectCard(int index)
    {
        if (!IsOpen || resolving || solved || matched[index] || index == firstCard)
            return;

        cards[index].ShowRevealed(faceColor);
        resultText.text = string.Empty;

        if (firstCard < 0)
        {
            firstCard = index;
            return;
        }

        var first = firstCard;
        firstCard = -1;

        if (layout[first] == layout[index])
        {
            matched[first] = matched[index] = true;
            cards[first].ShowMatched(matchedColor);
            cards[index].ShowMatched(matchedColor);
            matchedPairs++;

            if (matchedPairs * 2 >= layout.Length)
            {
                solved = true;
                restartButton.interactable = false;
                resultText.text = "¡Has encontrado todas las parejas! El mecanismo se ha activado.";
            }
            else
            {
                resultText.text = "¡Pareja!";
            }
            return;
        }

        StartCoroutine(HideMismatch(first, index));
    }

    IEnumerator HideMismatch(int a, int b)
    {
        resolving = true;
        resultText.text = "No coinciden.";
        yield return new WaitForSecondsRealtime(mismatchDelay);

        if (!sonidoVoltearDeNuevo.IsNull)
        RuntimeManager.PlayOneShot(sonidoVoltearDeNuevo);

        cards[a].ShowHidden(backColor);
        cards[b].ShowHidden(backColor);
        resultText.text = string.Empty;
        resolving = false;
    }

    void ResetBoard()
    {
        firstCard = -1;
        matchedPairs = 0;
        resolving = false;
        System.Array.Clear(matched, 0, matched.Length);
        foreach (var card in cards)
            card.ShowHidden(backColor);
        restartButton.interactable = true;
        resultText.text = string.Empty;
    }

    public static bool IsValidLayout(IReadOnlyList<int> cardLayout, int symbolCount, int cardCount, out string error)
    {
        error = null;
        if (cardLayout == null || cardLayout.Count != cardCount)
        {
            error = $"el orden debe tener {cardCount} cartas.";
            return false;
        }
        if (symbolCount * 2 != cardCount)
        {
            error = $"se necesitan {cardCount / 2} símbolos.";
            return false;
        }

        var counts = new int[symbolCount];
        foreach (var symbol in cardLayout)
        {
            if (symbol < 0 || symbol >= symbolCount)
            {
                error = $"el símbolo {symbol} no existe (usa 0 a {symbolCount - 1}).";
                return false;
            }
            counts[symbol]++;
        }
        for (int i = 0; i < symbolCount; i++)
        {
            if (counts[i] != 2)
            {
                error = $"el símbolo {i} aparece {counts[i]} veces; debe aparecer 2.";
                return false;
            }
        }
        return true;
    }
}
