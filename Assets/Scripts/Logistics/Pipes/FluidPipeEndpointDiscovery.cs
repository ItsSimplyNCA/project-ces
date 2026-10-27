using System.Collections.Generic;
using UnityEngine;

public static class FluidPipeEndpointDiscovery {
    private static readonly PipeConnections[] Faces = {
        PipeConnections.Left,
        PipeConnections.Right,
        PipeConnections.Front,
        PipeConnections.Back,
        PipeConnections.Top,
        PipeConnections.Bottom
    };

    public static List<FluidPipeEndpoint> CollectConnectedEndpoints(
        PipeSegment start, GridWorld gridWorld, IReadOnlyList<PipeSegment> cachedNetwork = null
    ) {
        List<FluidPipeEndpoint> endpoints = new();

        if (start == null || start.Definition == null || start.Definition.Medium != PipeMedium.Fluid || gridWorld == null) {
            return endpoints;
        }

        IReadOnlyList<PipeSegment> network = cachedNetwork ?? PipeNetworkTraversal.CollectConnected(start, gridWorld);

        foreach (PipeSegment pipe in network) {
            foreach (PipeConnections face in Faces) {
                if (!pipe.HasConnection(face)) continue;

                Vector3Int neighborCell = pipe.GridCell + PipeSegment.GetOffset(face);

                if (!gridWorld.TryGetBuildingAt(neighborCell, out BuildingInstance building)) continue;

                IFluidTransferEndpoint storage = building.GetComponent<IFluidTransferEndpoint>();

                MonoBehaviour owner = storage as MonoBehaviour;
                UniversalIO universalIO = building.GetComponent<UniversalIO>();

                if (storage == null || owner == null || universalIO == null) continue;

                foreach (UniversalPort port in universalIO.Ports) {
                    if (!port.ConnectionEnabled) continue;
                    if (port.Mode != PortMode.FluidInput && port.Mode != PortMode.FluidOutput) continue;
                    if (universalIO.GetAdjacentCell(port, gridWorld) != pipe.GridCell) continue;

                    Vector3Int directionToMachine = Vector3Int.RoundToInt(
                        -universalIO.GetWorldDirection(port)
                    );

                    if (PipeSegment.GetFaceFromOffset(directionToMachine) != face) continue;

                    endpoints.Add(new FluidPipeEndpoint(
                        storage,
                        owner,
                        port,
                        pipe
                    ));
                }
            }
        }

        return endpoints;
    }
}
