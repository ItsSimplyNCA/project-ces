using UnityEngine;

public class MachineSaveDebug : MonoBehaviour {
    [SerializeField] private Machine machine;
    [SerializeField] private GameDefinitionCatalog definitionCatalog;
    [SerializeField] private PlayerUIState uiState;

    private string capturedJson;

    private bool CanRun() {
        if (!Application.isPlaying) return false;

        if (machine == null || definitionCatalog == null || uiState == null) {
            Debug.LogError("Missing machine save debug references.", this);
            return false;
        }

        if (uiState.IsOpen) {
            Debug.Log("Close the current UI before testing save/load", this);
            return false;
        }

        return definitionCatalog.IsValid;
    }

    // DEBUG METHODS
    [ContextMenu("Debug/Capture Machine Snapshot")]
    private void CaptureSnapshot() {
        if (!CanRun()) return;

        MachineSaveData data = machine.CaptureSaveData();

        if (!machine.ValidateSaveData(data, definitionCatalog)) return;

        capturedJson = JsonUtility.ToJson(data, true);

        Debug.Log($"Machine snapshot captured. Progress: {machine.CraftingProgress:P1}", this);
    }

    [ContextMenu("Debug/Restore Machine Snapshot")]
    private void RestoreSnapshot() {
        if (!CanRun()) return;

        if (string.IsNullOrEmpty(capturedJson)) {
            Debug.LogWarning("No machine snapshot captured.", this);
            return;
        }

        MachineSaveData data;

        try {
            data = JsonUtility.FromJson<MachineSaveData>(capturedJson);
        } catch (System.ArgumentException exception) {
            Debug.LogError($"Invalid machine JSON: {exception.Message}", this);
            return;
        }

        if (machine.TryRestoreSaveData(data, definitionCatalog)) {
            Debug.Log($"Machine snapshot restored. Progress: {machine.CraftingProgress:P1}", this);
        }
    }
}
