public interface IFluidCellTransferEndpoint {
    FluidCellData GetOutputSlotFluidCell(int slotIndex);

    bool TryAddInputFluidCell(ItemDefinition item, FluidCellData fluidCell);
}