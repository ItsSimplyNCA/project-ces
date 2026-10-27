using UnityEngine;

public class EnergySourceState {
    public PowerGenerator Generator { get; }
    public PowerStorage Storage { get; }

    public MonoBehaviour Owner { get; }
    public float AvailablePowerMW { get; }
    public float AllocatedPowerMW { get; private set; }

    public bool IsStorage => Storage != null;
    public float RemainingPowerMW => Mathf.Max(0f, AvailablePowerMW - AllocatedPowerMW);

    public EnergySourceState(PowerGenerator generator) {
        Generator = generator;
        Owner = generator;
        AvailablePowerMW = generator.AvailablePowerMW;
    }

    public EnergySourceState(PowerStorage storage, float deltaTime) {
        Storage = storage;
        Owner = storage;
        AvailablePowerMW = storage.GetAvailableDischargePowerMW(deltaTime);
    }

    public float Allocate(float requestedPowerMW) {
        if (requestedPowerMW <= 0f) return 0f;

        float allocatedPower = Mathf.Min(requestedPowerMW, RemainingPowerMW);

        AllocatedPowerMW += allocatedPower;

        return allocatedPower;
    }
}
