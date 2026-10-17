using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Selector de opciones "‹ valor ›". Izquierda/derecha (teclado o mando) cambian el valor;
/// arriba/abajo navegan como cualquier Selectable.
/// </summary>
public class OptionSelector : Selectable
{
    [SerializeField] TMP_Text valueLabel;
    [SerializeField] Button previousButton;
    [SerializeField] Button nextButton;
    [SerializeField] bool wrap = true;

    readonly List<string> options = new();
    int index;

    public event Action<int> ValueChanged;

    public int Index => index;

    protected override void Awake()
    {
        base.Awake();
        if (previousButton != null)
            previousButton.onClick.AddListener(() => Step(-1));
        if (nextButton != null)
            nextButton.onClick.AddListener(() => Step(1));
    }

    public void SetOptions(IEnumerable<string> values, int selectedIndex)
    {
        options.Clear();
        options.AddRange(values);
        index = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, options.Count - 1));
        Refresh();
    }

    public override void OnMove(AxisEventData eventData)
    {
        switch (eventData.moveDir)
        {
            case MoveDirection.Left:
                Step(-1);
                break;
            case MoveDirection.Right:
                Step(1);
                break;
            default:
                base.OnMove(eventData);
                break;
        }
    }

    public void Step(int direction)
    {
        if (!IsInteractable() || options.Count < 2)
            return;

        var next = index + direction;
        if (wrap)
            next = (next + options.Count) % options.Count;
        else
            next = Mathf.Clamp(next, 0, options.Count - 1);

        if (next == index)
            return;

        index = next;
        Refresh();
        ValueChanged?.Invoke(index);
    }

    void Refresh()
    {
        if (valueLabel != null)
            valueLabel.text = options.Count > 0 ? options[index] : "—";
    }
}
