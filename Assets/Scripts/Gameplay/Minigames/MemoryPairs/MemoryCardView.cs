using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Vista de una carta del minijuego de memoria. Solo muestra estados; las reglas están en <see cref="MemoryPairsMinigame"/>.
/// Jerarquía: Card (Image de fondo + Button) → Symbol (Image) y Back (Image).
/// </summary>
public class MemoryCardView : MonoBehaviour
{
    [SerializeField] Button button;
    [Tooltip("Fondo de la carta; cambia de color según el estado.")]
    [SerializeField] Image background;
    [SerializeField] Image symbol;
    [SerializeField] Image back;

    public Button Button => button;

    public void Setup(Sprite symbolSprite, Sprite backSprite)
    {
        symbol.sprite = symbolSprite;
        back.sprite = backSprite;
        back.enabled = backSprite != null;
    }

    public void ShowHidden(Color backColor)
    {
        background.color = backColor;
        symbol.gameObject.SetActive(false);
        back.gameObject.SetActive(true);
        button.interactable = true;
    }

    public void ShowRevealed(Color faceColor)
    {
        background.color = faceColor;
        symbol.gameObject.SetActive(true);
        back.gameObject.SetActive(false);
    }

    public void ShowMatched(Color matchedColor)
    {
        ShowRevealed(matchedColor);
        button.interactable = false;
    }
}
