using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DefaultExecutionOrder(-90)]

public class GeneratorGUI : MonoBehaviour {
    [SerializeField] private Camera playerCamera;
    [SerializeField] private PlayerUIState uiState;
    [SerializeField] private float interactionDistance = 6f;

    [SerializeField] private GameObject generatorPanel;
    [SerializeField] private TMP_Text generatorStatusText;
    [SerializeField] private Button onOffButton;
    [SerializeField] private TMP_Text onOffButtonText;
    [SerializeField] private PortConfigurationGUI portConfigurationGUI;

    private PowerGenerator generator;
    private bool isOpen;

    private void Awake() {
        onOffButton.onClick.AddListener(ToggleGenerator);
    }

    private void Start() {
        generatorPanel.SetActive(false);
    }

    private void Update() {
        if (isOpen) {
            bool closePressed = Keyboard.current != null && (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame);

            if (generator == null || closePressed) {
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

        PowerGenerator target = hit.collider.GetComponentInParent<PowerGenerator>();

        if (target == null || !uiState.TryOpen(this)) return;

        generator = target;
        isOpen = true;

        portConfigurationGUI.Bind(generator.GetComponent<UniversalIO>());

        RefreshStatus();
        generatorPanel.SetActive(true);
    }

    private void OnDisable() {
        if (isOpen) Close();
    }

    private void OnDestroy() {
        if (onOffButton != null) {
            onOffButton.onClick.RemoveListener(ToggleGenerator);
        }
    }

    private void Close() {
        portConfigurationGUI.Unbind();
        generatorPanel.SetActive(false);

        generator = null;
        isOpen = false;

        uiState.Close(this);
    }

    private void RefreshStatus() {
        string status = generator.IsTripped
            ? "Tripped: Overloaded"
            : generator.IsOn
                ? "ON"
                : "OFF";

        generatorStatusText.text =
            $"Status: {status}\n" +
            $"Available power: {generator.AvailablePowerMW:0.###} MW";

        onOffButtonText.text = generator.IsOn
            ? "Turn OFF"
            : "Turn ON";
    }

    private void ToggleGenerator() {
        if (!isOpen || generator == null) return;

        generator.SetOn(!generator.IsOn);
        RefreshStatus();
    }
}
