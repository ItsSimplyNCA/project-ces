using UnityEngine;

public static class FluidPipeTransfer {
    public static float TransferFromSlot(
        FluidPipeEndpoint source,
        int sourceSlotIndex,
        FluidPipeEndpoint destination,
        float requestedAmount
    ) {
        if (source == null || destination == null) return 0f;
        if (source.Owner == null || destination.Owner == null) return 0f;
        if (requestedAmount <= 0f) return 0f;
        if (source.Port.Mode != PortMode.FluidOutput || destination.Port.Mode != PortMode.FluidInput) return 0f;
        if (source.Owner is Tank && source.Owner == destination.Owner) return 0f;
        if (sourceSlotIndex < 0 || sourceSlotIndex >= source.Storage.FluidOutputSlotCount) return 0f;

        FluidDefinition fluid = source.Storage.GetFluidOutputSlotFluid(sourceSlotIndex);

        if (fluid == null) return 0f;

        float availableAmount = source.Storage.GetFluidOutputSlotAmount(sourceSlotIndex);
        float amountToTransfer = Mathf.Min(requestedAmount, availableAmount);

        if (amountToTransfer <= 0f) return 0f;

        float acceptedAmount = destination.Storage.AddFluidInput(fluid, amountToTransfer);

        source.Storage.RemoveFluidOutputFromSlot(sourceSlotIndex, acceptedAmount);

        return acceptedAmount;
    }
}
