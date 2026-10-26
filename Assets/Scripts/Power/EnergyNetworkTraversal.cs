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
}
