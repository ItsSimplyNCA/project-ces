using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DefaultExecutionOrder(-80)]
public class PauseGUI : MonoBehaviour {
    [SerializeField] private PlayerUIState uiState;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button resumeButton;

    private bool isPaused;
    private float previousTimeScale = 1f;

    private void Awake() {
        resumeButton.onClick.AddListener(Resume);
        pausePanel.SetActive(false);
    }

    private void Update() {
        if (Keyboard.current == null) return;
        if (!Keyboard.current.escapeKey.wasPressedThisFrame) return;

        if (isPaused) {
            Resume();
            return;
        }

        if (uiState.IsOpen || uiState.ClosedThisFrame) return;

        Pause();
    }

    private void Pause() {
        if (!uiState.TryOpen(this)) return;

        previousTimeScale = Time.timeScale;
        isPaused = true;

        Time.timeScale = 0f;
        pausePanel.SetActive(true);
    }

    private void Resume() {
        if (!isPaused) return;

        isPaused = false;
        Time.timeScale = previousTimeScale;

        if (pausePanel != null) {
            pausePanel.SetActive(false);
        }

        if (uiState != null) {
            uiState.Close(this);
        }
    }

    private void OnDisable() {
        Resume();
    }

    private void OnDestroy() {
        if (resumeButton != null) {
            resumeButton.onClick.RemoveListener(Resume);
        }
    }
}
