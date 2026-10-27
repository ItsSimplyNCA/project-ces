using UnityEngine;

[RequireComponent(typeof(BuildingInstance))]
public class EnergyCableSegment : MonoBehaviour {
    [SerializeField] private EnergyCableDefinition definition;
    [SerializeField] private PipeConnections connections = PipeConnections.None;

    private BuildingInstance buildingInstance;

    public EnergyCableDefinition Definition => definition;
    public Vector3Int GridCell => buildingInstance.Origin;
    public PipeConnections Connections => connections;

    public event System.Action ConnectionsChanged;

    private void Awake() {
        buildingInstance = GetComponent<BuildingInstance>();
    }

    public bool HasConnection(PipeConnections face) {
        return face != PipeConnections.None && (connections & face) == face;
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

    public bool TryToggleConnectionTo(EnergyCableSegment other, out bool connected) {
        connected = false;

        if (other == null || other == this) return false;
        if (definition == null || other.Definition == null) return false;

        GridWorld gridWorld = GetComponentInParent<GridWorld>();

        if (gridWorld == null || other.GetComponentInParent<GridWorld>() != gridWorld) return false;

        Vector3Int offset = other.GridCell - GridCell;
        PipeConnections face = PipeSegment.GetFaceFromOffset(offset);

        if (face == PipeConnections.None) return false;

        PipeConnections opposite = PipeSegment.GetOpposite(face);

        bool alreadyConnected = HasConnection(face) && other.HasConnection(opposite);

        connected = !alreadyConnected;

        SetConnection(face, connected);
        other.SetConnection(opposite, connected);

        gridWorld.NotifyLogisticsChanged();

        return true;
    }

    public bool TryTogglePortConnection(UniversalIO universalIO, GridWorld gridWorld, out bool connected) {
        connected = false;

        if (universalIO == null || gridWorld == null || definition == null) return false;
        if (GetComponentInParent<GridWorld>() != gridWorld || universalIO.GetComponentInParent<GridWorld>() != gridWorld) return false;

        foreach (UniversalPort port in universalIO.Ports) {
            if (universalIO.GetAdjacentCell(port, gridWorld) != GridCell) continue;

            Vector3Int directionToMachine = Vector3Int.RoundToInt(-universalIO.GetWorldDirection(port));
            PipeConnections face = PipeSegment.GetFaceFromOffset(directionToMachine);

            if (face == PipeConnections.None) return false;

            bool alreadyConnected = HasConnection(face) && port.ConnectionEnabled;

            connected = !alreadyConnected;

            SetConnection(face, connected);
            port.SetConnectionEnabled(connected);

            gridWorld.NotifyLogisticsChanged();

            return true;
        }

        return false;
    }

    public void RestoreConnections(PipeConnections savedConnections) {
        if (connections == savedConnections) return;

        connections = savedConnections;
        ConnectionsChanged?.Invoke();
    }

    [ContextMenu("Debug/Print Energy Network Size")]
    private void PrintEnergyNetworkSizeForDebug() {
        if (!Application.isPlaying) return;

        GridWorld gridWorld = GetComponentInParent<GridWorld>();

        if (gridWorld == null) return;

        var network = EnergyNetworkTraversal.CollectConnected(this, gridWorld);

        Debug.Log($"Connected energy cable segments: {network.Count}", this);
    }

    [ContextMenu("Debug/Print Energy Endpoints")]
    private void PrintEnergyEndpointsForDebug() {
        if (!Application.isPlaying) return;

        GridWorld gridWorld = GetComponentInParent<GridWorld>();

        if (gridWorld == null) return;

        var endpoints = EnergyEndpointDiscovery.CollectConnectedEndpoints(this, gridWorld);

        Debug.Log($"Energy endpoints: {endpoints.Count}", this);

        foreach (EnergyEndpoint endpoint in endpoints) {
            Debug.Log(
                $"{endpoint.Owner.name}: " +
                $"{endpoint.Port.LocalCell} / " +
                $"{endpoint.Port.Face} / " +
                $"{endpoint.Port.Mode}",
                endpoint.Owner
            );
        }
    }
}
