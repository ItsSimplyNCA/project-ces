using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class FluidPipeSimulation : MonoBehaviour {
    [SerializeField] private GridWorld gridWorld;
    [SerializeField] private bool logTransfers;

    private const float TICK_DURATION = 1f;
    private const float TRANSFER_QUANTUM = 0.25f;

    private readonly FluidPipeTransferBudget transferBudget = new();
    private readonly List<List<PipeSegment>> networks = new();

    private PipeSegment[] pipes = System.Array.Empty<PipeSegment>();
    private bool networksDirty = true;
    private float elapsed;
    private int tickIndex;

    private void Awake() {
        gridWorld.LogisticsChanged += OnLogisticsChanged;
    }

    private void OnDestroy() {
        if (gridWorld != null) {
            gridWorld.LogisticsChanged -= OnLogisticsChanged;
        }
    }

    private void OnLogisticsChanged() {
        networksDirty = true;
    }

    private void Update() {
        elapsed += Time.deltaTime;

        while (elapsed >= TICK_DURATION) {
            elapsed -= TICK_DURATION;
            SimulateTick();
        }
    }

    private void RebuildNetworks() {
        pipes = gridWorld.GetComponentsInChildren<PipeSegment>();

        networks.Clear();
        HashSet<PipeSegment> visited = new();

        foreach (PipeSegment pipe in pipes) {
            if (pipe.Definition == null) continue;
            if (pipe.Definition.Medium != PipeMedium.Fluid) continue;
            if (visited.Contains(pipe)) continue;

            List<PipeSegment> network = PipeNetworkTraversal.CollectConnected(pipe, gridWorld);

            foreach (PipeSegment member in network) {
                visited.Add(member);
            }

            networks.Add(network);
        }

        networksDirty = false;
    }

    private void SimulateTick() {
        if (networksDirty) RebuildNetworks();

        transferBudget.BeginTick(pipes, TICK_DURATION);

        float totalTransferred = 0f;
        
        foreach (List<PipeSegment> network in networks) {
            totalTransferred += SimulateNetwork(network);
        }

        if (logTransfers && totalTransferred > 0f) {
            Debug.Log($"Fluid transferred: {totalTransferred:0.###}L", this);
        }

        tickIndex = (tickIndex + 1) % int.MaxValue;
    }

    private float SimulateNetwork(List<PipeSegment> network) {
        if (network.Count == 0) return 0f;

        var endpoints = FluidPipeEndpointDiscovery.CollectConnectedEndpoints(network[0], gridWorld, network);

        List<FluidPipeEndpoint> sources = new();
        List<FluidPipeEndpoint> destinations = new();

        foreach (FluidPipeEndpoint endpoint in endpoints) {
            if (endpoint.Port.Mode == PortMode.FluidOutput) {
                sources.Add(endpoint);
            } else if (endpoint.Port.Mode == PortMode.FluidInput) {
                destinations.Add(endpoint);
            }
        }

        if (sources.Count == 0 || destinations.Count == 0) return 0f;

        float totalTransferred = 0f;
        bool madeProgress;

        Dictionary<UniversalPort, FluidPipeEndpoint> roundRobinTargets = new();

        do {
            madeProgress = false;

            for (int i = 0; i < sources.Count; i++) {
                int index = (i + tickIndex % sources.Count) % sources.Count;
                float transferred = TryTransfer(sources[index], destinations, roundRobinTargets);

                if (transferred <= 0f) continue;

                totalTransferred += transferred;
                madeProgress = true;
            }
        } while (madeProgress);

        return totalTransferred;
    }

    private float TryTransfer(FluidPipeEndpoint source, List<FluidPipeEndpoint> destinations, Dictionary<UniversalPort, FluidPipeEndpoint> roundRobinTargets) {
        if (source.Owner == null) return 0f;
        if (transferBudget.GetRemaining(source.Pipe) <= 0f) return 0f;

        int slotCount = source.Storage.FluidOutputSlotCount;

        if (slotCount == 0) return 0f;

        bool roundRobin = source.Port.RoutingMode == ItemRoutingMode.RoundRobin;

        roundRobinTargets.TryGetValue(
            source.Port,
            out FluidPipeEndpoint committedTarget
        );

        List<(FluidPipeEndpoint Endpoint, int Distance, int Order)> routes = new();

        for (int i = 0; i < destinations.Count; i++) {
            FluidPipeEndpoint destination = destinations[i];

            if (destination.Owner == null) continue;
            if (source.Owner is Tank && source.Owner == destination.Owner) continue;

            List<PipeSegment> path = PipeNetworkTraversal.FindShortestPath(
                source.Pipe,
                destination.Pipe,
                gridWorld
            );

            if (path.Count == 0) continue;

            routes.Add((destination, path.Count, i));
        }

        routes.Sort((a, b) => {
            int distanceComparison = a.Distance.CompareTo(b.Distance);
            return distanceComparison != 0 ? distanceComparison : a.Order.CompareTo(b.Order);
        });

        if (routes.Count == 0) return 0f;

        int startIndex = roundRobin ? source.Port.RoundRobinDestinationCursor % routes.Count : 0;

        for (int attempt = 0; attempt < slotCount; attempt++) {
            int slotIndex = source.Port.OutputSlotCursor % slotCount;

            for (int offset = 0; offset < routes.Count; offset++) {
                int routeIndex = roundRobin ? (startIndex + offset) % routes.Count : offset;
                FluidPipeEndpoint destination = routes[routeIndex].Endpoint;

                if (roundRobin && committedTarget != null && destination != committedTarget) continue;

                List<PipeSegment> path = PipeNetworkTraversal.FindShortestPath(
                    source.Pipe,
                    destination.Pipe,
                    gridWorld,
                    fluidTransferBudget: transferBudget
                );

                if (path.Count == 0) continue;

                float requestedAmount = Mathf.Min(
                    TRANSFER_QUANTUM,
                    transferBudget.GetAvailable(path)
                );

                float transferred = FluidPipeTransfer.TransferFromSlot(
                    source,
                    slotIndex,
                    destination,
                    requestedAmount
                );

                if (transferred <= 0f) continue;

                transferBudget.Consume(path, transferred);

                if (roundRobin && committedTarget == null) {
                    roundRobinTargets[source.Port] = destination;

                    source.Port.SetRoundRobinDestinationCursor((routeIndex + 1) % routes.Count);
                }

                source.Port.AdvanceOutputSlotCursor(slotCount);

                return transferred;
            }

            source.Port.AdvanceOutputSlotCursor(slotCount);
        }

        return 0f;
    }
}
