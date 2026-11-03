using UnityEngine;

[RequireComponent(typeof(CharacterController), typeof(PlayerMovement), typeof(PlayerLook))]

[RequireComponent(typeof(PlayerInventory))]
public class PlayerSaveHandler : MonoBehaviour {
    private CharacterController controller;
    private PlayerMovement movement;
    private PlayerLook playerLook;
    private PlayerInventory inventory;

    private void Awake() {
        controller = GetComponent<CharacterController>();
        movement = GetComponent<PlayerMovement>();
        playerLook = GetComponent<PlayerLook>();
        inventory = GetComponent<PlayerInventory>();
    }

    public PlayerSaveData CaptureSaveData() {
        return new PlayerSaveData {
            position = transform.position,
            rotation = transform.rotation,
            cameraPitch = playerLook.Pitch,
            inventory = inventory.CaptureSaveData()
        };
    }

    public bool TryRestoreSaveData(PlayerSaveData data, GameDefinitionCatalog catalog) {
        if (data == null) {
            Debug.LogWarning("Player load failed: missing data", this);
            return false;
        }

        if (!IsFinite(data.position.x) || !IsFinite(data.position.y) || !IsFinite(data.position.z) || !IsFinite(data.cameraPitch)) {
            Debug.LogWarning("Player load failed: invalid position or camera pitch", this);
            return false;
        }

        Quaternion rotation = data.rotation;

        if (!IsFinite(data.rotation.x) || !IsFinite(data.rotation.y) || !IsFinite(data.rotation.z) || !IsFinite(data.rotation.w)) {
            Debug.LogWarning("Player load failed: invalid rotation", this);
            return false;
        }

        float rotationLengthSquared = 
            rotation.x * rotation.x +
            rotation.y * rotation.y +
            rotation.z * rotation.z +
            rotation.w * rotation.w;

        if (!IsFinite(rotationLengthSquared) || rotationLengthSquared < 0.000001f) {
            Debug.LogWarning("Player load failed: invalid rotation length", this);
            return false;
        }

        rotation = rotation.normalized;

        if (!inventory.TryRestoreSaveData(data.inventory, catalog)) {
            return false;
        }

        bool controllerWasEnabled = controller.enabled;

        try {
            controller.enabled = false;
            transform.SetPositionAndRotation(data.position, rotation);
            playerLook.RestorePitch(data.cameraPitch);
            movement.ResetVerticalVelocity();
        } finally {
            controller.enabled = controllerWasEnabled;
        }

        return true;
    }

    private static bool IsFinite(float value) {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
