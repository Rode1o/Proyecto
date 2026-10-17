using UnityEngine;

public enum CollectableCategory
{
    /// <summary>Objeto sin función especial (hojas, flores...).</summary>
    Item,
    /// <summary>Objeto que más adelante se podrá usar como herramienta.</summary>
    Tool
}

/// <summary>
/// Tipo de collectable (Hoja, Tijeras de jardinería...). Se crea como asset y lo comparten
/// todos los collectables de ese tipo. Crear: Assets → Create → Gameplay → Collectable Definition.
/// </summary>
[CreateAssetMenu(menuName = "Gameplay/Collectable Definition", fileName = "Collectable")]
public class CollectableDefinition : ScriptableObject
{
    [SerializeField] string displayName = "Objeto";
    [SerializeField] CollectableCategory category = CollectableCategory.Item;
    [Tooltip("Opcional, para un inventario futuro.")]
    [SerializeField] Sprite icon;

    public string DisplayName => displayName;
    public CollectableCategory Category => category;
    public Sprite Icon => icon;
    public bool IsTool => category == CollectableCategory.Tool;
}
