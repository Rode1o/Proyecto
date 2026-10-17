using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pestañas del panel de configuración: cada botón muestra su página y queda marcado como activo.
/// </summary>
public class SettingsTabs : MonoBehaviour
{
    [Serializable]
    class Tab
    {
        public Button button;
        public GameObject page;
    }

    [SerializeField] Tab[] tabs = Array.Empty<Tab>();

    int current = -1;

    public Selectable CurrentTabButton => current >= 0 ? tabs[current].button : null;

    void Awake()
    {
        for (var i = 0; i < tabs.Length; i++)
        {
            var index = i;
            tabs[i].button.onClick.AddListener(() => Select(index));
        }
    }

    void OnEnable()
    {
        Select(current < 0 ? 0 : current);
    }

    public void Select(int index)
    {
        current = index;
        for (var i = 0; i < tabs.Length; i++)
        {
            tabs[i].page.SetActive(i == index);
            if (tabs[i].button.TryGetComponent<MenuButtonVisual>(out var visual))
                visual.Pinned = i == index;
        }
    }
}
