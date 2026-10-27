using System.Collections.Generic;
using UnityEngine;

public static class EnergyNetworkTraversal {
    private static readonly PipeConnections[] Faces = {
        PipeConnections.Left,
        PipeConnections.Right,
        PipeConnections.Front,
        PipeConnections.Back,
        PipeConnections.Top,
        PipeConnections.Bottom
    };

    public static List<EnergyCableSegment> CollectConnected(EnergyCableSegment start, GridWorld gridWorld) {
        List<EnergyCableSegment> result = new();

        if (start == null || start.Definition == null || gridWorld == null) return result;
        if (start.GetComponentInParent<GridWorld>() != gridWorld) return result;

        HashSet<EnergyCableSegment> visited = new();
        Queue<EnergyCableSegment> pending = new();

        visited.Add(start);
        pending.Enqueue(start);

        while (pending.Count > 0) {
            EnergyCableSegment current = pending.Dequeue();
            result.Add(current);

            foreach (PipeConnections face in Faces) {
                if (!current.HasConnection(face)) continue;

                Vector3Int neighborCell = current.GridCell + PipeSegment.GetOffset(face);

                if (!gridWorld.TryGetBuildingAt(neighborCell, out BuildingInstance building)) continue;

                EnergyCableSegment neighbor = building.GetComponent<EnergyCableSegment>();

                if (neighbor == null || neighbor.Definition == null) continue;
                if (!neighbor.HasConnection(PipeSegment.GetOpposite(face))) continue;
                if (visited.Add(neighbor)) {
                    pending.Enqueue(neighbor);
                }
            }
        }

        return result;
    }

    public static List<EnergyCableSegment> FindShortestPath(
        EnergyCableSegment start,
        EnergyCableSegment target,
        GridWorld gridWorld,
        EnergyTransferBudget transferBudget = null
    ) {
        if (start == null || target == null || gridWorld == null || start.Definition == null || target.Definition == null) {
            return new List<EnergyCableSegment>();
        }

        if (start.GetComponentInParent<GridWorld>() != gridWorld || target.GetComponentInParent<GridWorld>() != gridWorld) {
            return new List<EnergyCableSegment>();
        }

        bool HasCapacity(EnergyCableSegment cable) {
            return transferBudget == null || transferBudget.GetRemaining(cable) > 0f;
        }

        if (!HasCapacity(start) || !HasCapacity(target)) {
            return new List<EnergyCableSegment>();
        }

        Dictionary<EnergyCableSegment, EnergyCableSegment> parents = new();
        Queue<EnergyCableSegment> pending = new();

        parents.Add(start, null);
        pending.Enqueue(start);

        while (pending.Count > 0) {
            EnergyCableSegment current = pending.Dequeue();

            if (current == target) {
                List<EnergyCableSegment> path = new();

                while (current != null) {
                    path.Add(current);
                    current = parents[current];
                }

                path.Reverse();
                return path;
            }

            foreach (PipeConnections face in Faces) {
                if (!current.HasConnection(face)) continue;

                Vector3Int neighborCell = current.GridCell + PipeSegment.GetOffset(face);

                if (!gridWorld.TryGetBuildingAt(neighborCell, out BuildingInstance building)) continue;

                EnergyCableSegment neighbor = building.GetComponent<EnergyCableSegment>();

                if (neighbor == null || neighbor.Definition == null) continue;
                if (!neighbor.HasConnection(PipeSegment.GetOpposite(face))) continue;
                if (!HasCapacity(neighbor)) continue;
                if (parents.ContainsKey(neighbor)) continue;

                parents.Add(neighbor, current);
                pending.Enqueue(neighbor);
            }
        }

        return new List<EnergyCableSegment>();
    }
}
