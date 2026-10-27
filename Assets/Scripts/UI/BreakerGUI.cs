using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DefaultExecutionOrder(-90)]
public class BreakerGUI : MonoBehaviour {
    [SerializeField] private Camera playerCamera;
    [SerializeField] private PlayerUIState uiState;
    [SerializeField] private float interactionDistance = 6f;

    [SerializeField] GameObject breakerPanel;
    [SerializeField] TMP_Text breakerStatusText;
    [SerializeField] Button toggleButton;
    [SerializeField] TMP_Text toggleButtonText;
    [SerializeField] PortConfigurationGUI portConfigurationGUI;

    private PowerBreaker breaker;
    private bool isOpen;

    private void Awake() {
        toggleButton.onClick.AddListener(ToggleBreaker);
    }

    private void Start() {
        breakerPanel.SetActive(false);
    }

    private void Update() {
        if (isOpen) {
            bool closePressed = Keyboard.current != null && (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame);

            if (breaker == null || closePressed) {
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

        PowerBreaker target = hit.collider.GetComponentInParent<PowerBreaker>();

        if (target == null || !uiState.TryOpen(this)) return;

        breaker = target;
        isOpen = true;

        portConfigurationGUI.Bind(breaker.GetComponent<UniversalIO>());

        RefreshStatus();
        breakerPanel.SetActive(true);
    }

    private void OnDisable() {
        if (isOpen) Close();
    }

    private void OnDestroy() {
        if (toggleButton != null) {
            toggleButton.onClick.RemoveListener(ToggleBreaker);
        }
    }

    private void Close() {
        portConfigurationGUI.Unbind();
        breakerPanel.SetActive(false);

        breaker = null;
        isOpen = false;

        uiState.Close(this);
    }

    private void RefreshStatus() {
        string status = breaker.IsTripped
            ? "Tripped: Overload"
            : breaker.IsClosed
                ? "Closed"
                : "Open";

        breakerStatusText.text =
            $"Status: {status}\n" +
            $"Power forwarding: " +
            $"{(breaker.ConductsPower ? "Enabled" : "Disabled")}";

        toggleButtonText.text = breaker.IsClosed
            ? "Open Circuits"
            : "Close Circuits";

        toggleButton.interactable = !breaker.IsTripped;
    }

    private void ToggleBreaker() {
        if (!isOpen || breaker == null) return;

        breaker.SetClosed(!breaker.IsClosed);
        RefreshStatus();
    }
}
