using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-90)]
public class PowerStorageGUI : MonoBehaviour {
    [SerializeField] private Camera playerCamera;
    [SerializeField] private PlayerUIState uiState;
    [SerializeField] private float interactionDistance = 6f;

    [SerializeField] private GameObject powerStoragePanel;
    [SerializeField] private TMP_Text powerStorageStatusText;
    [SerializeField] private PortConfigurationGUI portConfigurationGUI;

    private PowerStorage storage;
    private bool isOpen;

    private void Start() {
        powerStoragePanel.SetActive(false);
    }

    private void Update() {
        if (isOpen) {
            bool closePressed = Keyboard.current != null && (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame);

            if (storage == null || closePressed) {
                Close();
            } else {
                RefreshStatus();
            }

            return;
        }

        if (uiState.IsOpen || Keyboard.current == null) return;
        if (!Keyboard.current.eKey.wasPressedThisFrame) return;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        bool hasHit = Physics.Raycast(
            ray,
            out RaycastHit hit,
            interactionDistance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore
        );

        if (!hasHit) return;

        PowerStorage target = hit.collider.GetComponentInParent<PowerStorage>();

        if (target == null || !uiState.TryOpen(this)) return;

        storage = target;
        isOpen = true;

        portConfigurationGUI.Bind(storage.GetComponent<UniversalIO>());

        RefreshStatus();
        powerStoragePanel.SetActive(true);
    }

    private void OnDisable() {
        if (isOpen) {
            Close();
        }
    }

    private void Close() {
        portConfigurationGUI.Unbind();
        powerStoragePanel.SetActive(false);

        storage = null;
        isOpen = false;

        uiState.Close(this);
    }

    private void RefreshStatus() {
        string transferLimit = float.IsPositiveInfinity(storage.TransferLimitMW) ? "Unlimited" : $"{storage.TransferLimitMW:0.##} MW/s";

        powerStorageStatusText.text =
            $"Tier: {storage.Tier}\n" +
            $"Stored: {storage.StoredPowerMW:0.##} / {storage.CapacityMW:0.##} MW\n" +
            $"Charge: {storage.ChargeRatio:P1}\n" +
            $"Input: {storage.InputRateMW:0.##} MW/s\n" +
            $"Output: {storage.OutputRateMW:0.##} MW/s\n" +
            $"Net Production: {storage.NetRateMW:+0.##;-0.##;0} MW/s\n" +
            $"Transfer limit: {transferLimit}";
    }
}
