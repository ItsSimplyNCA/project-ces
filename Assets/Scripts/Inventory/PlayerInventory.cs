using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerInventory : MonoBehaviour {
    [System.Serializable] public class StartingItem {
        [SerializeField] private ItemDefinition item;
        [SerializeField, Min(0)] private int amount = 5;

        public ItemDefinition Item => item;
        public int Amount => amount;
    }

    [SerializeField, Min(1)] private int slotCount = 20;
    [SerializeField] private StartingItem[] startingItems;

    private InventorySlot[] slots;

    public int SlotCount => slots.Length;

    private void Awake() {
        slots = new InventorySlot[slotCount];

        for (int i = 0; i < slots.Length; i++) {
            slots[i] = new InventorySlot();
        }
    }

    private void Start() {
        foreach (StartingItem startingItem in startingItems) {
            Add(startingItem.Item, startingItem.Amount);
        }
    }

    public int Add(ItemDefinition item, int requestedAmount) {
        if (item == null || requestedAmount <= 0) return 0;

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

    public int GetAmount(ItemDefinition item) {
        if (item == null) return 0;

        int totalAmount = 0;

        foreach (InventorySlot slot in slots) {
            if (slot.Item == item) {
                totalAmount += slot.Amount;
            }
        }

        return totalAmount;
    }

    public bool TryRemove(ItemDefinition item, int requestedAmount) {
        if (item == null || requestedAmount <= 0) return false;

        if (GetAmount(item) < requestedAmount) return false;

        int remainingAmount = requestedAmount;

        foreach (InventorySlot slot in slots) {
            if (slot.Item != item) continue;

            remainingAmount -= slot.Remove(remainingAmount);

            if (remainingAmount == 0) return true;
        }

        return false;
    }

    public bool CanAdd(ItemDefinition item, int requestedAmount) {
        if (item == null || requestedAmount <= 0) return false;

        int availableSpace = 0;

        foreach (InventorySlot slot in slots) {
            if (slot.IsEmpty) {
                availableSpace += item.MaxStackSize;
            } else if (slot.Item == item) {
                availableSpace += item.MaxStackSize - slot.Amount;
            }

            if (availableSpace >= requestedAmount) return true;
        }

        return false;
    }

    public ItemDefinition GetSlotItem(int slotIndex) {
        return slots[slotIndex].Item;
    }

    public int GetSlotAmount(int slotIndex) {
        return slots[slotIndex].Amount;
    }

    public FluidCellData GetSlotFluidCell(int slotIndex) {
        return slots[slotIndex].FluidCell;
    }

    public int MoveSlot(int sourceIndex, int destinationIndex, int requestedAmount) {
        return slots[sourceIndex].MoveTo(slots[destinationIndex], requestedAmount);
    }

    public bool TryAddFluidCell(ItemDefinition item, FluidCellData fluidCell) {
        if (item == null || !item.IsFluidCell || fluidCell == null) return false;

        foreach (InventorySlot slot in slots) {
            if (slot.TryPutFluidCell(item, fluidCell)) return true;
        }

        return false;
    }

    public int TransferTo(IItemTransferEndpoint destination, ItemDefinition item, int requestedAmount) {
        if (destination == null || item == null || requestedAmount <= 0) return 0;

        int remainingAmount = requestedAmount;

        foreach (InventorySlot slot in slots) {
            if (slot.Item != item) continue;

            int acceptedAmount;

            if (item.IsFluidCell) {
                if (!(destination is IFluidCellTransferEndpoint destinationCells)) {
                    return requestedAmount - remainingAmount;
                }

                FluidCellData fluidCell = slot.FluidCell;

                acceptedAmount = fluidCell != null && destinationCells.TryAddInputFluidCell(item, fluidCell) ? 1 : 0;
            } else {
                int amountToTransfer = Mathf.Min(
                    slot.Amount,
                    remainingAmount
                );

                acceptedAmount = destination.AddInput(
                    item,
                    amountToTransfer
                );
            }

            slot.Remove(acceptedAmount);
            remainingAmount -= acceptedAmount;

            if (remainingAmount == 0) break;
        }

        return requestedAmount - remainingAmount;
    }

    public int CollectFrom(IItemTransferEndpoint source, int requestedAmount = int.MaxValue) {
        if (source == null || requestedAmount <= 0) return 0;

        int collectedAmount = 0;

        for (int i = 0; i < source.OutputSlotCount; i++) {
            while (collectedAmount < requestedAmount) {
                ItemDefinition item = source.GetOutputSlotItem(i);

                if (item == null || source.GetOutputSlotAmount(i) <= 0) break;

                int acceptedAmount;

                if (item.IsFluidCell) {
                    if (!(source is IFluidCellTransferEndpoint sourceCells)) break;

                    FluidCellData fluidCell = sourceCells.GetOutputSlotFluidCell(i);

                    acceptedAmount = TryAddFluidCell(item, fluidCell) ? 1 : 0;
                } else {
                    int amountToCollect = Mathf.Min(
                        source.GetOutputSlotAmount(i),
                        requestedAmount - collectedAmount
                    );

                    acceptedAmount = Add(item, amountToCollect);
                }

                if (acceptedAmount == 0) break;

                source.RemoveOutputFromSlot(i, acceptedAmount);
                collectedAmount += acceptedAmount;
            }
        }

        return collectedAmount;
    }

    public float DrainFluidCellTo(int InventorySlotIndex, IFluidTransferEndpoint destination) {
        if (destination == null) return 0f;
        if (InventorySlotIndex < 0 || InventorySlotIndex >= slots.Length) return 0f;

        FluidCellData cell = slots[InventorySlotIndex].FluidCell;

        if (cell == null || cell.Fluid == null || cell.Amount <= 0f) return 0f;

        float transferredAmount = destination.AddFluidInput(cell.Fluid, cell.Amount);

        cell.Remove(transferredAmount);

        return transferredAmount;
    }

    public float FillFluidCellFrom(int inventorySlotIndex, IFluidTransferEndpoint source, int sourceSlotIndex) {
        if (source == null) return 0f;
        if (inventorySlotIndex < 0 || inventorySlotIndex >= slots.Length) return 0f;
        if (sourceSlotIndex < 0 || sourceSlotIndex >= source.FluidOutputSlotCount) return 0f;

        FluidCellData cell = slots[inventorySlotIndex].FluidCell;

        if (cell == null) return 0f;

        FluidDefinition fluid = source.GetFluidOutputSlotFluid(sourceSlotIndex);

        if (fluid == null) return 0f;

        float requestedAmount = Mathf.Min(cell.Capacity - cell.Amount, source.GetFluidOutputSlotAmount(sourceSlotIndex));
        float transferredAmount = cell.Add(fluid, requestedAmount);

        source.RemoveFluidOutputFromSlot(sourceSlotIndex, transferredAmount);

        return transferredAmount;
    }

    public float FillFluidCellFromMachineInput(int inventorySlotIndex, Machine machine, int inputSlotIndex) {
        if (machine == null) return 0f;
        if (inventorySlotIndex < 0 || inventorySlotIndex >= slots.Length) return 0f;
        if (inputSlotIndex < 0 || inputSlotIndex >= machine.FluidInputSlotCount) return 0f;

        FluidCellData cell = slots[inventorySlotIndex].FluidCell;

        if (cell == null) return 0f;

        FluidDefinition fluid = machine.GetFluidInputSlotFluid(inputSlotIndex);

        if (fluid == null) return 0f;

        float requestedAmount = Mathf.Min(
            cell.Capacity - cell.Amount,
            machine.GetFluidInputSlotAmount(inputSlotIndex)
        );

        float transferredAmount = cell.Add(fluid, requestedAmount);

        machine.RemoveFluidInputFromSlot(
            inputSlotIndex,
            transferredAmount
        );

        return transferredAmount;
    }

    public bool HasRecipeInputs(RecipeDefinition recipe) {
        if (recipe == null) return false;

        Dictionary<ItemDefinition, int> requiredAmounts = new();
        
        foreach (ItemAmount input in recipe.Inputs) {
            if (input.Item == null || input.Amount <= 0) return false;

            requiredAmounts.TryGetValue(input.Item, out int amount);
            requiredAmounts[input.Item] = amount + input.Amount;
        }

        foreach (KeyValuePair<ItemDefinition, int> required in requiredAmounts) {
            if (GetAmount(required.Key) < required.Value) {
                return false;
            }
        }

        return true;
    }

    public bool CanFitRecipeOutputs(RecipeDefinition recipe) {
        if (recipe == null) return false;

        InventorySlot[] simulatedSlots = new InventorySlot[slots.Length];

        for (int i = 0; i < slots.Length; i++) {
            simulatedSlots[i] = new InventorySlot();

            if (!slots[i].IsEmpty) {
                simulatedSlots[i].Add(slots[i].Item, slots[i].Amount);
            }
        }

        foreach (ItemAmount output in recipe.Outputs) {
            if (output.Item == null || output.Amount <= 0) return false;

            int remainingAmount = output.Amount;

            foreach (InventorySlot slot in simulatedSlots) {
                if (slot.Item != output.Item) continue;
                remainingAmount -= slot.Add(output.Item, remainingAmount);
                if (remainingAmount == 0) break;
            }

            if (remainingAmount == 0) continue;

            foreach (InventorySlot slot in simulatedSlots) {
                if (!slot.IsEmpty) continue;
                remainingAmount -= slot.Add(output.Item, remainingAmount);
                if (remainingAmount == 0) break;
            }

            if (remainingAmount > 0) return false;
        }

        return true;
    }

    public bool TryTakeRecipeInputs(RecipeDefinition recipe, out List<InventorySlot> takenInputs) {
        takenInputs = new List<InventorySlot>();

        if (!HasRecipeInputs(recipe)) return false;

        foreach (ItemAmount input in recipe.Inputs) {
            int remainingAmount = input.Amount;

            foreach (InventorySlot slot in slots) {
                if (slot.Item != input.Item) continue;

                InventorySlot takenSlot = new InventorySlot();

                int movedAmount = slot.MoveTo(
                    takenSlot,
                    remainingAmount
                );

                if (movedAmount <= 0) continue;

                takenInputs.Add(takenSlot);
                remainingAmount -= movedAmount;

                if (remainingAmount == 0) break;
            }
        }

        return true;
    }

    public bool ReturnRecipeInputs(List<InventorySlot> takenInputs) {
        if (takenInputs == null) return true;

        foreach (InventorySlot takenSlot in takenInputs) {
            if (takenSlot.IsEmpty) continue;

            foreach (InventorySlot destination in slots) {
                if (destination.IsEmpty) continue;
                if (destination.Item != takenSlot.Item) continue;

                takenSlot.MoveTo(destination, takenSlot.Amount);

                if (takenSlot.IsEmpty) break;
            }

            if (takenSlot.IsEmpty) continue;

            foreach (InventorySlot destination in slots) {
                if (!destination.IsEmpty) continue;

                takenSlot.MoveTo(destination, takenSlot.Amount);

                if (takenSlot.IsEmpty) break;
            }
        }

        foreach (InventorySlot takenSlot in takenInputs) {
            if (!takenSlot.IsEmpty) return false;
        }

        return true;
    }

    public InventorySaveData CaptureSaveData() {
        InventorySaveData data = new InventorySaveData {
            slotCount = slots.Length
        };

        foreach (InventorySlot slot in slots) {
            InventorySlotSaveData slotData = new();

            if (!slot.IsEmpty) {
                slotData.itemId = slot.Item.ItemId;
                slotData.amount = slot.Amount;

                FluidCellData cell = slot.FluidCell;

                if (cell != null) {
                    slotData.fluidId = cell.Fluid != null ? cell.Fluid.FluidId : string.Empty;
                }
            }

            data.slots.Add(slotData);
        }

        return data;
    }

    public bool TryRestoreSaveData(InventorySaveData data, GameDefinitionCatalog catalog) {
        bool Fail(string message) {
            Debug.LogWarning($"Inventory load failed: {message}", this);
            return false;
        }

        if (data == null || data.slots == null) {
            return Fail("Missing inventory data.");
        }

        if (catalog == null || !catalog.IsValid) {
            return Fail("Invalid definition catalog.");
        }

        if (data.slotCount != slots.Length || data.slots.Count != data.slotCount) {
            return Fail("Inventory slot count does not match.");
        }

        InventorySlot[] restoredSlots = new InventorySlot[data.slotCount];

        for (int i = 0; i < restoredSlots.Length; i++) {
            InventorySlotSaveData slotData = data.slots[i];

            if (slotData == null) {
                return Fail($"Missing data for slot {i + 1}.");
            }

            if (float.IsNaN(slotData.fluidAmount) || float.IsInfinity(slotData.fluidAmount) || slotData.fluidAmount < 0f) {
                return Fail($"Invalid fluid amount in slot {i + 1}.");
            }

            InventorySlot restoredSlot = new();
            restoredSlots[i] = restoredSlot;

            bool hasFluidId = !string.IsNullOrEmpty(slotData.fluidId);

            if (string.IsNullOrEmpty(slotData.itemId)) {
                if (slotData.amount != 0 || hasFluidId || slotData.fluidAmount != 0f) {
                    return Fail($"Invalid empty slot {i + 1}.");
                }

                continue;
            }

            if (!catalog.TryGetItem(slotData.itemId, out ItemDefinition item)) {
                return Fail($"Unknown item ID: {slotData.itemId}");
            }

            if (slotData.amount <= 0 || slotData.amount > item.MaxStackSize) {
                return Fail($"Invalid item amount in slot {i + 1}.");
            }

            if (!item.IsFluidCell && (hasFluidId || slotData.fluidAmount != 0f)) {
                return Fail($"Unexpected fluid data in slot {i + 1}.");
            }

            int addedAmount = restoredSlot.Add(item, slotData.amount);

            if (addedAmount != slotData.amount) {
                return Fail($"Could not restore slot {i + 1}.");
            }

            if (!item.IsFluidCell) continue;

            if (!hasFluidId) {
                if (slotData.fluidAmount != 0f) {
                    return Fail($"Missing fluid ID in slot {i + 1}.");
                }

                continue;
            }

            if (!catalog.TryGetFluid(slotData.fluidId, out FluidDefinition fluid)) {
                return Fail($"Unknown fluid ID: {slotData.fluidId}");
            }

            FluidCellData cell = restoredSlot.FluidCell;

            if (slotData.fluidAmount <= 0f || slotData.fluidAmount > cell.Capacity) {
                return Fail($"Invalid cell contents in slot {i + 1}.");
            }

            float addedFluid = cell.Add(fluid, slotData.fluidAmount);

            if (addedFluid != slotData.fluidAmount) {
                return Fail($"Could not restore cell in slot {i + 1}.");
            }
        }

        slots = restoredSlots;

        return true;
    }

    [ContextMenu("Debug/Print Inventory Save Data")]
    private void PrintInventorySaveDataForDebug() {
        if (!Application.isPlaying) return;

        InventorySaveData data = CaptureSaveData();

        Debug.Log(JsonUtility.ToJson(data, true), this);
    }
}
