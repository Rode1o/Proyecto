using UnityEngine;

/// <summary>
/// Mecanismo que abre el minijuego de memoria con sus propios símbolos, reverso y orden de cartas.
/// Varios mecanismos pueden compartir el mismo <see cref="MemoryPairsMinigame"/>.
/// </summary>
public class MemoryPairsMechanism : PuzzleMechanism
{
    [SerializeField] MemoryPairsMinigame minigame;
    [Tooltip("Un sprite por pareja (tres).")]
    [SerializeField] Sprite[] symbols = new Sprite[3];
    [SerializeField] Sprite cardBack;
    [Tooltip("Índice de símbolo (0-2) de cada carta, de izquierda a derecha y de arriba abajo. Cada uno, dos veces.")]
    [SerializeField] int[] layout = { 0, 1, 2, 1, 2, 0 };

    protected override Minigame Minigame => minigame;

    protected override void OpenMinigame() => minigame.Open(symbols, cardBack, layout);

    void OnValidate()
    {
        var cardCount = minigame != null ? minigame.CardCount : 6;
        if (!MemoryPairsMinigame.IsValidLayout(layout, symbols != null ? symbols.Length : 0, cardCount, out var error))
            Debug.LogWarning($"'{name}': {error}", this);
    }
}
