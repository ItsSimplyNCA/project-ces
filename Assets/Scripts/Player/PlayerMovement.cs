using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour {
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private PlayerUIState uiState;
    [SerializeField, Min(0f)] private float jumpHeight = 1.5f; 

    private CharacterController controller;
    private float verticalVelocity;

    private void Awake() {
        controller = GetComponent<CharacterController>();
    }

    private void Update() {
        Keyboard keyboard = uiState.IsOpen
            ? null
            : Keyboard.current;

        Vector2 input = Vector2.zero;

        if (keyboard != null) {
            input.x =
                (keyboard.dKey.isPressed ? 1f : 0f) - 
                (keyboard.aKey.isPressed ? 1f : 0f);

            input.y =
                (keyboard.wKey.isPressed ? 1f : 0f) -
                (keyboard.sKey.isPressed ? 1f : 0f);
        }

        input = Vector2.ClampMagnitude(input, 1f);

        Vector3 horizontalVelocity = (transform.right * input.x + transform.forward * input.y) * moveSpeed;

        if (controller.isGrounded && verticalVelocity < 0f) {
            verticalVelocity = -2f;
        }

        if (controller.isGrounded && keyboard != null && keyboard.spaceKey.wasPressedThisFrame) {
            verticalVelocity = Mathf.Sqrt(-2f * gravity * jumpHeight);
        }
        
        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = horizontalVelocity + Vector3.up * verticalVelocity;

        controller.Move(velocity * Time.deltaTime);
    }
}