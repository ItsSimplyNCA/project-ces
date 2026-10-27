using System.Collections.Generic;
using UnityEngine;

public class Machine : MonoBehaviour, IItemTransferEndpoint, IFluidCellTransferEndpoint, IFluidTransferEndpoint, IDeconstructionContents {
    [SerializeField] private RecipeDefinition[] availableRecipes;
    [SerializeField, Min(1)] private int inputSlotCount = 4;
    [SerializeField, Min(1)] private int outputSlotCount = 4;
    [SerializeField] private bool isOn = true;
    [SerializeField, Min(1)] private int tier = 1;
    [SerializeField, Min(1f)] private float speedMultiplierPerTier = 2f;
    [SerializeField, Min(0)] private int fluidInputSlotCount = 2;
    [SerializeField, Min(0)] private int fluidOutputSlotCount = 2;
    [SerializeField, Min(0.001f)] private float fluidSlotCapacity = 10f;

    private RecipeDefinition selectedRecipe;
    private InventorySlot[] inputSlots;
    private InventorySlot[] outputSlots;
    private RecipeDefinition activeRecipe;
    private float craftingElapsed;
    private FluidSlot[] fluidInputSlots;
    private FluidSlot[] fluidOutputSlots;

    public IReadOnlyList<RecipeDefinition> AvailableRecipes => availableRecipes;
    public RecipeDefinition SelectedRecipe => selectedRecipe;
    public bool IsOn => isOn;
    public float CraftingProgress => activeRecipe != null ? Mathf.Clamp01(craftingElapsed / activeRecipe.Duration) : 0f;
    public RecipeDefinition ActiveRecipe => activeRecipe;
    public int InputSlotCount => inputSlotCount;
    public int OutputSlotCount => outputSlotCount;
    public int FluidInputSlotCount => fluidInputSlots.Length;
    public int FluidOutputSlotCount => fluidOutputSlots.Length;
    public int Tier => tier;
    public bool HasContents =>
        activeRecipe != null ||
        ContainsItems(inputSlots) ||
        ContainsItems(outputSlots) ||
        ContainsFluids(fluidInputSlots) ||
        ContainsFluids(fluidOutputSlots);

    public float SuppliedPowerMW { get; private set; }

    public float RequestedPowerMW {
        get {
            if (!isOn) return 0f;
            if (activeRecipe != null) return activeRecipe.PowerDemandMW;
            if (!SupportsRecipe(selectedRecipe)) return 0f;
            if (!HasRequiredInputs(selectedRecipe)) return 0f;
            if (!HasRequiredFluidInputs(selectedRecipe)) return 0f;
            if (!CanFitOutputs(selectedRecipe)) return 0f;
            if (!CanFitFluidOutputs(selectedRecipe)) return 0f;
            return selectedRecipe.PowerDemandMW;
        }
    }

    public float PowerRatio {
        get {
            if (!isOn) return 0f;
            RecipeDefinition recipe = activeRecipe != null ? activeRecipe : selectedRecipe;
            if (recipe == null) return 0f;
            if (recipe.PowerDemandMW <= 0f) return 1f;
            return Mathf.Clamp01(SuppliedPowerMW / recipe.PowerDemandMW);
        }
    }

    public void Awake() {
        inputSlots = CreateSlots(inputSlotCount);
        outputSlots = CreateSlots(outputSlotCount);

        fluidInputSlots = CreateFluidSlots(
            fluidInputSlotCount,
            fluidSlotCapacity
        );

        fluidOutputSlots = CreateFluidSlots(
            fluidOutputSlotCount,
            fluidSlotCapacity
        );
    }

    public void Update() {
        if (!isOn) return;
        if (activeRecipe == null && !TryStartCrafting()) return;

        craftingElapsed += Time.deltaTime * GetRecipeSpeedMultiplier(activeRecipe) * PowerRatio;

        if (craftingElapsed >= activeRecipe.Duration) {
            CompleteCrafting();
        }
    }

    private static InventorySlot[] CreateSlots(int count) {
        InventorySlot[] slots = new InventorySlot[count];

        for (int i = 0; i < slots.Length; i++) {
            slots[i] = new InventorySlot();
        }

        return slots;
    }

    private static FluidSlot[] CreateFluidSlots(int count, float capacity) {
        FluidSlot[] slots = new FluidSlot[count];

        for (int i = 0; i < slots.Length; i++) {
            slots[i] = new FluidSlot(capacity);
        }

        return slots;
    }

    public int AddInput(ItemDefinition item, int requestedAmount) {
        if (requestedAmount <= 0 || !AcceptsInputItem(item)) return 0;

        return AddToSlots(
            inputSlots,
            item,
            requestedAmount
        );
    }

    public float AddFluidInput(FluidDefinition fluid, float requestedAmount) {
        if (requestedAmount <= 0f || !AcceptsInputFluid(fluid)) return 0f;

        return AddFluidToSlots(
            fluidInputSlots,
            fluid,
            requestedAmount
        );
    }

    public bool TrySelectRecipe(RecipeDefinition recipe) {
        if (recipe == null) {
            selectedRecipe = null;
            return true;
        }

        if (!SupportsRecipe(recipe)) return false;

        selectedRecipe = recipe;
        return true;
    }

    private bool HasRequiredInputs(RecipeDefinition recipe) {
        Dictionary<ItemDefinition, int> requiredAmounts = new();

        foreach (ItemAmount input in recipe.Inputs) {
            requiredAmounts.TryGetValue(input.Item, out int amount);
            requiredAmounts[input.Item] = amount + input.Amount;
        }

        foreach (KeyValuePair<ItemDefinition, int> required in requiredAmounts) {
            int availableAmount = 0;

            foreach (InventorySlot slot in inputSlots) {
                if (slot.Item == required.Key) {
                    availableAmount += slot.Amount;
                }
            }

            if (availableAmount < required.Value) return false;
        }

        return true;
    }

    private bool HasRequiredFluidInputs(RecipeDefinition recipe) {
        Dictionary<FluidDefinition, float> requiredAmounts = new();

        foreach (FluidAmount input in recipe.FluidInputs) {
            requiredAmounts.TryGetValue(input.Fluid, out float amount);
            requiredAmounts[input.Fluid] = amount + input.Amount;
        }

        foreach (KeyValuePair<FluidDefinition, float> required in requiredAmounts) {
            float availableAmount = 0f;

            foreach (FluidSlot slot in fluidInputSlots) {
                if (slot.Fluid == required.Key) {
                    availableAmount += slot.Amount;
                }
            }

            if (availableAmount < required.Value) return false;
        }

        return true;
    }

    private bool CanFitOutputs(RecipeDefinition recipe) {
        InventorySlot[] simulatedSlots = CreateSlots(outputSlots.Length);

        for (int i = 0; i < outputSlots.Length; i++) {
            InventorySlot slot = outputSlots[i];

            if (!slot.IsEmpty) {
                simulatedSlots[i].Add(slot.Item, slot.Amount);
            }
        }

        foreach (ItemAmount output in recipe.Outputs) {
            int addedAmount = AddToSlots(
                simulatedSlots,
                output.Item,
                output.Amount
            );

            if (addedAmount != output.Amount) return false;
        }

        return true;
    }

    private bool CanFitFluidOutputs(RecipeDefinition recipe) {
        FluidSlot[] simulatedSlots = new FluidSlot[fluidOutputSlots.Length];

        for (int i = 0; i < fluidOutputSlots.Length; i++) {
            FluidSlot slot = fluidOutputSlots[i];

            simulatedSlots[i] = new FluidSlot(slot.Capacity);

            if (!slot.IsEmpty) {
                simulatedSlots[i].Add(slot.Fluid, slot.Amount);
            }
        }

        foreach (FluidAmount output in recipe.FluidOutputs) {
            float addedAmount = AddFluidToSlots(
                simulatedSlots,
                output.Fluid,
                output.Amount
            );

            if (addedAmount < output.Amount) return false;
        }

        return true;
    }

    private static int AddToSlots(InventorySlot[] slots, ItemDefinition item, int requestedAmount) {
        int remainingAmount = requestedAmount;

        foreach (InventorySlot slot in slots) {
            if (slot.Item != item) continue;

            remainingAmount -= slot.Add(item, remainingAmount);

            if (remainingAmount == 0) return requestedAmount;
        }

        foreach (InventorySlot slot in slots) {
            if (!slot.IsEmpty) continue;

            remainingAmount -= slot.Add(item, remainingAmount);

            if (remainingAmount == 0) return requestedAmount;
        }

        return requestedAmount - remainingAmount;
    }

    private static float AddFluidToSlots(FluidSlot[] slots, FluidDefinition fluid, float requestedAmount) {
        if (fluid == null || requestedAmount <= 0f) return 0f;

        float remainingAmount = requestedAmount;

        foreach (FluidSlot slot in slots) {
            if (slot.Fluid != fluid) continue;

            remainingAmount -= slot.Add(fluid, remainingAmount);

            if (remainingAmount <= 0f) return requestedAmount;
        }

        foreach (FluidSlot slot in slots) {
            if (!slot.IsEmpty) continue;

            remainingAmount -= slot.Add(fluid, remainingAmount);

            if (remainingAmount <= 0f) return requestedAmount;
        }

        return requestedAmount - remainingAmount;
    }

    private bool TryStartCrafting() {
        if (!SupportsRecipe(selectedRecipe)) return false;
        if (!HasRequiredInputs(selectedRecipe)) return false;
        if (!HasRequiredFluidInputs(selectedRecipe)) return false;
        if (!CanFitOutputs(selectedRecipe)) return false;
        if (!CanFitFluidOutputs(selectedRecipe)) return false;
        if (selectedRecipe.PowerDemandMW > 0f && SuppliedPowerMW <= 0f) return false;

        activeRecipe = selectedRecipe;

        ConsumeInputs(activeRecipe);
        ConsumeFluidInputs(activeRecipe);
        
        craftingElapsed = 0f;

        return true;
    }

    private void ConsumeInputs(RecipeDefinition recipe) {
        foreach (ItemAmount input in recipe.Inputs) {
            int remainingAmount = input.Amount;

            foreach (InventorySlot slot in inputSlots) {
                if (slot.Item != input.Item) continue;

                remainingAmount -= slot.Remove(remainingAmount);

                if (remainingAmount == 0) break;
            }
        }
    }

    private void ConsumeFluidInputs(RecipeDefinition recipe) {
        foreach (FluidAmount input in recipe.FluidInputs) {
            float remainingAmount = input.Amount;

            foreach (FluidSlot slot in fluidInputSlots) {
                if (slot.Fluid != input.Fluid) continue;

                remainingAmount -= slot.Remove(remainingAmount);

                if (remainingAmount <= 0f) break;
            }
        }
    }

    private void CompleteCrafting() {
        foreach (ItemAmount output in activeRecipe.Outputs) {
            AddToSlots(
                outputSlots,
                output.Item,
                output.Amount
            );
        }

        foreach (FluidAmount output in activeRecipe.FluidOutputs) {
            AddFluidToSlots(
                fluidOutputSlots,
                output.Fluid,
                output.Amount
            );
        }

        Debug.Log($"Crafting completed: {activeRecipe.RecipeName}", this);

        activeRecipe = null;
        craftingElapsed = 0f;
    }

    public ItemDefinition GetInputSlotItem(int slotIndex) {
        return inputSlots[slotIndex].Item;
    }

    public FluidDefinition GetFluidInputSlotFluid(int slotIndex) {
        return fluidInputSlots[slotIndex].Fluid;
    }

    public int GetInputSlotAmount(int slotIndex) {
        return inputSlots[slotIndex].Amount;
    }

    public float GetFluidInputSlotAmount(int slotIndex) {
        return fluidInputSlots[slotIndex].Amount;
    }

    public ItemDefinition GetOutputSlotItem(int slotIndex) {
        return outputSlots[slotIndex].Item;
    }

    public FluidDefinition GetFluidOutputSlotFluid(int slotIndex) {
        return fluidOutputSlots[slotIndex].Fluid;
    }

    public int GetOutputSlotAmount(int slotIndex) {
        return outputSlots[slotIndex].Amount;
    }

    public float GetFluidOutputSlotAmount(int slotIndex) {
        return fluidOutputSlots[slotIndex].Amount;
    }

    public int RemoveOutputFromSlot(int slotIndex, int requestedAmount) {
        return outputSlots[slotIndex].Remove(requestedAmount);
    }

    public float RemoveFluidOutputFromSlot(int slotIndex, float requestedAmount) {
        return fluidOutputSlots[slotIndex].Remove(requestedAmount);
    }

    public int RemoveInputFromSlot(int slotIndex, int requestedAmount) {
        return inputSlots[slotIndex].Remove(requestedAmount);
    }

    public float RemoveFluidInputFromSlot(int slotIndex, float requestedAmount) {
        return fluidInputSlots[slotIndex].Remove(requestedAmount);
    }

    public void SetOn(bool value) {
        isOn = value;
    }

    public bool SupportsRecipe(RecipeDefinition recipe) {
        if (recipe == null) return false;
        if (tier < recipe.MinimumMachineTier) return false;

        foreach (RecipeDefinition availableRecipe in availableRecipes) {
            if (availableRecipe == recipe) return true;
        }

        return false;
    }

    public float GetRecipeSpeedMultiplier(RecipeDefinition recipe) {
        int tierDifference = Mathf.Max(0, tier - recipe.MinimumMachineTier);
        return Mathf.Pow(speedMultiplierPerTier, tierDifference);
    }

    private static bool ContainsItems(InventorySlot[] slots) {
        foreach (InventorySlot slot in slots) {
            if (slot.Amount > 0) return true;
        }

        return false;
    }

    private static bool ContainsFluids(FluidSlot[] slots) {
        if (slots == null) return false;

        foreach (FluidSlot slot in slots) {
            if (!slot.IsEmpty) return true;
        }

        return false;
    }

    private bool AcceptsInputItem(ItemDefinition item) {
        if (item == null || selectedRecipe == null) return false;

        foreach (ItemAmount input in selectedRecipe.Inputs) {
            if (input.Item == item) return true;
        }

        return false;
    }

    private bool AcceptsInputFluid(FluidDefinition fluid) {
        if (fluid == null || selectedRecipe == null) return false;

        foreach (FluidAmount input in selectedRecipe.FluidInputs) {
            if (input.Fluid == fluid) return true;
        }

        return false;
    }

    public bool TryAddInputFluidCell(ItemDefinition item, FluidCellData fluidCell) {
        if (item == null || !item.IsFluidCell || fluidCell == null || !AcceptsInputItem(item)) return false;

        foreach (InventorySlot slot in inputSlots) {
            if (slot.TryPutFluidCell(item, fluidCell)) return true;
        }

        return false;
    }

    public FluidCellData GetOutputSlotFluidCell(int slotIndex) {
        return outputSlots[slotIndex].FluidCell;
    }

    public FluidCellData GetInputSlotFluidCell(int slotIndex) {
        return inputSlots[slotIndex].FluidCell;
    }

    public void SetSuppliedPower(float powerMW) {
        SuppliedPowerMW = Mathf.Max(0f, powerMW);
    }

    // DEBUG METHODS
    [ContextMenu("Debug/Select First Recipe")]
    private void SelectFirstRecipeForDebug() {
        if (!Application.isPlaying) return;
        if (availableRecipes == null || availableRecipes.Length == 0) return;

        TrySelectRecipe(availableRecipes[0]);

        Debug.Log($"Selected recipe: {selectedRecipe.RecipeName}", this);
    }

    [ContextMenu("Debug/Add Recipe Inputs")]
    private void AddRecipeInputsForDebug() {
        if (!Application.isPlaying || selectedRecipe == null) return;

        foreach (ItemAmount input in selectedRecipe.Inputs) {
            int addedAmount = AddInput(input.Item, input.Amount);

            Debug.Log($"Added input: {input.Item.ItemName} × {addedAmount}", this);
        }
    }

    [ContextMenu("Debug/Check Crafting Conditions")]
    private void CheckCraftingConditionsForDebug() {
        if (!Application.isPlaying || selectedRecipe == null) return;

        Debug.Log(
            $"Inputs ready: {HasRequiredInputs(selectedRecipe)}, " +
            $"Output space available: {CanFitOutputs(selectedRecipe)}",
            this
        );
    }

    [ContextMenu("Debug/Toggle On Off")]
    private void ToggleOnOffForDebug() {
        if (!Application.isPlaying) return;

        SetOn(!isOn);

        Debug.Log(
            $"Machine {(isOn ? "ON" : "OFF")}, " +
            $"progress: {CraftingProgress:P0}",
            this
        );
    }

    [ContextMenu("Debug/Print State")]
    private void PrintStateForDebug() {
        if (!Application.isPlaying) {
            return;
        }

        Debug.Log(
            $"Object: {gameObject.name}, " +
            $"parent: {(transform.parent != null ? transform.parent.name : "None")}, " +
            $"active: {gameObject.activeInHierarchy}, " +
            $"component enabled: {enabled}, " +
            $"ON: {isOn}, " +
            $"time scale: {Time.timeScale}, " +
            $"selected recipe: {(selectedRecipe != null ? selectedRecipe.RecipeName : "None")}, " +
            $"active recipe: {(activeRecipe != null ? activeRecipe.RecipeName : "None")}, " +
            $"elapsed: {craftingElapsed:F2}",
            this
        );
    }

    [ContextMenu("Debug/Add Recipe Fluid Inputs")]
    private void AddRecipeFluidInputsForDebug() {
        if (!Application.isPlaying || selectedRecipe == null) return;

        foreach (FluidAmount input in selectedRecipe.FluidInputs) {
            float addedAmount = AddFluidInput(input.Fluid, input.Amount);

            Debug.Log(
                $"Added fluid input: {input.Fluid.FluidName} - {addedAmount:0.###}L",
                this
            );
        }
    }
}
