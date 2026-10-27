using System.Collections.Generic;
using UnityEngine;

public class PipeTransferBudget {
    private readonly Dictionary<PipeSegment, int> remaining = new();
    private readonly Dictionary<PipeSegment, float> fractionalCarry = new();

    public void BeginTick(IEnumerable<PipeSegment> pipes, float tickDuration) {
        remaining.Clear();

        foreach (PipeSegment pipe in pipes) {
            if (pipe.Definition == null || pipe.Definition.Medium != PipeMedium.Item) continue;

            fractionalCarry.TryGetValue(pipe, out float carry);

            float availableAmount = pipe.Definition.TransferRate * tickDuration + carry;
            int wholeAmount = Mathf.FloorToInt(availableAmount);

            remaining[pipe] = wholeAmount;
            fractionalCarry[pipe] = availableAmount - wholeAmount;
        }
    }

    public int GetAvailable(IReadOnlyList<PipeSegment> path) {
        if (path.Count == 0) return 0;

        int availableAmount = int.MaxValue;

        foreach (PipeSegment pipe in path) {
            if (!remaining.TryGetValue(pipe, out int amount)) return 0;

            availableAmount = Mathf.Min(availableAmount, amount);
        }

        return availableAmount;
    }

    public void Consume(IReadOnlyList<PipeSegment> path, int amount) {
        if (amount <= 0) return;

        if (amount > GetAvailable(path)) {
            throw new System.InvalidOperationException(
                "Transfer exceeds the available pipe capacity."
            );
        }

        foreach (PipeSegment pipe in path) {
            remaining[pipe] -= amount;
        }
    }

    public int GetRemaining(PipeSegment pipe) {
        return remaining.TryGetValue(pipe, out int amount) ? amount : 0;
    }

    public void RetainPipes(IEnumerable<PipeSegment> pipes) {
        HashSet<PipeSegment> activePipes = new(pipes);
        List<PipeSegment> previousPipes = new(fractionalCarry.Keys);

        foreach (PipeSegment pipe in previousPipes) {
            if (!activePipes.Contains(pipe)) {
                fractionalCarry.Remove(pipe);
            }
        }
    }
}
