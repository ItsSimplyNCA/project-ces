using System.Collections.Generic;
using UnityEngine;

public static class EnergyRouteTraversal {
    private static readonly PipeConnections[] Faces = {
        PipeConnections.Left,
        PipeConnections.Right,
        PipeConnections.Front,
        PipeConnections.Back,
        PipeConnections.Top,
        PipeConnections.Bottom
    };

    public static EnergyRoute FindRoute(
        EnergyCableSegment start,
        EnergyCableSegment target,
        GridWorld gridWorld,
        EnergyTransferBudget transferBudget = null,
        bool allowStorageTansit = false
    ) {
        if (start == null || target == null || gridWorld == null || start.Definition == null || target.Definition == null) return null;
        if (start.GetComponentInParent<GridWorld>() != gridWorld || target.GetComponentInParent<GridWorld>() != gridWorld) return null;

        bool HasCapacity(EnergyCableSegment cable) {
            return transferBudget == null || transferBudget.GetRemaining(cable) > 0f;
        }

        if (!HasCapacity(start) || !HasCapacity(target)) return null;

        Dictionary<EnergyCableSegment, (EnergyCableSegment Parent, PowerBreaker Breaker, PowerStorage Storage)> parents = new();
        Queue<EnergyCableSegment> pending = new();

        parents.Add(start, (null, null, null));
        pending.Enqueue(start);

        while (pending.Count > 0) {
            EnergyCableSegment current = pending.Dequeue();

            if (current == target) {
                List<EnergyCableSegment> cables = new();
                List<PowerBreaker> breakers = new();
                List<PowerStorage> storages = new();

                while (current != null) {
                    cables.Add(current);

                    var step = parents[current];

                    if (step.Breaker != null) {
                        breakers.Add(step.Breaker);
                    }

                    if (step.Storage != null) {
                        storages.Add(step.Storage);
                    }

                    current = step.Parent;
                }

                cables.Reverse();
                breakers.Reverse();
                storages.Reverse();

                return new EnergyRoute(cables, breakers, storages);
            }

            foreach (var step in GetNextSteps(current, gridWorld, allowStorageTansit)) {
                EnergyCableSegment next = step.Cable;

                if (!HasCapacity(next)) continue;
                if (parents.ContainsKey(next)) continue;

                parents.Add(next, (current, step.Breaker, step.Storage));
                pending.Enqueue(next);
            }
        }

        return null;
    }

    private static IEnumerable<(EnergyCableSegment Cable, PowerBreaker Breaker, PowerStorage Storage)> GetNextSteps(
        EnergyCableSegment current,
        GridWorld gridWorld,
        bool allowStorageTansit
    ) {
        foreach (PipeConnections face in Faces) {
            if (!current.HasConnection(face)) continue;

            Vector3Int neighborCell = current.GridCell + PipeSegment.GetOffset(face);

            if (!gridWorld.TryGetBuildingAt(neighborCell, out BuildingInstance building)) continue;

            EnergyCableSegment neighbor = building.GetComponent<EnergyCableSegment>();

            if (neighbor != null && neighbor.Definition != null) {
                if (neighbor.HasConnection(PipeSegment.GetOpposite(face))) {
                    yield return (neighbor, null, null);
                }

                continue;
            }

            UniversalIO universalIO = building.GetComponent<UniversalIO>();

            if (universalIO == null) continue;

            PowerBreaker breaker = building.GetComponent<PowerBreaker>();
            PowerStorage storage = building.GetComponent<PowerStorage>();

            if (breaker != null) {
                if (!breaker.ConductsPower) continue;
                storage = null;
            } else if (storage == null || !allowStorageTansit) continue;

            bool hasConnectedInput = false;

            foreach (UniversalPort input in universalIO.Ports) {
                if (!input.ConnectionEnabled) continue;
                if (input.Mode != PortMode.EnergyInput) continue;
                if (universalIO.GetAdjacentCell(input, gridWorld) != current.GridCell) continue;

                Vector3Int directionToBreaker = Vector3Int.RoundToInt(-universalIO.GetWorldDirection(input));

                if (PipeSegment.GetFaceFromOffset(directionToBreaker) != face) continue;

                hasConnectedInput = true;
                break;
            }

            if (!hasConnectedInput) continue;

            foreach (UniversalPort output in universalIO.Ports) {
                if (!output.ConnectionEnabled) continue;
                if (output.Mode != PortMode.EnergyOutput) continue;

                Vector3Int outputCell = universalIO.GetAdjacentCell(output, gridWorld);

                if (!gridWorld.TryGetBuildingAt(outputCell, out BuildingInstance outputBuilding)) continue; 

                EnergyCableSegment outputCable = outputBuilding.GetComponent<EnergyCableSegment>();

                if (outputCable == null || outputCable.Definition == null) continue;

                Vector3Int directionToBuilding = Vector3Int.RoundToInt(-universalIO.GetWorldDirection(output));
                PipeConnections outputCableFace = PipeSegment.GetFaceFromOffset(directionToBuilding);

                if (!outputCable.HasConnection(outputCableFace)) continue;

                yield return (outputCable, breaker, storage);
            }
        }
    }
}
