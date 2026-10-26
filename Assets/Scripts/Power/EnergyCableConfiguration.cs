using UnityEngine;
using UnityEngine.InputSystem;

public class EnergyCableConfiguration : MonoBehaviour {
    [SerializeField] private Camera playerCamera;
    [SerializeField] private PlayerUIState uiState;
    [SerializeField] private BuildingPreviewController buildingPreviewController;
    [SerializeField] private float interactionDistance = 6f;

    private EnergyCableSegment selectedCable;

    public EnergyCableSegment SelectedCable => selectedCable;

    private void Update() {
        if (uiState.IsOpen || !buildingPreviewController.IsConfigurationMode) {
            selectedCable = null;
            return;
        }

        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        bool hasHit = Physics.Raycast(
            ray,
            out RaycastHit hit,
            interactionDistance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore
        );

        EnergyCableSegment target = hasHit
            ? hit.collider.GetComponentInParent<EnergyCableSegment>()
            : null;

        if (target == null) {
            UniversalIO targetIO = hasHit
                ? hit.collider.GetComponentInParent<UniversalIO>()
                : null;

            if (selectedCable != null && targetIO != null) {
                GridWorld gridWorld = selectedCable.GetComponentInParent<GridWorld>();

                bool portSuccess = selectedCable.TryTogglePortConnection(
                    targetIO,
                    gridWorld,
                    out bool portConnected
                );

                Debug.Log(
                    portSuccess
                        ? portConnected
                            ? "Energy cable connected to machine port"
                            : "Energy cable disconnected from machine port"
                        : "Connection failed: no adjacent machine port",
                    targetIO
                );
            }

            selectedCable = null;
            return;
        }

        if (selectedCable == null) {
            selectedCable = target;

            Debug.Log($"Selected energy cable at {target.GridCell}", target);

            return;
        }

        if (selectedCable == target) {
            selectedCable = null;
            Debug.Log("Energy cable selection cleared");
            return;
        }

        bool success = selectedCable.TryToggleConnectionTo(target, out bool connected);

        Debug.Log(
            success
                ? connected
                    ? "Energy cables connected"
                    : "Energy cables disconnected"
                : "Connection failed: cables must be adjacent",
            target
        );

        selectedCable = null;
    }
}
