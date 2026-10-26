using UnityEngine;

public class PlayerUIState : MonoBehaviour {
    private MonoBehaviour activeWindow;

    public bool IsOpen => activeWindow != null;
    public bool TryOpen(MonoBehaviour window) {
        if (window == null || IsOpen) return false;

        activeWindow = window;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        return true;
    }

    public void Close(MonoBehaviour window) {
        if (activeWindow != window) return;

        activeWindow = null;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
