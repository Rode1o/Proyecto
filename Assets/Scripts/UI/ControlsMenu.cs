using System;
using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// Muestra la lista de controles del juego. El gameplay lee el ratón directamente
/// (ClickToMove, PlayerInteractor, OrbitCamera, Minigame), así que la lista se define aquí
/// y se puede editar desde el Inspector si cambian los controles.
/// </summary>
public class ControlsMenu : MonoBehaviour
{
    [Serializable]
    struct Entry
    {
        public string action;
        public string control;

        public Entry(string action, string control)
        {
            this.action = action;
            this.control = control;
        }
    }

    [SerializeField] TMP_Text listText;
    [SerializeField] Entry[] entries =
    {
        new("Moverse", "Clic izq. sobre el terreno"),
        new("Girar la cámara", "Mantener clic der. y arrastrar"),
        new("Interactuar", "Clic izq. sobre un mecanismo y mantener"),
        new("Jugar minijuego", "Clic izq. sobre los puntos o cartas"),
        new("Cerrar minijuego", "Esc o botón Cerrar"),
    };

    void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        var sb = new StringBuilder();
        sb.Append("<b><color=#8C4A14>Acción<pos=38%>Teclado y ratón</color></b>\n\n");

        foreach (var entry in entries)
            sb.Append(entry.action).Append("<pos=38%>").Append(entry.control).Append('\n');

        listText.text = sb.ToString();
    }
}
