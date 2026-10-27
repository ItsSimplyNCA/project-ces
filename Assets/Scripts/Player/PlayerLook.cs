using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLook : MonoBehaviour {
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float sensitivity = 0.1f;
    [SerializeField] private float pitchLimit = 85f;
    [SerializeField] private PlayerUIState uiState;

    private float pitch;

    private void OnEnable() {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnDisable() {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update() {
        if (uiState.IsOpen) {
            return;
        }

        if (Mouse.current == null || Cursor.lockState != CursorLockMode.Locked) {
            return;
        }

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        float yawDelta = mouseDelta.x * sensitivity;

        pitch -= mouseDelta.y * sensitivity;
        pitch = Mathf.Clamp(pitch, -pitchLimit, pitchLimit);

        transform.Rotate(Vector3.up, yawDelta, Space.Self);
        cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }
}
