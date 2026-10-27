using TMPro;
using UnityEngine;

public class MachineHUD : MonoBehaviour {
    [SerializeField] private Camera playerCamera;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private float interactionDistance = 6f;
    [SerializeField] private PlayerUIState uiState;

    private void Update() {
        if (uiState.IsOpen) {
            SetText(string.Empty);
            return;
        }
        
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        bool hasHit = Physics.Raycast(
            ray,
            out RaycastHit hit,
            interactionDistance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore
        );

        Machine machine = hasHit ? hit.collider.GetComponentInParent<Machine>() : null;

        if (machine == null) {
            SetText(string.Empty);
            return;
        }

        string selectedRecipeName = machine.SelectedRecipe != null
            ? machine.SelectedRecipe.RecipeName
            : "None";
        
        string text = $"Selected recipe: {selectedRecipeName}\n";

        if (!machine.IsOn) {
            text += "Status: OFF";
        } else if (machine.ActiveRecipe != null) {
            int progress = Mathf.RoundToInt(machine.CraftingProgress * 100f);

            text += $"Crafting: {machine.ActiveRecipe.RecipeName}\nProgress:{progress}%";
        } else {
            text += machine.SelectedRecipe == null
                ? "Status: No recipe selected"
                : "Status: Waiting for inputs or output space";
        }

        text += "\n\nInputs:";

        for (int i = 0; i < machine.InputSlotCount; i++) {
            ItemDefinition item = machine.GetInputSlotItem(i);

            text += item != null
                ? $"\n{i + 1}. {item.ItemName} × {machine.GetInputSlotAmount(i)}"
                : $"\n{i + 1}. Empty";
        }

        text += "\n\nOutputs:";

        for (int i = 0; i < machine.OutputSlotCount; i++) {
            ItemDefinition item = machine.GetOutputSlotItem(i);

            text += item != null
                ? $"\n{i + 1}. {item.ItemName} × {machine.GetOutputSlotAmount(i)}"
                : $"\n{i + 1}. Empty";
        }

        text += "\n\n[G] Insert recipe inputs\n\n[F] Collect outputs";

        SetText(text);
    }

    private void SetText(string text) {
        if (statusText.text != text) {
            statusText.text = text;
        }
    }
}
