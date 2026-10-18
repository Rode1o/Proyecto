using System;
using FMODUnity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Texto de un diálogo con opciones, editable desde el Inspector.</summary>
[Serializable]
public class DialogContent
{
    public string speaker = "Oveja";
    [TextArea(2, 5)] public string text;
    [Tooltip("Una etiqueta por botón, en orden. Sin opciones, el diálogo se cierra con un clic.")]
    public string[] options = { "Sí", "No" };
    [Tooltip("Opcional: retrato para este diálogo. Vacío = el retrato por defecto del cuadro.")]
    public Sprite portrait;
}

/// <summary>
/// Cuadro de diálogo modal reutilizable. Muestra un <see cref="DialogContent"/> y devuelve el índice
/// de la opción elegida. El root incluye un bloqueador a pantalla completa con Raycast Target, así
/// ClickToMove, PlayerInteractor y OrbitCamera ignoran los clics mientras está abierto.
/// Un diálogo sin opciones (una réplica) se cierra con un clic en cualquier parte o tras unos segundos,
/// y devuelve -1.
/// </summary>
public class DialogBox : MonoBehaviour
{
    [SerializeField] GameObject root;
    [SerializeField] TMP_Text speakerText;
    [SerializeField] TMP_Text bodyText;
    [Tooltip("Botones de opción en orden; se ocultan los que no tengan texto.")]
    [SerializeField] Button[] optionButtons;
    [SerializeField] TMP_Text[] optionLabels;

    [Header("Retrato (opcional)")]
    [SerializeField] Image portrait;
    [SerializeField] Sprite defaultPortrait;

    [Header("Réplicas sin opciones")]
    [Tooltip("Bloqueador a pantalla completa: un clic cierra las réplicas.")]
    [SerializeField] Button dismissArea;
    [Tooltip("Las réplicas se cierran solas tras estos segundos (0 = solo con clic).")]
    [SerializeField, Min(0f)] float replyDuration = 3f;

    [Header("Sonidos (FMOD, opcionales)")]
    [Tooltip("Suena cada vez que se abre el diálogo (la voz de quien habla).")]
    [SerializeField] EventReference openSound;
    [Tooltip("Un sonido por opción, en el mismo orden que los botones (p. ej. Sí, No).")]
    [SerializeField] EventReference[] optionSounds = Array.Empty<EventReference>();

    Action<int> onChosen;
    bool isReply;
    float replyTimer;

    public bool IsOpen => root.activeSelf;

    void Awake()
    {
        root.SetActive(false);
        for (int i = 0; i < optionButtons.Length; i++)
        {
            int index = i;
            optionButtons[i].onClick.AddListener(() => Choose(index));
        }
        if (dismissArea != null)
            dismissArea.onClick.AddListener(() =>
            {
                if (isReply)
                    Choose(-1);
            });
    }

    void Update()
    {
        if (!IsOpen || !isReply || replyDuration <= 0f)
            return;

        replyTimer += Time.unscaledDeltaTime;
        if (replyTimer >= replyDuration)
            Choose(-1);
    }

    public void Show(DialogContent content, Action<int> chosen)
    {
        onChosen = chosen;
        speakerText.text = content.speaker;
        bodyText.text = content.text;

        if (portrait != null)
        {
            portrait.sprite = content.portrait != null ? content.portrait : defaultPortrait;
            portrait.enabled = portrait.sprite != null;
        }

        var optionCount = 0;
        for (int i = 0; i < optionButtons.Length; i++)
        {
            var hasOption = content.options != null && i < content.options.Length && !string.IsNullOrEmpty(content.options[i]);
            optionButtons[i].gameObject.SetActive(hasOption);
            if (hasOption)
            {
                optionLabels[i].text = content.options[i];
                optionCount++;
            }
        }

        isReply = optionCount == 0;
        replyTimer = 0f;
        root.SetActive(true);
        Play(openSound);
    }

    /// <summary>Réplica sin opciones (p. ej. la respuesta de la oveja al rechazarla).</summary>
    public void Say(string speaker, string text, Sprite portraitOverride = null) =>
        Show(new DialogContent { speaker = speaker, text = text, options = Array.Empty<string>(), portrait = portraitOverride }, null);

    public void Choose(int index)
    {
        if (!IsOpen)
            return;

        // -1 = réplica cerrada sin elegir: no lleva sonido de botón.
        if (index >= 0 && index < optionSounds.Length)
            Play(optionSounds[index]);

        root.SetActive(false);
        var callback = onChosen;
        onChosen = null;
        callback?.Invoke(index);
    }

    static void Play(EventReference sound)
    {
        if (!sound.IsNull)
            RuntimeManager.PlayOneShot(sound);
    }
}
