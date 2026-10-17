using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Muestra los controles leyendo los bindings del InputActionAsset,
/// así la lista se actualiza sola si cambian las acciones.
/// </summary>
public class ControlsMenu : MonoBehaviour
{
    const string KeyboardGroup = "Keyboard&Mouse";
    const string GamepadGroup = "Gamepad";

    static readonly Dictionary<string, string> ActionNames = new()
    {
        { "Move", "Moverse" },
        { "Look", "Mirar" },
        { "Attack", "Atacar" },
        { "Interact", "Interactuar" },
        { "Crouch", "Agacharse" },
        { "Jump", "Saltar" },
        { "Sprint", "Correr" },
        { "Previous", "Anterior" },
        { "Next", "Siguiente" },
    };

    [SerializeField] InputActionAsset actions;
    [SerializeField] string actionMapName = "Player";
    [SerializeField] TMP_Text listText;

    void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        var map = actions != null ? actions.FindActionMap(actionMapName) : null;
        if (map == null)
        {
            listText.text = "No se encontraron controles.";
            return;
        }

        var sb = new StringBuilder();
        sb.Append("<b><color=#8C4A14>Acción<pos=30%>Teclado y ratón<pos=68%>Mando</color></b>\n\n");

        foreach (var action in map.actions)
        {
            var keyboard = Describe(action, KeyboardGroup);
            var gamepad = Describe(action, GamepadGroup);
            if (keyboard == null && gamepad == null)
                continue;

            var name = ActionNames.TryGetValue(action.name, out var translated) ? translated : action.name;
            sb.Append(name).Append("<pos=30%>").Append(keyboard ?? "—")
              .Append("<pos=68%>").Append(gamepad ?? "—").Append('\n');
        }

        listText.text = sb.ToString();
    }

    static readonly Dictionary<string, string> ControlNames = new()
    {
        { "LMB", "Clic izq." },
        { "RMB", "Clic der." },
        { "Space", "Espacio" },
        { "Delta", "Ratón" },
        { "LS", "Stick izq." },
        { "RS", "Stick der." },
        { "Left Stick Press", "L3" },
        { "Right Stick Press", "R3" },
        { "D-Pad/Up", "Cruceta arriba" },
        { "D-Pad/Down", "Cruceta abajo" },
        { "D-Pad/Left", "Cruceta izq." },
        { "D-Pad/Right", "Cruceta der." },
    };

    static readonly HashSet<string> ArrowKeys = new() { "Up", "Down", "Left", "Right" };

    static string Describe(InputAction action, string group)
    {
        var mask = InputBinding.MaskByGroup(group);
        var bindings = action.bindings;
        var parts = new List<string>();

        for (var i = 0; i < bindings.Count; i++)
        {
            if (bindings[i].isComposite)
            {
                var keys = new List<(string part, string key)>();
                for (var j = i + 1; j < bindings.Count && bindings[j].isPartOfComposite; j++)
                {
                    if (mask.Matches(bindings[j]))
                        keys.Add((bindings[j].name, action.GetBindingDisplayString(j, InputBinding.DisplayStringOptions.DontIncludeInteractions)));
                }

                if (keys.Count > 0)
                    parts.Add(JoinComposite(keys));
                continue;
            }

            if (bindings[i].isPartOfComposite || !mask.Matches(bindings[i]))
                continue;

            parts.Add(Translate(action.GetBindingDisplayString(i)));
        }

        var unique = new List<string>();
        foreach (var part in parts)
        {
            if (!string.IsNullOrWhiteSpace(part) && !unique.Contains(part))
                unique.Add(part);
        }

        return unique.Count > 0 ? string.Join(" / ", unique) : null;
    }

    // Orden en que se leen las teclas de dirección: W A S D.
    static readonly string[] DirectionOrder = { "up", "left", "down", "right" };

    static string JoinComposite(List<(string part, string key)> keys)
    {
        keys.Sort((a, b) => DirectionIndex(a.part).CompareTo(DirectionIndex(b.part)));

        var letters = "";
        var hasArrows = false;
        var others = new List<string>();
        foreach (var (_, key) in keys)
        {
            if (ArrowKeys.Contains(key))
                hasArrows = true;
            else if (key.Length == 1)
                letters += key;
            else
                others.Add(Translate(key));
        }

        var groups = new List<string>();
        if (letters.Length > 0)
            groups.Add(letters);
        if (hasArrows)
            groups.Add("Flechas");
        if (others.Count > 0)
            groups.Add(string.Join("/", others));
        return string.Join(" / ", groups);
    }

    static int DirectionIndex(string part)
    {
        var index = System.Array.IndexOf(DirectionOrder, part?.ToLowerInvariant());
        return index < 0 ? DirectionOrder.Length : index;
    }

    static string Translate(string control)
    {
        const string holdPrefix = "Hold ";
        if (control.StartsWith(holdPrefix))
            return "Mantener " + Translate(control.Substring(holdPrefix.Length));

        return ControlNames.TryGetValue(control, out var translated) ? translated : control;
    }
}
