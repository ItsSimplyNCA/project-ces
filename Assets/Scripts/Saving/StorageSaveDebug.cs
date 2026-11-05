using UnityEngine;

public class StorageSaveDebug : MonoBehaviour {
    [SerializeField] private Silo silo;
    [SerializeField] private Tank tank;
    [SerializeField] private GameDefinitionCatalog definitionCatalog;
    [SerializeField] private PlayerUIState uiState;

    private string siloJson;
    private string tankJson;

    private bool CanRun() {
        if (!Application.isPlaying) return false;

        if (silo == null || tank == null || definitionCatalog == null || uiState == null) {
            Debug.LogError("Missing storage save debug references.", this);
            return false;
        }

        if (uiState.IsOpen) {
            Debug.Log("Close the current UI before testing save/laod.", this);
            return false;
        }

        return definitionCatalog.IsValid;
    }

    // DEBUG METHODS
    [ContextMenu("Debug/Capture Storage Snapshot")]
    private void CaptureSnapshot() {
        if (!CanRun()) return;

        SiloSaveData siloData = silo.CaptureSaveData();
        TankSaveData tankData = tank.CaptureSaveData();

        if (!silo.ValidateSaveData(siloData, definitionCatalog) || !tank.ValidateSaveData(tankData, definitionCatalog)) {
            return;
        }

        siloJson = JsonUtility.ToJson(siloData, true);
        tankJson = JsonUtility.ToJson(tankData, true);

        Debug.Log("Storage snapshot captured.", this);
    }

    [ContextMenu("Debug/Restore Storage Snapshot")]
    private void RestoreSnapshot() {
        if (!CanRun()) return;

        if (string.IsNullOrEmpty(siloJson) || string.IsNullOrEmpty(tankJson)) {
            Debug.LogWarning("No storage snapshot captured.", this);
            return;
        }

        SiloSaveData siloData;
        TankSaveData tankData;

        try {
            siloData = JsonUtility.FromJson<SiloSaveData>(siloJson);
            tankData = JsonUtility.FromJson<TankSaveData>(tankJson);
        } catch (System.ArgumentException exception) {
            Debug.LogError($"Invalid storage JSON: {exception.Message}", this);
            return;
        }

        if (!silo.ValidateSaveData(siloData, definitionCatalog) || !tank.ValidateSaveData(tankData, definitionCatalog)) return;
        if (!silo.TryRestoreSaveData(siloData, definitionCatalog)) return;
        if (!tank.TryRestoreSaveData(tankData, definitionCatalog)) return;

        Debug.Log("Storage snapshot restored.", this);
    }
}
