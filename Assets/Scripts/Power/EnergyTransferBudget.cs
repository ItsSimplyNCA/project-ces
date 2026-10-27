using System;
using System.Collections.Generic;
using UnityEngine;

public class EnergyTransferBudget {
    private readonly Dictionary<EnergyCableSegment, float> remainingPowerMW = new();

    public void BeginCalculation(IEnumerable<EnergyCableSegment> cables) {
        remainingPowerMW.Clear();

        foreach (EnergyCableSegment cable in cables) {
            if (cable == null || cable.Definition == null) continue;

            remainingPowerMW[cable] = cable.Definition.CapacityMW;
        }
    }

    public float GetRemaining(EnergyCableSegment cable) {
        if (cable == null) return 0f;

        return remainingPowerMW.TryGetValue(cable, out float powerMW)
            ? powerMW
            : 0f;
    }

    public float GetAvailable(IReadOnlyList<EnergyCableSegment> path) {
        if (path == null || path.Count == 0) return 0f;

        float availablePowerMW = float.PositiveInfinity;

        foreach (EnergyCableSegment cable in path) {
            availablePowerMW = Mathf.Min(
                availablePowerMW,
                GetRemaining(cable)
            );
        }

        return availablePowerMW;
    }

    public void Consume(IReadOnlyList<EnergyCableSegment> path, float allocatedPowerMW) {
        if (path == null || path.Count == 0 || allocatedPowerMW <= 0f) return;

        foreach (EnergyCableSegment cable in path) {
            if (allocatedPowerMW > GetRemaining(cable)) {
                throw new System.InvalidOperationException(
                    "Allocated power exceeds the remaining cable capacity."
                );
            }
        }

        foreach (EnergyCableSegment cable in path) {
            remainingPowerMW[cable] = Mathf.Max(
                0f,
                remainingPowerMW[cable] - allocatedPowerMW
            );
        }
    }
}
