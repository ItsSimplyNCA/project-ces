using System;
using System.Collections.Generic;
using UnityEngine;

public class PortSaveDebug : MonoBehaviour {
    [SerializeField] private UniversalIO universalIO;
    [SerializeField] private PlayerUIState uiState;

    private string capturedJson;

    [Serializable] private class Snapshot {
        public List<UniversalPortSaveData> ports = new();
    }

    private bool CanRun() {
        if (!Application.isPlaying) return false;

        if (universalIO == null || uiState == null) {
            Debug.LogError("Missing port save debug references.", this);
            return false;
        }
        
        if (uiState.IsOpen) {
            Debug.Log("Close the current UI before testing save/load.", this);
            return false;
        }

        return true;
    }

    //DEBUG METHODS
    [ContextMenu("Debug/Capture Port Snapshot")]
    private void CaptureSnapshot() {
        if (!CanRun()) return;

        Snapshot data = new();

        foreach (UniversalPort port in universalIO.Ports) {
            data.ports.Add(new UniversalPortSaveData {
                localCell = port.LocalCell,
                face = port.Face,
                mode = port.Mode,
                connectionEnabled = port.ConnectionEnabled,
                outputSlotCursor = port.OutputSlotCursor,
                routingMode = port.RoutingMode,
                roundRobinDestinationCursor = port.RoundRobinDestinationCursor
            });
        }

        if (!universalIO.ValidateSaveData(data.ports)) return;

        capturedJson = JsonUtility.ToJson(data, true);

        Debug.Log("Port snapshot captured.", this);
    }

    [ContextMenu("Debug/Restore Port Snapshot")]
    private void RestoreSnapshot() {
        if (!CanRun()) return;

        if (string.IsNullOrEmpty(capturedJson)) {
            Debug.LogWarning("No port snapshot captured.", this);
            return;
        }

        Snapshot data;

        try {
            data = JsonUtility.FromJson<Snapshot>(capturedJson);
        } catch (ArgumentException exception) {
            Debug.LogError($"Invalid port JSON: {exception.Message}", this);
            return;
        }

        if (data == null) {
            Debug.LogWarning("Missing port snapshot data.", this);
            return;
        }

        if (universalIO.TryRestoreSaveData(data.ports)) {
            Debug.Log("Port snapshot restored.", this);
        }
    }
}
