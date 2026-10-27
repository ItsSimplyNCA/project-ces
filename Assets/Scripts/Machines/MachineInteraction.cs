using UnityEngine;
using UnityEngine.InputSystem;

public class MachineInteraction : MonoBehaviour {
    [SerializeField] private Camera playerCamera;
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private PlayerUIState uiState;
    [SerializeField] private float interactionDistance = 6f;

    private void Update() {
        if (uiState.IsOpen || Keyboard.current == null) return;

        bool collectOutputs = Keyboard.current.fKey.wasPressedThisFrame;
        bool insertInputs = Keyboard.current.gKey.wasPressedThisFrame;

        if (!collectOutputs && !insertInputs) return;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        bool hasHit = Physics.Raycast(
            ray,
            out RaycastHit hit,
            interactionDistance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore
        );

        if (!hasHit) return;

        Machine machine = hit.collider.GetComponentInParent<Machine>();

        if (machine == null) return;

        if (collectOutputs) CollectOutputs(machine);
        else InsertRecipeInputs(machine);
    }

    public void CollectOutputs(Machine machine) {
        if (machine == null) return;

        playerInventory.CollectFrom(machine);

        for (int i = 0; i < machine.OutputSlotCount; i++) {
            if (machine.GetOutputSlotAmount(i) > 0) {
                Debug.LogWarning("Inventory full");
                return;
            }
        }
    }

    public void CollectInputs(Machine machine) {
        if (machine == null) return;

        bool inventoryFull = false;

        for (int i = 0; i < machine.InputSlotCount; i++) {
            ItemDefinition item = machine.GetInputSlotItem(i);
            int availableAmount = machine.GetInputSlotAmount(i);

            if (item == null || availableAmount <= 0) continue;

            int addedAmount;

            if (item.IsFluidCell) {
                FluidCellData fluidCell = machine.GetInputSlotFluidCell(i);

                addedAmount = playerInventory.TryAddFluidCell(item, fluidCell) ? 1 : 0;
            } else {
                addedAmount = playerInventory.Add(item, availableAmount);
            }

            machine.RemoveInputFromSlot(i, addedAmount);

            if (addedAmount < availableAmount) {
                inventoryFull = true;
            }
        }

        if (inventoryFull) {
            Debug.LogWarning("Inventory full");
        }
    }

    public void InsertRecipeInputs(Machine machine) {
        if (machine == null) return;

        RecipeDefinition recipe = machine.SelectedRecipe;

        if (recipe == null) {
            Debug.Log("No recipe selected");
            return;
        }

        foreach (ItemAmount input in recipe.Inputs) {
            playerInventory.TransferTo(machine, input.Item, input.Amount);
        }
    }
}