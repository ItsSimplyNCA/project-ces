using UnityEngine;
using UnityEngine.InputSystem;

public class PipeConfiguration : MonoBehaviour {
    [SerializeField] private Camera playerCamera;
    [SerializeField] private PlayerUIState uiState;
    [SerializeField] private BuildingPreviewController buildingPreviewController;
    [SerializeField] private float interactionDistance = 6f;

    private PipeSegment selectedPipe;

    public PipeSegment SelectedPipe => selectedPipe;

    private void Update() {
        if (uiState.IsOpen || !buildingPreviewController.IsConfigurationMode) {
            selectedPipe = null;
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

        PipeSegment target = hasHit
            ? hit.collider.GetComponentInParent<PipeSegment>()
            : null;

        if (target == null) {
            UniversalIO targetIO = hasHit
                ? hit.collider.GetComponentInParent<UniversalIO>()
                : null;
            
            if (selectedPipe != null && targetIO != null) {
                GridWorld gridWorld = selectedPipe.GetComponentInParent<GridWorld>();

                bool portSuccess = selectedPipe.TryTogglePortConnection(
                    targetIO,
                    gridWorld,
                    out bool portConnected
                );

                Debug.Log(
                    portSuccess
                        ? portConnected
                            ? "Pipe connected to machine port"
                            : "Pipe disconnected from machine port"
                        : "Connection failed: no adjacent machine port",
                    targetIO
                );
            } else {
                Debug.Log("Pipe selection cleared");
            }

            selectedPipe = null;
            return;
        }

        if (selectedPipe == null) {
            selectedPipe = target;
            Debug.Log($"Selected pipe at {target.GridCell}", target);
            return;
        }

        if (selectedPipe == target) {
            selectedPipe = null;
            Debug.Log("Pipe selection cleared");
            return;
        }

        bool success = selectedPipe.TryToggleConnectionTo(target, out bool connected);

        if (success) {
            Debug.Log(connected ? "Pipes connected" : "Pipes disconnected", target);
        } else {
            Debug.Log("Connection failed: pipes must be adjacent and use the same medium", target);
        }

        selectedPipe = null;
    }
}
