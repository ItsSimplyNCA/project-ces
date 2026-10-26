using TMPro;
using UnityEngine;

public class CrosshairHUD : MonoBehaviour {
    [SerializeField] private PlayerUIState uiState;
    [SerializeField] private TMP_Text crosshairText;

    private void LateUpdate() {
        bool shouldShow = !uiState.IsOpen;

        if (crosshairText.enabled != shouldShow) {
            crosshairText.enabled = shouldShow;
        }
    }
}
