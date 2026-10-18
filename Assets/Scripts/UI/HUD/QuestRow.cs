using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Fila de un encargo: icono, nombre, progreso y check (apagado hasta completarlo).
/// La usan el panel de encargos y el diario; la descripción es opcional.
/// </summary>
public class QuestRow : MonoBehaviour
{
    [SerializeField] Image icon;
    [SerializeField] TMP_Text title;
    [Tooltip("Opcional: texto largo del diario.")]
    [SerializeField] TMP_Text description;
    [SerializeField] TMP_Text progressLabel;
    [SerializeField] Image check;

    [Header("Colores")]
    [SerializeField] Color checkOffColor = new(0.23f, 0.14f, 0.08f, 0.22f);
    [SerializeField] Color checkOnColor = Color.white;
    [SerializeField] Color titleColor = new Color32(59, 36, 20, 255);
    [SerializeField] Color completedTitleColor = new Color32(104, 68, 40, 200);

    [Header("Animación al completar")]
    [SerializeField] float popScale = 1.4f;
    [SerializeField] float popDuration = 0.3f;

    float popTime = -1f;

    public void Show(QuestDefinition quest, int progress, bool completed, bool animate)
    {
        if (icon != null)
        {
            icon.sprite = quest.Icon;
            icon.enabled = quest.Icon != null;
        }

        title.text = quest.Title;
        title.color = completed ? completedTitleColor : titleColor;

        if (description != null)
            description.text = quest.Description;

        // Con meta 1 basta el check: el contador se oculta y el nombre usa su sitio.
        if (progressLabel != null)
        {
            progressLabel.gameObject.SetActive(quest.Goal > 1);
            progressLabel.text = $"{progress}/{quest.Goal}";
        }

        check.color = completed ? checkOnColor : checkOffColor;
        if (animate && completed)
            popTime = 0f;
        else if (!completed)
            check.rectTransform.localScale = Vector3.one;
    }

    void Update()
    {
        if (popTime < 0f)
            return;

        popTime += Time.unscaledDeltaTime;
        var t = Mathf.Clamp01(popTime / popDuration);
        // Sube rápido y vuelve a 1 con un arco suave.
        var scale = 1f + (popScale - 1f) * Mathf.Sin(t * Mathf.PI);
        check.rectTransform.localScale = new Vector3(scale, scale, 1f);
        if (t >= 1f)
            popTime = -1f;
    }
}
