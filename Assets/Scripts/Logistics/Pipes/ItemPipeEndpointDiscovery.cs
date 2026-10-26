using System.Collections.Generic;
using UnityEngine;

public static class ItemPipeEndpointDiscovery {
    private static readonly PipeConnections[] Faces = {
        PipeConnections.Left,
        PipeConnections.Right,
        PipeConnections.Front,
        PipeConnections.Back,
        PipeConnections.Top,
        PipeConnections.Bottom
    };

    public static List<ItemPipeEndpoint> CollectConnectedEndpoints(PipeSegment start, GridWorld gridWorld, IReadOnlyList<PipeSegment> cachedNetwork = null) {
        List<ItemPipeEndpoint> endpoints = new();

        if (start == null || start.Definition == null || start.Definition.Medium != PipeMedium.Item || gridWorld == null) {
            return endpoints;
        }

        IReadOnlyList<PipeSegment> network = cachedNetwork ?? PipeNetworkTraversal.CollectConnected(start, gridWorld);

        foreach (PipeSegment pipe in network) {
            foreach (PipeConnections face in Faces) {
                if (!pipe.HasConnection(face)) continue;

                Vector3Int neighborCell = pipe.GridCell + PipeSegment.GetOffset(face);

                if (!gridWorld.TryGetBuildingAt(neighborCell, out BuildingInstance building)) continue;

                IItemTransferEndpoint storage = building.GetComponent<IItemTransferEndpoint>();

                MonoBehaviour owner = storage as MonoBehaviour;

                UniversalIO universalIO = building.GetComponent<UniversalIO>();

                if (storage == null || owner == null || universalIO == null) continue;

                foreach (UniversalPort port in universalIO.Ports) {
                    if (!port.ConnectionEnabled) continue;
                    if (port.Mode != PortMode.ItemInput && port.Mode != PortMode.ItemOutput) continue;
                    if (universalIO.GetAdjacentCell(port, gridWorld) != pipe.GridCell) continue;

                    Vector3Int directionToMachine = Vector3Int.RoundToInt(-universalIO.GetWorldDirection(port));

                    if (PipeSegment.GetFaceFromOffset(directionToMachine) != face) continue;

                    endpoints.Add(new ItemPipeEndpoint(storage, owner, port, pipe));
                }
            }
        }

        return endpoints;
    }
}
