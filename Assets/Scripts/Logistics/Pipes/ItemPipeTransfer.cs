using UnityEngine;

public static class ItemPipeTransfer {
    public static int TransferFromSlot(ItemPipeEndpoint source, int outputSlotIndex, ItemPipeEndpoint destination, int requestedAmount) {
        if (source == null || destination == null || requestedAmount <= 0) return 0;
        if (source.Owner == null || destination.Owner == null) return 0;
        if (source.Storage is Silo && object.ReferenceEquals(source.Storage, destination.Storage)) return 0;
        if (source.Port.Mode != PortMode.ItemOutput || destination.Port.Mode != PortMode.ItemInput) return 0;

        ItemDefinition item = source.Storage.GetOutputSlotItem(outputSlotIndex);

        if (item == null) return 0;

        if (item.IsFluidCell) {
            if (!(source.Storage is IFluidCellTransferEndpoint sourceCells) || !(destination.Storage is IFluidCellTransferEndpoint destinationCells)) return 0;

            FluidCellData fluidCell = sourceCells.GetOutputSlotFluidCell(outputSlotIndex);

            if (fluidCell == null || !destinationCells.TryAddInputFluidCell(item, fluidCell)) return 0;

            source.Storage.RemoveOutputFromSlot(outputSlotIndex, 1);

            return 1;
        }

        int availableAmount = source.Storage.GetOutputSlotAmount(outputSlotIndex);
        int amountToTransfer = Mathf.Min(availableAmount, requestedAmount);
        int acceptedAmount = destination.Storage.AddInput(item, amountToTransfer);

        source.Storage.RemoveOutputFromSlot(outputSlotIndex, acceptedAmount);

        return acceptedAmount;
    }
}
