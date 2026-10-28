using UnityEngine;

public class PlayerUIState : MonoBehaviour {
    private MonoBehaviour activeWindow;
    private int lastClosedFrame = -1;

    public bool IsOpen => activeWindow != null;
    public bool ClosedThisFrame => lastClosedFrame == Time.frameCount;

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
        lastClosedFrame = Time.frameCount;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
