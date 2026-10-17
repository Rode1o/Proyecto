using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Base de los minijuegos de interfaz. Un mismo minijuego se reutiliza en varios mecanismos:
/// cada <see cref="PuzzleMechanism"/> lo abre con su propia configuración y escucha <see cref="Finished"/>.
/// El root debe incluir un bloqueador a pantalla completa con Raycast Target: así ClickToMove,
/// PlayerInteractor y OrbitCamera ven el puntero sobre la UI e ignoran los clics mientras está abierto.
/// </summary>
public abstract class Minigame : MonoBehaviour
{
    [Tooltip("Raíz visual que se muestra al abrir y se oculta al cerrar.")]
    [SerializeField] GameObject root;

    /// <summary>true si se resolvió, false si se cerró sin resolver.</summary>
    public event Action<bool> Finished;

    public bool IsOpen => root.activeSelf;

    protected virtual void Awake()
    {
        root.SetActive(false);
    }

    protected virtual void Update()
    {
        if (IsOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            Close();
    }

    /// <summary>Cierra la interfaz. Por defecto cuenta como no resuelto.</summary>
    public virtual void Close() => Finish(false);

    protected void Show() => root.SetActive(true);

    protected void Finish(bool solved)
    {
        if (!IsOpen)
            return;

        root.SetActive(false);
        OnClosed();
        Finished?.Invoke(solved);
    }

    protected virtual void OnClosed() { }
}
