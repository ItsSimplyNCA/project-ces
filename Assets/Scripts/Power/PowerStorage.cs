using UnityEngine;

public class PowerStorage : MonoBehaviour, IDeconstructionContents {
    [SerializeField] private PowerStorageDefinition definition;

    private float storedPower;

    public int Tier => definition.Tier;
    public float CapacityMW => definition.CapacityMW;
    public float StoredPowerMW => storedPower;
    public float ChargeRatio => Mathf.Clamp01(storedPower / CapacityMW);
    public bool HasContents => storedPower > 0f;
    public float TransferLimitMW => definition.TransferLimitEnabled ? definition.TransferLimitMW : float.PositiveInfinity;

    public float InputRateMW { get; private set; }
    public float OutputRateMW { get; private set; }
    public float NetRateMW => InputRateMW - OutputRateMW;

    public void BeginSimulationStep() {
        InputRateMW = 0f;
        OutputRateMW = 0f;
    }
    
    public float GetAvailableDischargePowerMW(float deltaTime) {
        if (deltaTime <= 0f || storedPower <= 0f) return 0f;
        return Mathf.Min(storedPower / deltaTime, TransferLimitMW);
    }

    public float GetAvailableChargePowerMW(float deltaTime) {
        if (deltaTime <= 0f) return 0f;
        float availableSpace = Mathf.Max(0f, CapacityMW - storedPower);
        return Mathf.Min(availableSpace / deltaTime, TransferLimitMW);
    }

    public float ReceivePower(float inputRateMW, float deltaTime) {
        if (inputRateMW <= 0f || deltaTime <= 0f) return 0f;

        float acceptedRate = Mathf.Min(inputRateMW, GetAvailableChargePowerMW(deltaTime));
        float addedPower = AddPower(acceptedRate * deltaTime);

        InputRateMW += addedPower / deltaTime;

        return addedPower;
    }

    public float SupplyPower(float outputRateMW, float deltaTime) {
        if (outputRateMW <= 0f || deltaTime <= 0f) return 0f;

        float suppliedRate = Mathf.Min(outputRateMW, GetAvailableDischargePowerMW(deltaTime));
        float removedPower = RemovePower(suppliedRate * deltaTime);

        OutputRateMW += removedPower / deltaTime;

        return removedPower;
    }

    private float AddPower(float requestedPower) {
        if (requestedPower <= 0f) return 0f;

        float availableSpace = Mathf.Max(0f, CapacityMW - storedPower);
        float addedPower = Mathf.Min(requestedPower, availableSpace);

        storedPower += addedPower;

        return addedPower;
    }

    private float RemovePower(float requestedPower) {
        if (requestedPower <= 0f) return 0f;

        float removedPower = Mathf.Min(requestedPower, storedPower);

        storedPower = Mathf.Max(0f, storedPower - removedPower);

        return removedPower;
    }

    [ContextMenu("Debug/Fill Power Storage")]
    private void FillForDebug() {
        if (!Application.isPlaying) return;
        AddPower(CapacityMW - storedPower);
    }

    [ContextMenu("Debug/Empty Power Storage")]
    private void EmptyForDebug() {
        if (!Application.isPlaying) return;
        RemovePower(storedPower);
    }
}
