using System;
using UnityEngine;

public class PowerSaveDebug : MonoBehaviour {
    [SerializeField] private PowerGenerator generator;
    [SerializeField] private PowerBreaker breaker;
    [SerializeField] private PowerStorage storage;
    [SerializeField] private PlayerUIState uiState;

    private string capturedJson;

    [Serializable] private class Snapshot {
        public PowerGeneratorSaveData generator;
        public PowerBreakerSaveData breaker;
        public PowerStorageSaveData storage;
    }

    private bool CanRun() {
        if (!Application.isPlaying) return false;

        if (generator == null || breaker == null || storage == null || uiState == null) {
            Debug.LogError("Missing power save debug references.", this);
            return false;
        }

        if (uiState.IsOpen) {
            Debug.Log("Close the current UI before testing save/load.", this);
            return false;
        }

        return true;
    }

    private bool ValidateSnapshot(Snapshot data) {
        if (data == null) {
            Debug.LogWarning("Missing power snapshot data.", this);
            return false;
        }

        return generator.ValidateSaveData(data.generator)
            && breaker.ValidateSaveData(data.breaker)
            && storage.ValidateSaveData(data.storage);
    }

    // DEBUG METHODS
    [ContextMenu("Debug/Capture Power Snapshot")]
    private void CaptureSnapshot() {
        if (!CanRun()) return;

        Snapshot data = new() {
            generator = generator.CaptureSaveData(),
            breaker = breaker.CaptureSaveData(),
            storage = storage.CaptureSaveData()
        };

        if (!ValidateSnapshot(data)) return;

        capturedJson = JsonUtility.ToJson(data, true);

        Debug.Log("Power snapshot captured.", this);
    }

    [ContextMenu("Debug/Restore Power Snapshot")]
    private void RestoreSnapshot() {
        if (!CanRun()) return;

        if (string.IsNullOrEmpty(capturedJson)) {
            Debug.LogWarning("No power snapshot captured.", this);
            return;
        }

        Snapshot data;

        try {
            data = JsonUtility.FromJson<Snapshot>(capturedJson);
        } catch (ArgumentException exception) {
            Debug.LogError($"Invalid power JSON: {exception.Message}", this);
            return;
        }

        if (!ValidateSnapshot(data)) return;

        if (!generator.TryRestoreSaveData(data.generator)) return;
        if (!breaker.TryRestoreSaveData(data.breaker)) return;
        if (!storage.TryRestoreSaveData(data.storage)) return;

        Debug.Log("Power snapshot restored.", this);
    }
}
