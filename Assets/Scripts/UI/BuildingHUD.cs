using TMPro;
using UnityEngine;

public class BuildingHUD : MonoBehaviour {
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private BuildingPreviewController buildingPreviewController;
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private PipeConfiguration pipeConfiguration;

    private string previousText;

    private void Update() {
        BuildingDefinition buildingDefinition = buildingPreviewController.SelectedBuilding;
        
        bool deconstructMode = buildingPreviewController.IsDeconstructMode;
        int amount = playerInventory.GetAmount(buildingDefinition.Item);

        string modeText = buildingPreviewController.IsConfigurationMode
            ? "Configuration"
            : deconstructMode ? "Deconstruct" : "Build";

        string text = 
            $"Mode: {modeText} [X: Deconstruct / C: Configure]\n" +
            $"{buildingDefinition.Item.ItemName}: {amount}";

        if (deconstructMode) {
            int progress = Mathf.RoundToInt(
                buildingPreviewController.DeconstructProgress * 100f
            );

            text += $"\nDeconstructing: {progress}%";
        } else if (!buildingPreviewController.IsConfigurationMode) {
            PipeSegment selectedPipe = pipeConfiguration.SelectedPipe;

            if (selectedPipe == null) {
                text += "\nClick a pipe to select it";
            } else {
                text +=
                    $"\nSelected pipe: {selectedPipe.GridCell}" +
                    $"\nConnections: {selectedPipe.Connections}" +
                    $"\nClick an adjacent pipe to connect/disconnect";
            }
        } else {
            text += "\nRotate: [R]";
        }

        if (text != previousText) {
            statusText.text = text;
            previousText = text;
        }
    }
}