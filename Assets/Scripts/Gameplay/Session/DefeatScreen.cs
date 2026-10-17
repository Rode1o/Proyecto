using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Pantalla de derrota: se muestra cuando <see cref="GameSession"/> lanza Defeated.
/// Su bloqueador a pantalla completa impide clics en el mundo; "Reintentar" recarga la escena.
/// </summary>
public class DefeatScreen : MonoBehaviour
{
    [SerializeField] GameSession session;
    [SerializeField] GameObject root;
    [SerializeField] TMP_Text messageText;
    [SerializeField] Button retryButton;

    void Awake()
    {
        root.SetActive(false);
        retryButton.onClick.AddListener(Retry);
    }

    void OnEnable()
    {
        if (session != null)
            session.Defeated += Show;
    }

    void OnDisable()
    {
        if (session != null)
            session.Defeated -= Show;
    }

    void Show(string message)
    {
        messageText.text = message;
        root.SetActive(true);
    }

    void Retry()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
