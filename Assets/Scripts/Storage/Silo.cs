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
}
