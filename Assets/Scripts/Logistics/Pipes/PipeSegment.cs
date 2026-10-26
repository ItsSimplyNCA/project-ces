using UnityEngine;

[RequireComponent(typeof(BuildingInstance))]
public class PipeSegment : MonoBehaviour {
    [SerializeField] private PipeDefinition definition;
    [SerializeField] private PipeConnections connections = PipeConnections.None;

    private BuildingInstance buildingInstance;

    public PipeDefinition Definition => definition;
    public Vector3Int GridCell => buildingInstance.Origin;
    public PipeConnections Connections => connections;
    public event System.Action ConnectionsChanged;

    private void Awake() {
        buildingInstance = GetComponent<BuildingInstance>();
    }

    public bool HasConnection(PipeConnections face) {
        return (connections & face) == face && face != PipeConnections.None;
    }

    public void SetConnection(PipeConnections face, bool enabled) {
        PipeConnections previousConnections = connections;

        if (enabled) {
            connections |= face;
        } else {
            connections &= ~face;
        }

        if (connections != previousConnections) {
            ConnectionsChanged?.Invoke();
        }
    }

    public static Vector3Int GetOffset(PipeConnections face) {
        return face switch {
            PipeConnections.Left => Vector3Int.left,
            PipeConnections.Right => Vector3Int.right,
            PipeConnections.Front => new Vector3Int(0, 0, 1),
            PipeConnections.Back => new Vector3Int(0, 0, -1),
            PipeConnections.Top => Vector3Int.up,
            PipeConnections.Bottom => Vector3Int.down,
            _ => throw new System.ArgumentOutOfRangeException(nameof(face))
        };
    }

    public static PipeConnections GetOpposite(PipeConnections face) {
        return face switch {
            PipeConnections.Left => PipeConnections.Right,
            PipeConnections.Right => PipeConnections.Left,
            PipeConnections.Front => PipeConnections.Back,
            PipeConnections.Back => PipeConnections.Front,
            PipeConnections.Top => PipeConnections.Bottom,
            PipeConnections.Bottom => PipeConnections.Top,
            _ => throw new System.ArgumentOutOfRangeException(nameof(face))
        };
    }

    public static PipeConnections GetFaceFromOffset(Vector3Int offset) {
        return (offset.x, offset.y, offset.z) switch {
            (-1, 0, 0) => PipeConnections.Left,
            (1, 0, 0) => PipeConnections.Right,
            (0, 0, 1) => PipeConnections.Front,
            (0, 0, -1) => PipeConnections.Back,
            (0, 1, 0) => PipeConnections.Top,
            (0, -1, 0) => PipeConnections.Bottom,
            _ => PipeConnections.None
        };
    }

    public bool TryConnectTo(PipeSegment other) {
        if (other == null || other == this) return false;
        if (definition == null || other.Definition == null) return false;
        if (definition.Medium != other.Definition.Medium) return false;

        Vector3Int offset = other.GridCell - GridCell;
        PipeConnections face = GetFaceFromOffset(offset);

        if (face == PipeConnections.None) return false;

        SetConnection(face, true);
        other.SetConnection(GetOpposite(face), true);

        GetComponentInParent<GridWorld>()?.NotifyLogisticsChanged();

        return true;
    }

    public bool TryToggleConnectionTo(PipeSegment other, out bool connected) {
        connected = false;
        if (other == null || other == this) return false;

        Vector3Int offset = other.GridCell - GridCell;
        PipeConnections face = GetFaceFromOffset(offset);

        if (face == PipeConnections.None) return false;

        PipeConnections opposite = GetOpposite(face);

        bool alreadyConnected = HasConnection(face) && other.HasConnection(opposite);

        if (alreadyConnected) {
            SetConnection(face, false);
            other.SetConnection(opposite, false);

            GetComponentInParent<GridWorld>()?.NotifyLogisticsChanged();

            return true;
        }

        connected = TryConnectTo(other);

        return connected;
    }

    public bool TryTogglePortConnection(UniversalIO universalIO, GridWorld gridWorld, out bool connected) {
        connected = false;

        if (universalIO == null || gridWorld == null || definition == null) return false;
        if (universalIO.GetComponentInParent<GridWorld>() != gridWorld) return false;

        foreach (UniversalPort port in universalIO.Ports) {
            if (universalIO.GetAdjacentCell(port, gridWorld) != GridCell) continue;

            Vector3Int offset = Vector3Int.RoundToInt(
                -universalIO.GetWorldDirection(port)
            );

            PipeConnections face = GetFaceFromOffset(offset);

            if (face == PipeConnections.None) return false;

            bool alreadyConnected = HasConnection(face) && port.ConnectionEnabled;

            connected = !alreadyConnected;

            SetConnection(face, connected);
            port.SetConnectionEnabled(connected);

            GetComponentInParent<GridWorld>()?.NotifyLogisticsChanged();

            return true;
        }

        return false;
    }

    public void RestoreConnections(PipeConnections savedConnections) {
        if (connections == savedConnections) return;

        connections = savedConnections;
        ConnectionsChanged?.Invoke();
    }

    [ContextMenu("Debug/Connect Right Neighbor")]
    private void ConnectRightNeighborForDebug() {
        if (!Application.isPlaying) return;

        GridWorld gridWorld = GetComponentInParent<GridWorld>();

        if (gridWorld == null) return;

        Vector3Int neighborCell = GridCell + Vector3Int.right;

        if (!gridWorld.TryGetBuildingAt(neighborCell, out BuildingInstance neighbor)) {
            Debug.Log("No building in the right neighbor cell", this);
            return;
        }

        PipeSegment other = neighbor.GetComponent<PipeSegment>();

        bool connected = TryConnectTo(other);

        Debug.Log($"Pipe connection created: {connected}", this);
    }

    [ContextMenu("Debug/Print Network Size")]
    private void PrintNetworkSizeForDebug() {
        if (!Application.isPlaying) return;

        GridWorld gridWorld = GetComponentInParent<GridWorld>();

        if (gridWorld == null) return;

        var network = PipeNetworkTraversal.CollectConnected(this, gridWorld);

        Debug.Log($"Connected pipe segments: {network.Count}", this);
    }

    [ContextMenu("Debug/Print Item Endpoints")]
    private void PrintItemEndpointsForDebug() {
        if (!Application.isPlaying) return;

        GridWorld gridWorld = GetComponentInParent<GridWorld>();

        if (gridWorld == null) return;

        var endpoints = ItemPipeEndpointDiscovery.CollectConnectedEndpoints(this, gridWorld);

        Debug.Log($"Item endpoints: {endpoints.Count}", this);

        foreach (ItemPipeEndpoint endpoint in endpoints) {
            Debug.Log(
                $"{endpoint.Owner.name}: " +
                $"{endpoint.Port.LocalCell} / {endpoint.Port.Face} / " +
                $"{endpoint.Port.Mode}",
                endpoint.Owner
            );
        }
    }

    [ContextMenu("Debug/Print Item Routes")]
    private void PrintItemRoutesForDebug() {
        if (!Application.isPlaying) return;

        GridWorld gridWorld = GetComponentInParent<GridWorld>();

        if (gridWorld == null) return;

        var endpoints = ItemPipeEndpointDiscovery.CollectConnectedEndpoints(this, gridWorld);

        foreach (ItemPipeEndpoint source in endpoints) {
            if (source.Port.Mode != PortMode.ItemOutput) continue;

            foreach (ItemPipeEndpoint destination in endpoints) {
                if (destination.Port.Mode != PortMode.ItemInput) continue;

                var path = PipeNetworkTraversal.FindShortestPath(
                    source.Pipe,
                    destination.Pipe,
                    gridWorld
                );

                if (path.Count == 0) continue;

                float capacity = float.PositiveInfinity;

                foreach (PipeSegment segment in path) {
                    capacity = Mathf.Min(capacity, segment.Definition.TransferRate);
                }

                Debug.Log(
                    $"Route: {source.Owner.name} -> {destination.Owner.name}, " +
                    $"pipe segments: {path.Count}, " +
                    $"single-route capacity: {capacity} items/s",
                    this
                );
            }
        }
    }

    [ContextMenu("Debug/Transfer One Item")]
    private void TransferOneItemForDebug() {
        if (!Application.isPlaying) return;

        GridWorld gridWorld = GetComponentInParent<GridWorld>();

        if (gridWorld == null) return;

        var endpoints = ItemPipeEndpointDiscovery.CollectConnectedEndpoints(this, gridWorld);

        foreach (ItemPipeEndpoint source in endpoints) {
            if (source.Port.Mode != PortMode.ItemOutput) continue;

            for (int i = 0; i < source.Storage.OutputSlotCount; i++) {
                foreach (ItemPipeEndpoint destination in endpoints) {
                    if (destination.Port.Mode != PortMode.ItemInput) continue;

                    int transferredAmount = ItemPipeTransfer.TransferFromSlot(source, i, destination, 1);

                    if (transferredAmount > 0) {
                        Debug.Log("Transferred one item", this);
                        return;
                    }
                }
            }
        }

        Debug.Log("No transferable item or accepting destination", this);
    }

    [ContextMenu("Debug/Print Fluid Endpoints")]
    private void PrintFluidEndpointsForDebug() {
        if (!Application.isPlaying) return;

        GridWorld gridWorld = GetComponentInParent<GridWorld>();

        if (gridWorld == null) return;

        var endpoints = FluidPipeEndpointDiscovery.CollectConnectedEndpoints(this, gridWorld);

        Debug.Log($"Fluid endpoints: {endpoints.Count}", this);

        foreach (FluidPipeEndpoint endpoint in endpoints) {
            Debug.Log(
                $"{endpoint.Owner.name}: " +
                $"{endpoint.Port.LocalCell} / " +
                $"{endpoint.Port.Face} / " +
                $"{endpoint.Port.Mode}",
                endpoint.Owner
            );
        }
    }

    [ContextMenu("Debug/Transfer One Liter")]
    private void TransferOneLiterForDebug() {
        if (!Application.isPlaying) return;

        GridWorld gridWorld = GetComponentInParent<GridWorld>();

        if (gridWorld == null) return;

        var endpoints = FluidPipeEndpointDiscovery.CollectConnectedEndpoints(this, gridWorld);

        foreach (FluidPipeEndpoint source in endpoints) {
            if (source.Port.Mode != PortMode.FluidOutput) continue;

            for (int i = 0; i < source.Storage.FluidOutputSlotCount; i++) {
                foreach (FluidPipeEndpoint destination in endpoints) {
                    if (destination.Port.Mode != PortMode.FluidInput) continue;

                    float transferredAmount = FluidPipeTransfer.TransferFromSlot(
                        source,
                        i,
                        destination,
                        1f
                    );

                    if (transferredAmount <= 0f) continue;

                    Debug.Log(
                        $"Transferred {transferredAmount:0.###}L",
                        this
                    );

                    return;
                }
            }
        }

        Debug.Log("No transferable fluid or accepting destination", this);
    }
}
