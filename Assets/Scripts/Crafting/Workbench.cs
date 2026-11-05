using System.Collections.Generic;
using UnityEngine;

public class Workbench : MonoBehaviour{
    [SerializeField] private RecipeDefinition[] availableRecipes = System.Array.Empty<RecipeDefinition>();

    private RecipeDefinition activeRecipe;
    private PlayerInventory playerInventory;
    private List<InventorySlot> takenInputs;
    private float craftingElapsed;

    public IReadOnlyList<RecipeDefinition> AvailableRecipes => availableRecipes;
    public RecipeDefinition ActiveRecipe => activeRecipe;
    public bool IsCrafting => activeRecipe != null;
    public float CraftingProgress => activeRecipe != null
        ? Mathf.Clamp01(craftingElapsed / activeRecipe.Duration)
        : 0f;

    public bool HasContents {
        get {
            if (activeRecipe != null) return true;
            if (takenInputs == null) return false;

            foreach (InventorySlot slot in takenInputs) {
                if (!slot.IsEmpty) return true;
            }

            return false;
        }
    }

    public bool SupportsRecipe(RecipeDefinition recipe) {
        if (recipe == null) return false;
        if (recipe.FluidInputs.Count > 0 || recipe.FluidOutputs.Count > 0) return false;

        foreach (RecipeDefinition availableRecipe in availableRecipes) {
            if (availableRecipe == recipe) return true;
        }

        return false;
    }

    public bool TryStartCrafting(RecipeDefinition recipe, PlayerInventory inventory) {
        if (inventory == null || HasContents) return false;
        if (!SupportsRecipe(recipe)) return false;

        if (!inventory.HasRecipeInputs(recipe)) {
            Debug.Log("Missing ingredients", this);
            return false;
        }

        if (!inventory.CanFitRecipeOutputs(recipe)) {
            Debug.LogWarning("Inventory full", this);
            return false;
        }

        if (!inventory.TryTakeRecipeInputs(recipe, out var inputs)) {
            return false;
        }

        playerInventory = inventory;
        takenInputs = inputs;
        activeRecipe = recipe;
        craftingElapsed = 0f;

        return true;
    }

    private void Update() {
        if (Time.deltaTime <= 0f) return;
        if (playerInventory == null) return;

        if (activeRecipe == null) {
            if (takenInputs != null && playerInventory.ReturnRecipeInputs(takenInputs)) {
                ClearProcess();
            }

            return;
        }

        craftingElapsed = Mathf.Min(
            craftingElapsed + Time.deltaTime,
            activeRecipe.Duration
        );

        if (craftingElapsed < activeRecipe.Duration) return;
        if (!playerInventory.CanFitRecipeOutputs(activeRecipe)) return;

        foreach (ItemAmount output in activeRecipe.Outputs) {
            playerInventory.Add(output.Item, output.Amount);
        }

        Debug.Log($"Crafting completed: {activeRecipe.RecipeName}", this);

        ClearProcess();
    }

    public bool CancelCrafting() {
        if (!HasContents) return true;

        activeRecipe = null;
        craftingElapsed = 0f;

        Debug.Log("Crafting cancelled", this);

        if (playerInventory == null) return false;
        if (!playerInventory.ReturnRecipeInputs(takenInputs)) return false;

        ClearProcess();
        return true;
    }

    private void ClearProcess() {
        activeRecipe = null;
        playerInventory = null;
        takenInputs = null;
        craftingElapsed = 0f;
    }

    public WorkbenchSaveData CaptureSaveData() {
        WorkbenchSaveData data = new() {
            activeRecipeId = activeRecipe != null ? activeRecipe.RecipeId : string.Empty,
            craftingElapsed = this.craftingElapsed,
            takenInputs = new InventorySaveData {
                slotCount = takenInputs != null ? takenInputs.Count : 0
            }
        };

        if (takenInputs == null) return data;

        foreach (InventorySlot slot in takenInputs) {
            InventorySlotSaveData savedSlot = new();

            if (!slot.IsEmpty) {
                savedSlot.itemId = slot.Item.ItemId;
                savedSlot.amount = slot.Amount;

                FluidCellData cell = slot.FluidCell;

                if (cell != null) {
                    savedSlot.fluidId = cell.Fluid != null ? cell.Fluid.FluidId : string.Empty;
                    savedSlot.fluidAmount = cell.Amount;
                }
            }

            data.takenInputs.slots.Add(savedSlot);
        }

        return data;
    }

    public bool ValidateCraftingSaveData(WorkbenchSaveData data, GameDefinitionCatalog catalog) {
        bool Fail(string message) {
            Debug.LogWarning($"Workbench crafting validation failed: {message}", this);
            return false;
        }

        if (data == null) {
            return Fail("Missing workbench data.");
        }

        if (catalog == null || !catalog.IsValid) {
            return Fail("Invalid definition catalog.");
        }

        if (float.IsNaN(data.craftingElapsed) || float.IsInfinity(data.craftingElapsed) || data.craftingElapsed < 0f) {
            return Fail("Invalid crafting elapsed time.");
        }

        if (string.IsNullOrEmpty(data.activeRecipeId)) {
            if (data.craftingElapsed != 0f) {
                return Fail("Crafting progress exist without an active recipe.");
            }

            return true;
        }

        if (!catalog.TryGetRecipe(data.activeRecipeId, out RecipeDefinition recipe)) {
            return Fail($"Unknown active recipe: {data.activeRecipeId}");
        }

        if (!SupportsRecipe(recipe)) {
            return Fail($"Unsupported active recipe: {data.activeRecipeId}");
        }

        if (float.IsNaN(recipe.Duration) || float.IsInfinity(recipe.Duration) || recipe.Duration <= 0f) {
            return Fail("Active recipe has an invalid duration.");
        }

        if (data.craftingElapsed > recipe.Duration) {
            return Fail("Crafting progress exceeds the recipe duration.");
        }

        return true;
    }

    public bool ValidateTakenInputsSaveData(WorkbenchSaveData data, GameDefinitionCatalog catalog) {
        bool Fail(string message) {
            Debug.LogWarning($"Workbench input validation failed: {message}", this);
            return false;
        }

        if (data == null || data.takenInputs == null || data.takenInputs.slots == null) {
            return Fail("Missing taken input data.");
        }

        if (catalog == null || !catalog.IsValid) {
            return Fail("Invalid definition catalog.");
        }

        InventorySaveData inventory = data.takenInputs;

        if (inventory.slotCount < 0 || inventory.slotCount != inventory.slots.Count) {
            return Fail("Invalid taken input slot count.");
        }

        float cellCapacity = new FluidCellData().Capacity;

        for (int i = 0; i < inventory.slots.Count; i++) {
            InventorySlotSaveData slot = inventory.slots[i];

            if (slot == null) {
                return Fail($"Missing slot data at index {i}.");
            }

            if (float.IsNaN(slot.fluidAmount) || float.IsInfinity(slot.fluidAmount) || slot.fluidAmount < 0f) {
                return Fail($"Invalid fluid amount at index {i}.");
            }

            bool hasFluidId = !string.IsNullOrEmpty(slot.fluidId);

            if (string.IsNullOrEmpty(slot.itemId)) {
                if (slot.amount != 0 || hasFluidId || slot.fluidAmount != 0f) {
                    return Fail($"Invalid empty slot at index {i}.");
                }

                continue;
            }

            if (!catalog.TryGetItem(slot.itemId, out ItemDefinition item)) {
                return Fail($"Unknown item ID at index {i}: {slot.itemId}");
            }

            if (slot.amount <= 0 || slot.amount > item.MaxStackSize) {
                return Fail($"Invalid item amount at index {i}.");
            }

            if (!item.IsFluidCell) {
                if (hasFluidId || slot.fluidAmount != 0f) {
                    return Fail($"Unexpected fluid data on a regular item at index {i}.");
                }

                continue;
            }

            if (!hasFluidId) {
                if (slot.fluidAmount != 0f) {
                    return Fail($"Cell contents exist without a fluid ID at index {i}.");
                }

                continue;
            }

            if (!catalog.TryGetFluid(slot.fluidId, out _)) {
                return Fail($"Unknown fluid ID at index {i}: {slot.fluidId}");
            }

            if (slot.fluidAmount <= 0f || slot.fluidAmount > cellCapacity) {
                return Fail($"Invalid cell contents at index {i}.");
            }
        }

        return true;
    }

    public bool ValidateSaveData(WorkbenchSaveData data, GameDefinitionCatalog catalog) {
        bool Fail(string message) {
            Debug.LogWarning($"Workbench validation failed: {message}", this);
            return false;
        }

        if (!ValidateCraftingSaveData(data, catalog) || !ValidateTakenInputsSaveData(data, catalog)) {
            return false;
        }

        if (string.IsNullOrEmpty(data.activeRecipeId)) {
            return true;
        }

        catalog.TryGetRecipe(data.activeRecipeId, out RecipeDefinition recipe);

        if (recipe.Inputs == null) {
            return Fail("Active recipe has no input list.");
        }

        Dictionary<ItemDefinition, long> remainingRequirements = new();

        foreach (ItemAmount input in recipe.Inputs) {
            if (input == null || input.Item == null || input.Amount <= 0) {
                return Fail("Active recipe contains an invalid input.");
            }

            remainingRequirements.TryGetValue(input.Item, out long requiredAmount);
            remainingRequirements[input.Item] = requiredAmount + input.Amount;
        }

        foreach (InventorySlotSaveData slot in data.takenInputs.slots) {
            if (string.IsNullOrEmpty(slot.itemId)) continue;

            catalog.TryGetItem(slot.itemId, out ItemDefinition item);

            if (!remainingRequirements.TryGetValue(item, out long remainingAmount)) {
                return Fail($"Unexpected taken input: {slot.itemId}");
            }

            if (slot.amount > remainingAmount) {
                return Fail($"Too many taken inputs: {slot.itemId}");
            }

            remainingAmount -= slot.amount;

            if (remainingAmount == 0) {
                remainingRequirements.Remove(item);
            } else {
                remainingRequirements[item] = remainingAmount;
            }
        }

        if (remainingRequirements.Count != 0) {
            return Fail("Taken inputs do not cover the active recipe.");
        }

        return true;
    }

    public bool TryRestoreSaveData(WorkbenchSaveData data, GameDefinitionCatalog catalog, PlayerInventory inventory) {
        if (!Application.isPlaying) {
            Debug.LogWarning("Workbench state can only be restored in Play mode.", this);
            return false;
        }

        if (inventory == null) {
            Debug.LogWarning("Workbench restore failed: missing player inventory.", this);
            return false;
        }

        if (!ValidateSaveData(data, catalog)) return false;

        List<InventorySlot> restoredInputs = new(data.takenInputs.slots.Count);

        foreach (InventorySlotSaveData savedSlot in data.takenInputs.slots) {
            InventorySlot restoredSlot = new();

            if (!string.IsNullOrEmpty(savedSlot.itemId)) {
                catalog.TryGetItem(savedSlot.itemId, out ItemDefinition item);

                restoredSlot.Add(item, savedSlot.amount);

                if (item.IsFluidCell && !string.IsNullOrEmpty(savedSlot.fluidId)) {
                    catalog.TryGetFluid(savedSlot.fluidId, out FluidDefinition fluid);

                    restoredSlot.FluidCell.Add(fluid, savedSlot.fluidAmount);
                }
            }

            restoredInputs.Add(restoredSlot);
        }

        RecipeDefinition restoredRecipe = null;

        if (!string.IsNullOrEmpty(data.activeRecipeId)) {
            catalog.TryGetRecipe(data.activeRecipeId, out restoredRecipe);
        }

        takenInputs = restoredInputs;
        activeRecipe = restoredRecipe;
        craftingElapsed = data.craftingElapsed;
        playerInventory = inventory;

        return true;
    }
}
