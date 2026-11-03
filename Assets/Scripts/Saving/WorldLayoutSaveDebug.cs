using UnityEngine;

public class WorldLayoutSaveDebug : MonoBehaviour {
    [SerializeField] private GridWorld gridWorld;
    [SerializeField] private GameDefinitionCatalog definitionCatalog;

    private string capturedJson;

    private bool CanRun() {
        if (!Application.isPlaying) return false;

        if (gridWorld == null | definitionCatalog == null) {
            Debug.LogError("Missing world layout debug references", this);
            return false;
        }

        return definitionCatalog.IsValid;
    }

    private bool TryReadSnapshot(out WorldSaveData data) {
        data = null;

        if (!CanRun()) return false;

        if (string.IsNullOrEmpty(capturedJson)) {
            Debug.LogWarning("No world layout snapshot captured", this);
            return false;
        }

        try {
            data = JsonUtility.FromJson<WorldSaveData>(capturedJson);
        } catch (System.ArgumentException exception) {
            Debug.LogError($"Invalid world layout JSON: {exception.Message}", this);
            return false;
        }

        if (data == null || data.buildings == null) {
            Debug.LogWarning("Missing world layout data", this);
            return false;
        }

        return true;
    }

    [ContextMenu("Debug/Capture World Layout Snapshot")]
    private void CaptureSnapshot() {
        if (!CanRun()) return;

        WorldSaveData data = gridWorld.CaptureWorldLayout();

        if (!gridWorld.ValidateWorldLayoutData(data, definitionCatalog)) return;

        capturedJson = JsonUtility.ToJson(data, true);

        Debug.Log($"World layout snapshot captured: {data.buildings.Count} buildings", this);
    }

    [ContextMenu("Debug/Validate World Layout Snapshot")]
    private void ValidateSnapshot() {
        if (!TryReadSnapshot(out WorldSaveData data)) return;
        
        if (gridWorld.ValidateWorldLayoutData(data, definitionCatalog)) {
            Debug.Log("World layout snapshot is valid", this);
        }
    }

    [ContextMenu("Debug/Test Overlapping Building Rejection")]
    private void TestOverlappingBuildingRejection() {
        if (!TryReadSnapshot(out WorldSaveData data)) return;

        if (data.buildings.Count == 0) {
            Debug.LogWarning("Capture a layout containing at least one building", this);
            return;
        }

        BuildingSaveData first = data.buildings[0];

        data.buildings.Add(new BuildingSaveData {
            buildingId = first.buildingId,
            origin = first.origin,
            quarterTurns = first.quarterTurns
        });

        bool accepted = gridWorld.ValidateWorldLayoutData(data, definitionCatalog);

        if (accepted) {
            Debug.LogError("Overlapping building was incorrectly accepted", this);
        } else {
            Debug.Log("Overlapping building correctly rejected", this);
        }
    }
}
