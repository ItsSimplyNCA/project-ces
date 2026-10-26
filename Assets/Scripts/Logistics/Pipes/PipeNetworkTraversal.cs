using System.Collections.Generic;
using UnityEngine;

public class PipeNetworkTraversal {
    private static readonly PipeConnections[] Faces = {
        PipeConnections.Left,
        PipeConnections.Right,
        PipeConnections.Front,
        PipeConnections.Back,
        PipeConnections.Top,
        PipeConnections.Bottom
    };

    public static List<PipeSegment> CollectConnected(PipeSegment start, GridWorld gridWorld) {
        List<PipeSegment> result = new();

        if (start == null || gridWorld == null || start.Definition == null) return result;

        HashSet<PipeSegment> visited = new();
        Queue<PipeSegment> pending = new();

        visited.Add(start);
        pending.Enqueue(start);

        while (pending.Count > 0) {
            PipeSegment current = pending.Dequeue();
            result.Add(current);

            foreach (PipeConnections face in Faces) {
                if (!current.HasConnection(face)) continue;

                Vector3Int neighborCell = current.GridCell + PipeSegment.GetOffset(face);

                if (!gridWorld.TryGetBuildingAt(neighborCell, out BuildingInstance building)) continue;

                PipeSegment neighbor = building.GetComponent<PipeSegment>();

                if (neighbor == null || neighbor.Definition == null) continue;
                if (neighbor.Definition.Medium != start.Definition.Medium) continue;

                PipeConnections opposite = PipeSegment.GetOpposite(face);

                if (!neighbor.HasConnection(opposite)) continue;

                if (visited.Add(neighbor)) {
                    pending.Enqueue(neighbor);
                }
            }
        }

        return result;
    }

    public static List<PipeSegment> FindShortestPath(
        PipeSegment start,
        PipeSegment target,
        GridWorld gridWorld,
        PipeTransferBudget transferBudget = null,
        FluidPipeTransferBudget fluidTransferBudget = null
    ) {
        if (start == null || target == null || gridWorld == null || start.Definition == null || target.Definition == null) {
            return new List<PipeSegment>();
        }

        if (start.Definition.Medium != target.Definition.Medium) {
            return new List<PipeSegment>();
        }

        bool HasCapacity(PipeSegment pipe) {
            if (transferBudget != null && transferBudget.GetRemaining(pipe) <= 0) return false;
            if (fluidTransferBudget != null && fluidTransferBudget.GetRemaining(pipe) <= 0f) return false;

            return true;
        }

        if (!HasCapacity(start) || !HasCapacity(target)) {
            return new List<PipeSegment>();
        }

        Dictionary<PipeSegment, PipeSegment> parents = new();
        Queue<PipeSegment> pending = new();

        parents.Add(start, null);
        pending.Enqueue(start);

        while (pending.Count > 0) {
            PipeSegment current = pending.Dequeue();

            if (current == target) {
                List<PipeSegment> path = new();

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

                PipeSegment neighbor = building.GetComponent<PipeSegment>();

                if (neighbor == null || neighbor.Definition == null) continue;
                if (neighbor.Definition.Medium != start.Definition.Medium) continue;
                if (!HasCapacity(neighbor)) continue;
                if (!neighbor.HasConnection(PipeSegment.GetOpposite(face))) continue;
                if (parents.ContainsKey(neighbor)) continue;

                parents.Add(neighbor, current);
                pending.Enqueue(neighbor);
            }
        }

        return new List<PipeSegment>();
    }
}
