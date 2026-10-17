using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Movimiento WASD / flechas para el jugador de prueba. La dirección es relativa a la cámara
/// y solo gira el modelo visual, para que la cámara hija no rote con él.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlaceholderPlayerMovement : MonoBehaviour
{
    [SerializeField] float moveSpeed = 3.5f;
    [SerializeField] float turnSpeed = 720f;
    [SerializeField] float gravity = -20f;
    [Tooltip("Modelo que gira hacia la dirección de movimiento.")]
    [SerializeField] Transform visual;
    [SerializeField] Camera viewCamera;

    CharacterController controller;
    float verticalSpeed;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (viewCamera == null)
            viewCamera = Camera.main;
    }

    void Update()
    {
        var input = ReadInput();
        var move = ToWorld(input) * moveSpeed;

        verticalSpeed = controller.isGrounded ? -1f : verticalSpeed + gravity * Time.deltaTime;
        move.y = verticalSpeed;
        controller.Move(move * Time.deltaTime);

        if (visual != null && input.sqrMagnitude > 0.001f)
        {
            var target = Quaternion.LookRotation(ToWorld(input), Vector3.up);
            visual.rotation = Quaternion.RotateTowards(visual.rotation, target, turnSpeed * Time.deltaTime);
        }
    }

    static Vector2 ReadInput()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null)
            return Vector2.zero;

        var input = Vector2.zero;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) input.y += 1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) input.y -= 1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input.x += 1f;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) input.x -= 1f;
        return Vector2.ClampMagnitude(input, 1f);
    }

    Vector3 ToWorld(Vector2 input)
    {
        var reference = viewCamera != null ? viewCamera.transform : transform;
        var forward = Vector3.ProjectOnPlane(reference.forward, Vector3.up).normalized;
        var right = Vector3.ProjectOnPlane(reference.right, Vector3.up).normalized;
        return forward * input.y + right * input.x;
    }
}
