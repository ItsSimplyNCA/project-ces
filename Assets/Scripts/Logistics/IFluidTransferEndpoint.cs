public interface IFluidTransferEndpoint {
    float AddFluidInput(FluidDefinition fluid, float requestedAmount);
    int FluidOutputSlotCount { get; }
    FluidDefinition GetFluidOutputSlotFluid(int slotIndex);
    float GetFluidOutputSlotAmount(int slotIndex);
    float RemoveFluidOutputFromSlot(int slotIndex, float requestedAmount);
}