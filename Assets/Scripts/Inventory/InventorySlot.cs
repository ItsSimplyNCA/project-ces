using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class InventorySlot {
    [SerializeField] private ItemDefinition item;
    [SerializeField] private int amount;
    [SerializeField] private FluidCellData fluidCellData;

    public ItemDefinition Item => item;
    public int Amount => amount;
    public bool IsEmpty => item == null;
    public FluidCellData FluidCell => fluidCellData;

    public int Add(ItemDefinition itemToAdd, int requestedAmount) {
        if (itemToAdd == null || requestedAmount <= 0) return 0;

        if (!IsEmpty && item != itemToAdd) return 0;

        int availableSpace = itemToAdd.MaxStackSize - amount;
        int addedAmount = Mathf.Min(requestedAmount, availableSpace);

        if (addedAmount <= 0) return 0;

        if (IsEmpty) {
            fluidCellData = itemToAdd.IsFluidCell ? new FluidCellData() : null;
        }

        item = itemToAdd;
        amount += addedAmount;

        return addedAmount;
    }

    public int Remove(int requestedAmount) {
        if (IsEmpty || requestedAmount <= 0) return 0;

        int removedAmount = Mathf.Min(requestedAmount, amount);
        amount -= removedAmount;

        if (amount == 0) {
            Clear();
        }

        return removedAmount;
    }

    public int MoveTo(InventorySlot destination, int requestedAmount) {
        if (destination == null || destination == this || IsEmpty || requestedAmount <= 0) return 0;

        if (item.IsFluidCell) {
            if (!destination.IsEmpty) return 0;

            destination.item = item;
            destination.amount = 1;
            destination.fluidCellData = fluidCellData;

            Clear();

            return 1;
        }

        int amountToMove = Mathf.Min(requestedAmount, amount);
        int addedAmount = destination.Add(item, amountToMove);

        Remove(addedAmount);

        return addedAmount;
    }

    private void Clear() {
        item = null;
        amount = 0;
        fluidCellData = null;
    }

    public bool TryPutFluidCell(ItemDefinition cellItem, FluidCellData cellData) {
        if (!IsEmpty || cellItem == null || !cellItem.IsFluidCell || cellData == null) return false;

        item = cellItem;
        amount = 1;
        fluidCellData = cellData;

        return true;
    }
}