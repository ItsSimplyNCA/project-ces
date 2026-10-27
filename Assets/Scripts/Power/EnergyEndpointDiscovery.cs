using System.Collections.Generic;
using UnityEngine;

public static class EnergyEndpointDiscovery {
    private static readonly PipeConnections[] Faces = {
        PipeConnections.Left,
        PipeConnections.Right,
        PipeConnections.Front,
        PipeConnections.Back,
        PipeConnections.Top,
        PipeConnections.Bottom
    };

    public static List<EnergyEndpoint> CollectConnectedEndpoints(EnergyCableSegment start, GridWorld gridWorld) {
        List<EnergyEndpoint> endpoints = new();

        if (start == null || start.Definition == null || gridWorld == null) return endpoints;

        var network = EnergyNetworkTraversal.CollectConnected(start, gridWorld);

        foreach (EnergyCableSegment cable in network) {
            foreach (PipeConnections face in Faces) {
                if (!cable.HasConnection(face)) continue;

                Vector3Int neighborCell = cable.GridCell + PipeSegment.GetOffset(face);

                if (!gridWorld.TryGetBuildingAt(neighborCell, out BuildingInstance building)) continue;

                UniversalIO universalIO = building.GetComponent<UniversalIO>();

                if (universalIO == null) continue;

                foreach (UniversalPort port in universalIO.Ports) {
                    if (!port.ConnectionEnabled) continue;
                    if (port.Mode != PortMode.EnergyInput && port.Mode != PortMode.EnergyOutput) continue;
                    if (universalIO.GetAdjacentCell(port, gridWorld) != cable.GridCell) continue;

                    Vector3Int directionToMachine = Vector3Int.RoundToInt(-universalIO.GetWorldDirection(port));

                    if (PipeSegment.GetFaceFromOffset(directionToMachine) != face) continue;

                    endpoints.Add(new EnergyEndpoint(universalIO, port, cable));
                }
            }
        }

        return endpoints;
    }
}
