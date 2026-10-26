using System.Collections.Generic;
using UnityEngine;

public class ItemPipeSimulation : MonoBehaviour {
    [SerializeField] private GridWorld gridWorld;
    [SerializeField] private bool logTickTransfers;

    private readonly PipeTransferBudget transferBudget = new();
    private readonly Dictionary<ItemPipeEndpoint, int> transferredThisTick = new();
    private float elapsed;
    private int tickIndex;
    private PipeSegment[] cachedPipes = System.Array.Empty<PipeSegment>();
    private readonly List<List<PipeSegment>> cachedNetworks = new();
    private bool networksDirty = true;

    private sealed class DestinationRoute {
        public ItemPipeEndpoint Destination { get; }
        public List<PipeSegment> Path { get; }
        public int Order { get; }

        public DestinationRoute(ItemPipeEndpoint destination, List<PipeSegment> path, int order) {
            Destination = destination;
            Path = path;
            Order = order;
        }
    }

    private sealed class SourceRoutes {
        public ItemPipeEndpoint Source { get; }
        public List<DestinationRoute> Routes { get; } = new();

        public DestinationRoute RoundRobinRoute { get; set; }

        public SourceRoutes(ItemPipeEndpoint source) {
            Source = source;
        }
    }

    private void Awake() {
        gridWorld.LogisticsChanged += OnLogisticsChanged;
    }

    private void Update() {
        elapsed += Time.deltaTime;

        while (elapsed >= 1f) {
            elapsed -= 1f;
            SimulateTick();
        }
    }

    private void OnDestroy() {
        if (gridWorld != null) {
            gridWorld.LogisticsChanged -= OnLogisticsChanged;
        }
    }

    private void OnLogisticsChanged() {
        networksDirty = true;
    }

    private void SimulateTick() {
        transferredThisTick.Clear();
        
        if (networksDirty) {
            RebuildNetworks();
            networksDirty = false;
        }

        transferBudget.BeginTick(cachedPipes, 1f);

        foreach (var network in cachedNetworks) {
            SimulateNetwork(network);
        }

        if (logTickTransfers) {
            foreach (var entry in transferredThisTick) {
                ItemPipeEndpoint source = entry.Key;

                Debug.Log(
                    $"Output at pipe {source.Pipe.GridCell}, " +
                    $"port {source.Port.LocalCell}/{source.Port.Face}: " +
                    $"{entry.Value} items/tick",
                    source.Owner
                );
            }
        }

        tickIndex++;
    }

    private void SimulateNetwork(IReadOnlyList<PipeSegment> network) {
        var endpoints = ItemPipeEndpointDiscovery.CollectConnectedEndpoints(network[0], gridWorld, network);

        List<ItemPipeEndpoint> sources = new();
        List<ItemPipeEndpoint> destinations = new();

        foreach (ItemPipeEndpoint endpoint in endpoints) {
            if (endpoint.Port.Mode == PortMode.ItemOutput) {
                sources.Add(endpoint);
            } else if (endpoint.Port.Mode == PortMode.ItemInput) {
                destinations.Add(endpoint);
            }
        }

        List<SourceRoutes> sourceRoutes = new();

        foreach (ItemPipeEndpoint source in sources) {
            SourceRoutes routes = new(source);

            for (int i = 0; i < destinations.Count; i++) {
                ItemPipeEndpoint destination = destinations[i];

                var path = PipeNetworkTraversal.FindShortestPath(
                    source.Pipe,
                    destination.Pipe,
                    gridWorld
                );

                if (path.Count > 0) {
                    routes.Routes.Add(new DestinationRoute(destination, path, i));
                }
            }

            routes.Routes.Sort((a, b) => {
                int comparison = a.Path.Count.CompareTo(b.Path.Count);

                return comparison != 0
                    ? comparison
                    : a.Order.CompareTo(b.Order);
            });

            if (routes.Routes.Count > 0) {
                sourceRoutes.Add(routes);
            }
        }

        if (sourceRoutes.Count == 0) return;

        bool transferred;

        do {
            transferred = false;

            for (int i = 0; i < sourceRoutes.Count; i++) {
                int index = (tickIndex + i) % sourceRoutes.Count;

                if (TryTransferOne(sourceRoutes[index])) {
                    transferred = true;
                }
            }
        } while (transferred);
    }

    private bool TryTransferOne(SourceRoutes sourceRoutes) {
        ItemPipeEndpoint source = sourceRoutes.Source;

        if (transferBudget.GetRemaining(source.Pipe) == 0) return false;

        int slotCount = source.Storage.OutputSlotCount;

        for (int i = 0; i < slotCount; i++) {
            int slotIndex = source.Port.OutputSlotCursor;

            if (source.Storage.GetOutputSlotItem(slotIndex) == null) {
                source.Port.AdvanceOutputSlotCursor(slotCount);
                continue;
            }

            foreach (DestinationRoute route in GetCandidateRoutes(sourceRoutes)) {
                List<PipeSegment> path = route.Path;

                if (transferBudget.GetAvailable(path) == 0) {
                    path = PipeNetworkTraversal.FindShortestPath(
                        source.Pipe,
                        route.Destination.Pipe,
                        gridWorld,
                        transferBudget
                    );

                    if (path.Count == 0) continue;
                }

                int transferredAmount = ItemPipeTransfer.TransferFromSlot(
                    source,
                    slotIndex,
                    route.Destination,
                    1
                );

                if (transferredAmount == 0) continue;

                transferBudget.Consume(path, transferredAmount);

                transferredThisTick.TryGetValue(source, out int totalAmount);
                transferredThisTick[source] = totalAmount + transferredAmount;

                if (source.Port.RoutingMode == ItemRoutingMode.RoundRobin && sourceRoutes.RoundRobinRoute == null) {
                    sourceRoutes.RoundRobinRoute = route;

                    int index = sourceRoutes.Routes.IndexOf(route);

                    source.Port.SetRoundRobinDestinationCursor((index + 1) % sourceRoutes.Routes.Count);
                }

                source.Port.AdvanceOutputSlotCursor(slotCount);

                return true;
            }

            source.Port.AdvanceOutputSlotCursor(slotCount);
        }

        return false;
    }

    private void RebuildNetworks() {
        cachedNetworks.Clear();

        cachedPipes = gridWorld.GetComponentsInChildren<PipeSegment>();

        HashSet<PipeSegment> visited = new();

        foreach (PipeSegment pipe in cachedPipes) {
            if (visited.Contains(pipe) || pipe.Definition == null || pipe.Definition.Medium != PipeMedium.Item) continue;

            var network = PipeNetworkTraversal.CollectConnected(pipe, gridWorld);

            foreach (PipeSegment segment in network) {
                visited.Add(segment);
            }

            cachedNetworks.Add(network);
        }

        transferBudget.RetainPipes(cachedPipes);
    }

    private IEnumerable<DestinationRoute> GetCandidateRoutes(SourceRoutes sourceRoutes) {
        if (sourceRoutes.Source.Port.RoutingMode != ItemRoutingMode.RoundRobin) {
            foreach (DestinationRoute route in sourceRoutes.Routes) {
                yield return route;
            }

            yield break;
        }

        if (sourceRoutes.RoundRobinRoute != null) {
            yield return sourceRoutes.RoundRobinRoute;
            yield break;
        }

        int count = sourceRoutes.Routes.Count;

        if (count == 0) yield break;

        int starterIndex = sourceRoutes.Source.Port.RoundRobinDestinationCursor % count;

        for (int i = 0; i < count; i++) {
            yield return sourceRoutes.Routes[(starterIndex + i) % count];
        }
    }
}
