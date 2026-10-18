using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Mensaje breve en pantalla (p. ej. "Recoge todos los objetos de la isla primero").
/// Muestra los avisos de <see cref="PlayerInteractor.InteractionBlocked"/>.
/// </summary>
public class HudMessage : MonoBehaviour
{
    [Tooltip("Vacío = el PlayerInteractor de la escena.")]
    [SerializeField] PlayerInteractor interactor;
    [Tooltip("Raíz del mensaje; se activa mientras se muestra.")]
    [SerializeField] GameObject root;
    [SerializeField] TMP_Text text;
    [SerializeField, Min(0.1f)] float duration = 2.5f;

    void Awake()
    {
        root.SetActive(false);
        if (interactor == null)
            interactor = FindAnyObjectByType<PlayerInteractor>();
    }

    void OnEnable()
    {
        if (interactor != null)
            interactor.InteractionBlocked += OnInteractionBlocked;
    }

    void OnDisable()
    {
        if (interactor != null)
            interactor.InteractionBlocked -= OnInteractionBlocked;
    }

    void OnInteractionBlocked(Interactable _, string reason) => Show(reason);

    public void Show(string message)
    {
        if (string.IsNullOrEmpty(message))
            return;

        StopAllCoroutines();
        text.text = message;
        root.SetActive(true);
        StartCoroutine(HideAfterDelay());
    }

    IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(duration);
        root.SetActive(false);
    }
}
