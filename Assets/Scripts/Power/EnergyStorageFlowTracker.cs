using System.Collections.Generic;

public class EnergyStorageFlowTracker {
    private readonly Dictionary<PowerStorage, float> transitRates = new();

    public void BeginCalculation() {
        transitRates.Clear();
    }

    public void RecordTransfer(EnergyRoute route, float allocatedRate) {
        if (route == null || allocatedRate <= 0f) return;

        foreach (PowerStorage storage in route.Storages) {
            transitRates.TryGetValue(storage, out float transitRate);
            transitRates[storage] = transitRate + allocatedRate;
        }
    }

    public void ApplyTransitStatistics() {
        foreach (var entry in transitRates) {
            if (entry.Key != null) {
                entry.Key.RecordTransitPower(entry.Value);
            }
        }

        transitRates.Clear();
    }
}
