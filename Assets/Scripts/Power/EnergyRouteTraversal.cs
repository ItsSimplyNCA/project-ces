using System.Collections.Generic;
using System.Linq;
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
        EnergyTransferBudget transferBudget = null
    ) {
        if (start == null || target == null || gridWorld == null || start.Definition == null || target.Definition == null) return null;
        if (start.GetComponentInParent<GridWorld>() != gridWorld || target.GetComponentInParent<GridWorld>() != gridWorld) return null;

        bool HasCapacity(EnergyCableSegment cable) {
            return transferBudget == null || transferBudget.GetRemaining(cable) > 0f;
        }

        if (!HasCapacity(start) || !HasCapacity(target)) return null;

        Dictionary<EnergyCableSegment, EnergyCableSegment> parents = new();
        Dictionary<EnergyCableSegment, PowerBreaker> crossedBreakers = new();
        Queue<EnergyCableSegment> pending = new();

        parents.Add(start, null);
        pending.Enqueue(start);

        while (pending.Count > 0) {
            EnergyCableSegment current = pending.Dequeue();

            if (current == target) {
                List<EnergyCableSegment> cables = new();
                List<PowerBreaker> breakers = new();

                while (current != null) {
                    cables.Add(current);

                    if (crossedBreakers.TryGetValue(current, out PowerBreaker breaker) && breaker != null) {
                        breakers.Add(breaker);
                    }

                    current = parents[current];
                }

                cables.Reverse();
                breakers.Reverse();

                return new EnergyRoute(cables, breakers);
            }

            foreach (var step in GetNextSteps(current, gridWorld)) {
                EnergyCableSegment next = step.Cable;

                if (!HasCapacity(next)) continue;
                if (parents.ContainsKey(next)) continue;

                parents.Add(next, current);
                crossedBreakers.Add(next, step.Breaker);
                pending.Enqueue(next);
            }
        }

        return null;
    }

    private static IEnumerable<(EnergyCableSegment Cable, PowerBreaker Breaker)> GetNextSteps(EnergyCableSegment current, GridWorld gridWorld) {
        foreach (PipeConnections face in Faces) {
            if (!current.HasConnection(face)) continue;

            Vector3Int neighborCell = current.GridCell + PipeSegment.GetOffset(face);

            if (!gridWorld.TryGetBuildingAt(neighborCell, out BuildingInstance building)) continue;

            EnergyCableSegment neighbor = building.GetComponent<EnergyCableSegment>();

            if (neighbor != null && neighbor.Definition != null) {
                if (neighbor.HasConnection(PipeSegment.GetOpposite(face))) {
                    yield return (neighbor, null);
                }

                continue;
            }

            PowerBreaker breaker = building.GetComponent<PowerBreaker>();
            UniversalIO universalIO = building.GetComponent<UniversalIO>();

            if (breaker == null || !breaker.ConductsPower || universalIO == null) continue;

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

                Vector3Int directionToBreaker = Vector3Int.RoundToInt(-universalIO.GetWorldDirection(output));
                PipeConnections outputCableFace = PipeSegment.GetFaceFromOffset(directionToBreaker);

                if (!outputCable.HasConnection(outputCableFace)) continue;

                yield return (outputCable, breaker);
            }
        }
    }
}
