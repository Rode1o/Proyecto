using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Menú principal: los botones están siempre a la izquierda y Controles / Configuración
/// se despliegan en un panel a la derecha.
/// </summary>
public class MainMenu : MonoBehaviour
{
    [SerializeField] string gameSceneName = "SampleScene";

    [Header("Botones del menú principal")]
    [SerializeField] Button playButton;
    [SerializeField] Button controlsButton;
    [SerializeField] Button settingsButton;
    [SerializeField] Button exitButton;

    [Header("Paneles desplegables")]
    [SerializeField] PanelReveal settingsPanel;
    [SerializeField] PanelReveal controlsPanel;
    [SerializeField] Selectable settingsFirstSelected;
    [SerializeField] Selectable controlsFirstSelected;

    [Header("Volver (visible con un panel abierto)")]
    [SerializeField] Button backButton;

    PanelReveal openPanel;
    Button openedFrom;

    void Awake()
    {
        playButton.onClick.AddListener(Play);
        controlsButton.onClick.AddListener(() => OpenPanel(controlsPanel, controlsFirstSelected, controlsButton));
        settingsButton.onClick.AddListener(() => OpenPanel(settingsPanel, settingsFirstSelected, settingsButton));
        exitButton.onClick.AddListener(Exit);
        backButton.onClick.AddListener(ClosePanel);

#if UNITY_WEBGL
        exitButton.gameObject.SetActive(false);
#endif
    }

    void Start()
    {
        settingsPanel.Hide(instant: true);
        controlsPanel.Hide(instant: true);
        backButton.gameObject.SetActive(false);
        Select(playButton);
    }

    void Update()
    {
        if (openPanel == null)
            return;

        var backPressed = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                          || (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);
        if (backPressed)
            ClosePanel();
    }

    public void Play()
    {
        if (!Application.CanStreamedLevelBeLoaded(gameSceneName))
        {
            Debug.LogError($"La escena '{gameSceneName}' no está en Build Settings.", this);
            return;
        }

        SceneManager.LoadScene(gameSceneName);
    }

    public void Exit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void OpenPanel(PanelReveal panel, Selectable firstSelected, Button opener)
    {
        if (openPanel == panel)
        {
            Select(firstSelected);
            return;
        }

        if (openPanel != null)
            openPanel.Hide(instant: true);

        SetPinned(openedFrom, false);
        openPanel = panel;
        openedFrom = opener;
        SetPinned(openedFrom, true);

        panel.Show();
        backButton.gameObject.SetActive(true);
        Select(firstSelected);
    }

    public void ClosePanel()
    {
        if (openPanel == null)
            return;

        if (openPanel == settingsPanel)
            GameAudioSettings.Save();

        openPanel.Hide();
        openPanel = null;
        backButton.gameObject.SetActive(false);

        SetPinned(openedFrom, false);
        Select(openedFrom);
        openedFrom = null;
    }

    static void SetPinned(Button button, bool pinned)
    {
        if (button != null && button.TryGetComponent<MenuButtonVisual>(out var visual))
            visual.Pinned = pinned;
    }

    static void Select(Selectable selectable)
    {
        if (EventSystem.current != null && selectable != null)
            EventSystem.current.SetSelectedGameObject(selectable.gameObject);
    }
}
