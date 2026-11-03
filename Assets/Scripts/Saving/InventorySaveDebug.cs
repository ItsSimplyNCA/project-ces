using UnityEngine;

public class InventorySaveDebug : MonoBehaviour {
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private GameDefinitionCatalog definitionCatalog;
    [SerializeField] private PlayerUIState uiState;

    private string capturedJson;

    private bool CanRun() {
        if (!Application.isPlaying) return false;

        if (playerInventory == null || definitionCatalog == null || uiState == null) {
            Debug.LogError("Missing invetory save debug references", this);
            return false;
        }

        if (uiState.IsOpen) {
            Debug.Log("Close the current UI before testing save/load", this);
            return false;
        }

        return true;
    }

    [ContextMenu("Debug/Capture Inventory Snapshot")]
    private void CaptureSnapshot() {
        if (!CanRun()) return;
        if (!definitionCatalog.IsValid) return;

        InventorySaveData data = playerInventory.CaptureSaveData();
        capturedJson = JsonUtility.ToJson(data, true);

        Debug.Log("Inventory snapshot captured", this);
    }

    [ContextMenu("Debug/Restore Inventory Snapshot")]
    private void RestoreSnapshot() {
        if (!CanRun()) return;

        if (string.IsNullOrEmpty(capturedJson)) {
            Debug.LogWarning("No invetory snapshot captured", this);
            return;
        }

        InventorySaveData data;

        try {
            data = JsonUtility.FromJson<InventorySaveData>(capturedJson);
        } catch (System.ArgumentException exception) {
            Debug.LogError($"Invalid inventory JSON: {exception.Message}", this);
            return;
        }

        bool restored = playerInventory.TryRestoreSaveData(data, definitionCatalog);

        if (restored) {
            Debug.Log("Inventory snapshot restored", this);
        }
    }
}
