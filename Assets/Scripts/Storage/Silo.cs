using System.Collections.Generic;
using UnityEngine;

public class Silo : MonoBehaviour, IItemTransferEndpoint, IFluidCellTransferEndpoint, IDeconstructionContents {
    [SerializeField] private SiloDefinition definition;

    private ItemDefinition item;
    private int amount;
    private bool isLocked;

    private readonly Queue<FluidCellData> fluidCells = new();

    public int Tier => definition.Tier;
    public int Capacity => definition.Capacity;
    public ItemDefinition Item => item;
    public int Amount => amount;
    public bool IsLocked => isLocked;
    public int OutputSlotCount => 1;
    public bool HasContents => amount > 0;

    public int Add(ItemDefinition itemToAdd, int requestedAmount) {
        if (itemToAdd == null || requestedAmount <= 0) return 0;
        if (item != null && item != itemToAdd) return 0;

        int availableSpace = Capacity - amount;
        int addedAmount = Mathf.Min(requestedAmount, availableSpace);

        if (addedAmount <= 0) return 0;

        if (itemToAdd.IsFluidCell) {
            for (int i = 0; i < addedAmount; i++) {
                fluidCells.Enqueue(new FluidCellData());
            }
        }

        item = itemToAdd;
        amount += addedAmount;

        return addedAmount;
    }

    public int Remove(int requestedAmount) {
        if (requestedAmount <= 0 || amount == 0) return 0;

        int removeAmount = Mathf.Min(requestedAmount, amount);

        if (item.IsFluidCell) {
            for (int i = 0; i < removeAmount; i++) {
                fluidCells.Dequeue();
            }
        }

        amount -= removeAmount;

        if (amount == 0 && !isLocked) {
            item = null;
        }

        return removeAmount;
    }

    public bool TrySetLocked(bool value) {
        if (value && item == null) return false;

        isLocked = value;

        if (!isLocked && amount == 0) {
            item = null;
        }

        return true;
    }

    public int AddInput(ItemDefinition item, int requestedAmount) {
        return Add(item, requestedAmount);
    }

    public bool TryAddInputFluidCell(ItemDefinition cellItem, FluidCellData fluidCell) {
        if (cellItem == null || !cellItem.IsFluidCell || fluidCell == null || amount >= Capacity) return false;
        if (item != null && item != cellItem) return false;

        item = cellItem;

        fluidCells.Enqueue(fluidCell);
        amount++;

        return true;
    }

    public ItemDefinition GetOutputSlotItem(int slotIndex) {
        ValidateOutputSlot(slotIndex);
        return amount > 0 ? item : null;
    }

    public int GetOutputSlotAmount(int slotIndex) {
        ValidateOutputSlot(slotIndex);
        return amount;
    }

    public int RemoveOutputFromSlot(int slotIndex, int requestedAmount) {
        ValidateOutputSlot(slotIndex);
        return Remove(requestedAmount);
    }

    private static void ValidateOutputSlot(int slotIndex) {
        if (slotIndex != 0) {
            throw new System.ArgumentOutOfRangeException(nameof(slotIndex));
        }
    }

    public FluidCellData GetOutputSlotFluidCell(int slotIndex) {
        ValidateOutputSlot(slotIndex);

        return amount > 0 && item.IsFluidCell ? fluidCells.Peek() : null;
    }

    public SiloSaveData CaptureSaveData() {
        SiloSaveData data = new() {
            itemId = item != null ? item.ItemId : string.Empty,
            amount = this.amount,
            isLocked = this.isLocked
        };

        foreach (FluidCellData cell in fluidCells) {
            data.fluidCells.Add(new FluidSlotSaveData {
                fluidId = cell.Fluid != null ? cell.Fluid.FluidId : string.Empty,
                amount = cell.Amount
            });
        }

        return data;
    }

    public bool ValidateSaveData(SiloSaveData data, GameDefinitionCatalog catalog) {
        bool Fail(string message) {
            Debug.LogWarning($"Silo validation failed: {message}", this);
            return false;
        }

        if (data == null) {
            return Fail("Missing silo data.");
        }

        if (catalog == null || !catalog.IsValid) {
            return Fail("Invalid definition catalog.");
        }

        if (definition == null || Capacity <= 0) {
            return Fail("Missing definition or invalid capacity.");
        }

        if (data.amount < 0 || data.amount > Capacity) {
            return Fail("Invalid item amount.");
        }

        if (data.fluidCells == null) {
            return Fail("Missing fluid cell list.");
        }

        if (string.IsNullOrEmpty(data.itemId)) {
            if (data.amount != 0 || data.isLocked || data.fluidCells.Count != 0) {
                return Fail("Contents or lock exist without an item ID.");
            }

            return true;
        }

        if (!catalog.TryGetItem(data.itemId, out ItemDefinition savedItem)) {
            return Fail($"Unknown item ID: {data.itemId}");
        }

        if (data.amount == 0 && !data.isLocked) {
            return Fail("An empty unlocked silo must not retain an item ID.");
        }

        if (!savedItem.IsFluidCell) {
            if (data.fluidCells.Count != 0) {
                return Fail("Unexpected fluid cells for a regular item.");
            }

            return true;
        }

        if (data.fluidCells.Count != data.amount) {
            return Fail("Fluid cell count does not match the item amount.");
        }

        float cellCapacity = new FluidCellData().Capacity;

        for (int i = 0; i < data.fluidCells.Count; i++) {
            FluidSlotSaveData cell = data.fluidCells[i];

            if (cell == null) {
                return Fail($"Missing fluid cell data at index {i}.");
            }

            if (float.IsNaN(cell.amount) || float.IsInfinity(cell.amount) || cell.amount < 0f || cell.amount > cellCapacity) {
                return Fail($"Invalid fluid cell amount at index {i}.");
            }

            if (string.IsNullOrEmpty(cell.fluidId)) {
                if (cell.amount != 0f) {
                    return Fail($"Cell contents exist without a fluid ID at index {i}.");
                }

                continue;
            }

            if (!catalog.TryGetFluid(cell.fluidId, out _)) {
                return Fail($"Unknown fluid ID at index {i}: {cell.fluidId}");
            }

            if (cell.amount <= 0f) {
                return Fail($"Fluid ID exists without contents at index {i}.");
            }
        }

        return true;
    }

    public bool TryRestoreSaveData(SiloSaveData data, GameDefinitionCatalog catalog) {
        if (!Application.isPlaying) {
            Debug.LogWarning("Silo state can only be restored in Play mode", this);
            return false;
        }

        if (!ValidateSaveData(data, catalog)) return false;

        ItemDefinition restoredItem = null;

        if (!string.IsNullOrEmpty(data.itemId)) {
            catalog.TryGetItem(data.itemId, out restoredItem);
        }

        Queue<FluidCellData> restoredCells = new();

        foreach (FluidSlotSaveData savedCell in data.fluidCells) {
            FluidCellData cell = new();

            if (!string.IsNullOrEmpty(savedCell.fluidId)) {
                catalog.TryGetFluid(savedCell.fluidId, out FluidDefinition restoredFluid);
                cell.Add(restoredFluid, savedCell.amount);
            }

            restoredCells.Enqueue(cell);
        }

        fluidCells.Clear();

        foreach (FluidCellData cell in restoredCells) {
            fluidCells.Enqueue(cell);
        }

        item = restoredItem;
        amount = data.amount;
        isLocked = data.isLocked;

        return true;
    }
}
