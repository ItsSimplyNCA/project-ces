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

    public TankSaveData CaptureSaveData() {
        return new TankSaveData {
            fluidId = fluid != null ? fluid.FluidId : string.Empty,
            amount = this.amount,
            isLocked = this.isLocked
        };
    }

    public bool ValidateSaveData(TankSaveData data, GameDefinitionCatalog catalog) {
        bool Fail(string message) {
            Debug.LogWarning($"Tank validation failed: {message}", this);
            return false;
        }

        if (data == null) {
            return Fail("Missing tank data.");
        }

        if (catalog == null || !catalog.IsValid) {
            return Fail("Invalid definition catalog.");
        }

        if (definition == null) {
            return Fail("Missing tank definition.");
        }

        if (float.IsNaN(Capacity) || float.IsInfinity(Capacity) || Capacity <= 0f) {
            return Fail("Invalid tank capacity.");
        }

        if (float.IsNaN(data.amount) || float.IsInfinity(data.amount) || data.amount < 0f || data.amount > Capacity) {
            return Fail("Invalid fluid amount.");
        }

        if (string.IsNullOrEmpty(data.fluidId)) {
            if (data.amount != 0f || data.isLocked) {
                return Fail("Contents or lock exist without a fluid ID");
            }
            
            return true;
        }

        if (!catalog.TryGetFluid(data.fluidId, out _)) {
            return Fail($"Unknown fluid ID: {data.fluidId}");
        }

        if (data.amount == 0f && !data.isLocked) {
            return Fail("An empty unlocked tank must not retain a fluid ID.");
        }

        return true;
    }

    public bool TryRestoreSaveData(TankSaveData data, GameDefinitionCatalog catalog) {
        if (!Application.isPlaying) {
            Debug.LogWarning("Tank state can only be restored in Play mode.", this);
            return false;
        }

        if (!ValidateSaveData(data, catalog)) {
            return false;
        }

        FluidDefinition restoredFluid = null;

        if (!string.IsNullOrEmpty(data.fluidId)) {
            catalog.TryGetFluid(data.fluidId, out restoredFluid);
        }

        fluid = restoredFluid;
        amount = data.amount;
        isLocked = data.isLocked;

        return true;
    }
    
    // DEBUG METHODS
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
