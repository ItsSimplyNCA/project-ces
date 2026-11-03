using System;
using System.IO;
using UnityEngine;

public class PlayerFileSaveDebug : MonoBehaviour {
    [SerializeField] private PlayerSaveHandler playerSaveHandler;
    [SerializeField] private GameDefinitionCatalog definitionCatalog;
    [SerializeField] private PlayerUIState uiState;

    private string SavePath => Path.Combine(Application.persistentDataPath, "player-debug.json");

    private bool CanRun() {
        if (!Application.isPlaying) return false;

        if (playerSaveHandler == null || definitionCatalog == null || uiState == null) {
            Debug.LogError("Missing player save debug references", this);
            return false;
        }

        if (uiState.IsOpen) {
            Debug.Log("Close the curretn UI before saving or loading", this);
            return false;
        }

        return definitionCatalog.IsValid;
    }

    [ContextMenu("Debug/Save Player To File")]
    private void SavePlayer() {
        if (!CanRun()) return;

        GameSaveData data = new GameSaveData {
            version = 1,
            player = playerSaveHandler.CaptureSaveData()
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

            Debug.Log($"Player saved: {SavePath}", this);
        } catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException) {
            Debug.LogError($"Player save failed: {exception.Message}", this);
        }
    }

    [ContextMenu("Debug/Load Player From File")]
    private void LoadPlayer() {
        if (!CanRun()) return;

        if (!File.Exists(SavePath)) {
            Debug.LogWarning("Player save file not found", this);
            return;
        }

        try {
            string json = File.ReadAllText(SavePath);
            GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);

            if (data == null || data.version != 1) {
                Debug.LogError("Unsupported player save format", this);
                return;
            }

            bool restored = playerSaveHandler.TryRestoreSaveData(data.player, definitionCatalog);

            if (restored) {
                Debug.Log("Player loaded from file", this);
            }
        } catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException) {
            Debug.LogError($"Player load failed: {exception.Message}", this);
        }
    }
}
