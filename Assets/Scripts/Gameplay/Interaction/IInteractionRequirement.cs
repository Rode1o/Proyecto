/// <summary>
/// Condición que debe cumplirse para poder usar un <see cref="Interactable"/>.
/// Se añade como componente en el mismo GameObject; el Interactable las detecta solo.
/// </summary>
public interface IInteractionRequirement
{
    /// <param name="reason">Mensaje para el jugador cuando no se cumple.</param>
    bool IsMet(out string reason);
}
