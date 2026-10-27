using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ConfirmationGUI : MonoBehaviour {
    [SerializeField] private PlayerUIState uiState;
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private System.Action pendingAction;
    private bool isOpen;

    private void Awake() {
        confirmButton.onClick.AddListener(Confirm);
        cancelButton.onClick.AddListener(Cancel);
    }

    private void Start() {
        panel.SetActive(false);
    }

    private void Update() {
        if (isOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) {
            Cancel();
        }
    }

    private void OnDestroy() {
        confirmButton.onClick.RemoveListener(Confirm);
        cancelButton.onClick.RemoveListener(Cancel);
    }

    public bool Show(string message, System.Action onConfirmed) {
        if (!uiState.TryOpen(this)) {
            return false;
        }

        pendingAction = onConfirmed;
        messageText.text = message;

        isOpen = true;
        panel.SetActive(true);

        return true;
    }

    private void Confirm() {
        if (!isOpen) return;

        System.Action action = pendingAction;

        Close();

        action?.Invoke();
    }

    private void Cancel() {
        if (isOpen) {
            Close();
        }
    }

    private void Close() {
        panel.SetActive(false);

        pendingAction = null;
        isOpen = false;

        uiState.Close(this);
    }

    [ContextMenu("Debug/Show Confirmation")]
    private void ShowConfirmationForDebug() {
        if (!Application.isPlaying) return;

        Show(
            "This is a test confirmation.",
            () => Debug.Log("Confirmed")
        );
    }
}
