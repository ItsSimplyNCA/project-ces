using System.Collections.Generic;
using UnityEngine;

public class FluidPipeTransferBudget {
    private readonly Dictionary<PipeSegment, float> remainingAmounts = new();

    public void BeginTick(IEnumerable<PipeSegment> pipes, float tickDuration) {
        remainingAmounts.Clear();

        foreach (PipeSegment pipe in pipes) {
            if (pipe == null || pipe.Definition == null) continue;
            if (pipe.Definition.Medium != PipeMedium.Fluid) continue;

            remainingAmounts[pipe] = pipe.Definition.TransferRate * Mathf.Max(0f, tickDuration);
        }
    }

    public float GetRemaining(PipeSegment pipe) {
        if (pipe == null) return 0f;
        return remainingAmounts.TryGetValue(pipe, out float amount) ? amount : 0f;
    }

    public float GetAvailable(IReadOnlyList<PipeSegment> path) {
        if (path == null || path.Count == 0) return 0f;

        float availableAmount = float.PositiveInfinity;

        foreach (PipeSegment pipe in path) {
            availableAmount = Mathf.Min(
                availableAmount,
                GetRemaining(pipe)
            );
        }

        return availableAmount;
    }

    public void Consume(IReadOnlyList<PipeSegment> path, float transferredAmount) {
        if (path == null || path.Count == 0 || transferredAmount <= 0f) return;

        foreach (PipeSegment pipe in path) {
            float remainingAmount = GetRemaining(pipe);

            if (transferredAmount > remainingAmount) {
                throw new System.InvalidOperationException("Fluid transfer exceeds the remaining pipe capacity");
            }
        }

        foreach (PipeSegment pipe in path) {
            remainingAmounts[pipe] = Mathf.Max(
                0f,
                remainingAmounts[pipe] - transferredAmount
            );
        }
    }
}
