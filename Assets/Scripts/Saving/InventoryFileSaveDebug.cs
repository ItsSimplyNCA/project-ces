using System;
using System.IO;
using UnityEngine;

public class InventoryFileSaveDebug : MonoBehaviour {
    [Serializable] private class SaveFileData {
        public int version;
        public InventorySaveData inventory;
    }

    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private GameDefinitionCatalog definitionCatalog;
    [SerializeField] private PlayerUIState uiState;

    private string SavePath => Path.Combine(
        Application.persistentDataPath,
        "inventory-debug.json"
    );

    private bool CanRun() {
        if (!Application.isPlaying) return false;

        if (playerInventory == null || definitionCatalog == null || uiState == null) {
            Debug.LogError("Missing inventory file save references", this);
            return false;
        }

        if (uiState.IsOpen) {
            Debug.Log("Close the current UI before saving or loading", this);
            return false;
        }

        return definitionCatalog.IsValid;
    }

    [ContextMenu("Debug/Save Inventory To File")]
    private void SaveInventory() {
        if (!CanRun()) return;

        SaveFileData data = new SaveFileData {
            version = 1,
            inventory = playerInventory.CaptureSaveData()
        };

        string temporaryPath = SavePath + ".tmp";
        string backupPath = SavePath + ".bak";

        try {
            Directory.CreateDirectory(Application.persistentDataPath);

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(temporaryPath, json);

            if (File.Exists(SavePath)) {
                File.Replace(temporaryPath, SavePath, backupPath);
            } else {
                File.Move(temporaryPath, SavePath);
            }

            Debug.Log($"Inventory saved: {SavePath}", this);
        } catch(Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException) {
            Debug.LogError($"Inventory save failed: {exception.Message}", this);
        }
    }

    [ContextMenu("Debug/Load Inventory From File")]
    private void LoadInventory() {
        if (!CanRun()) return;

        if (!File.Exists(SavePath)) {
            Debug.LogWarning("Inventory save file not found", this);
            return;
        }

        try {
            string json = File.ReadAllText(SavePath);
            SaveFileData data = JsonUtility.FromJson<SaveFileData>(json);

            if (data == null || data.version != 1) {
                Debug.LogError("Unsupported inventory save format", this);
                return;
            }

            bool restored = playerInventory.TryRestoreSaveData(data.inventory, definitionCatalog);

            if (restored) {
                Debug.Log("Inventory loaded from file", this);
            }
        } catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException) {
            Debug.LogError($"Inventory load failed: {exception.Message}", this);
        }
    }
}
