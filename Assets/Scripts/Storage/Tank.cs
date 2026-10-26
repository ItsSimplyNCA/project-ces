using UnityEngine;

public class Tank : MonoBehaviour, IFluidTransferEndpoint, IDeconstructionContents {
    [SerializeField] private TankDefinition definition;
    [SerializeField] private FluidDefinition debugFluid;
    
    private FluidDefinition fluid;
    private float amount;
    private bool isLocked;

    public int Tier => definition.Tier;
    public float Capacity => definition.Capacity;

    public FluidDefinition Fluid => fluid;
    public float Amount => amount;
    public bool IsLocked => isLocked;
    public bool HasContents => amount > 0f;
    public int FluidOutputSlotCount => 1;

    public float Add(FluidDefinition fluidToAdd, float requestedAmount) {
        if (fluidToAdd == null || requestedAmount <= 0f) return 0f;
        if (fluid != null && fluid != fluidToAdd) return 0f;

        float availableSpace = Capacity - amount;
        float addedAmount = Mathf.Min(
            requestedAmount,
            availableSpace
        );

        if (addedAmount <= 0f) return 0f;

        fluid = fluidToAdd;
        amount += addedAmount;

        return addedAmount;
    }

    public float Remove(float requestedAmount) {
        if (requestedAmount <= 0f || amount == 0f) return 0f;

        float removedAmount = Mathf.Min(
            requestedAmount,
            amount
        );

        amount -= removedAmount;

        if (amount == 0f && !isLocked) {
            fluid = null;
        }

        return removedAmount;
    }

    public bool TrySetLocked(bool value) {
        if (value && fluid == null) return false;

        isLocked = value;

        if (!isLocked && amount == 0f) {
            fluid = null;
        }

        return true;
    }

    public float FillCell(FluidCellData cell) {
        if (cell == null || fluid == null || amount <= 0f) return 0f;

        float requestedAmount = Mathf.Min(
            amount,
            cell.Capacity - cell.Amount
        );

        float transferredAmount = cell.Add(fluid, requestedAmount);

        Remove(transferredAmount);

        return transferredAmount;
    }

    public float DrainCell(FluidCellData cell) {
        if (cell == null || cell.Fluid == null || cell.Amount <= 0f) return 0f;

        float transferredAmount = Add(cell.Fluid, cell.Amount);

        cell.Remove(transferredAmount);

        return transferredAmount;
    }

    public float AddFluidInput(FluidDefinition fluid, float requestedAmount) {
        return Add(fluid, requestedAmount);
    }

    public FluidDefinition GetFluidOutputSlotFluid(int slotIndex) {
        return slotIndex == 0 && amount > 0f ? fluid : null;
    }

    public float GetFluidOutputSlotAmount(int slotIndex) {
        return slotIndex == 0 ? amount : 0f;
    }

    public float RemoveFluidOutputFromSlot(int slotIndex, float requestedAmount) {
        return slotIndex == 0 ? Remove(requestedAmount) : 0f;
    }

    [ContextMenu("Debug/Add 2.5L Fluid")]
    private void AddFluidForDebug() {
        if (!Application.isPlaying) return;

        float addedAmount = Add(debugFluid, 2.5f);

        Debug.Log($"Added {addedAmount:0.###}L", this);
    }

    [ContextMenu("Debug/Fill Tank")]
    private void FillTankForDebug() {
        if (!Application.isPlaying || debugFluid == null) return;
        float addedAmount = Add(debugFluid, Capacity - amount);
        Debug.Log($"Added {addedAmount:0.###}L", this);
    }
}
