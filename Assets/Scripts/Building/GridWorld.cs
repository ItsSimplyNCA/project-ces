using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class GridWorld : MonoBehaviour {
    [SerializeField] private float cellSize = 1f;

    private readonly Dictionary<Vector3Int, GameObject> occupiedCells = new();
    private readonly Dictionary<Vector3Int, PipeConnections> pipeConnectionMemory = new();
    private readonly HashSet<(Vector3Int Cell, Vector3Int Direction)> portConnectionMemory = new();
    private readonly Dictionary<Vector3Int, PipeConnections> energyCableConnectionMemory = new();

    public event System.Action LogisticsChanged;

    public float CellSize => cellSize;

    public Vector3Int WorldToCell(Vector3 position) {
        return new Vector3Int(
            Mathf.FloorToInt(position.x / cellSize),
            Mathf.FloorToInt(position.y / cellSize),
            Mathf.FloorToInt(position.z / cellSize)
        );
    }

    public Vector3 CellToWorldCorner(Vector3Int cell) {
        return (Vector3)cell * cellSize;
    }

    public bool CanPlace(Vector3Int origin, BuildingDefinition definition, int quarterTurns) {
        Vector3Int size = GetRotatedSize(definition, quarterTurns);

        for (int x = 0; x < size.x; x++) {
            for (int y = 0; y < size.y; y++) {
                for (int z = 0; z < size.z; z++) {
                    Vector3Int cell = origin + new Vector3Int(x, y, z);

                    if (occupiedCells.ContainsKey(cell)) {
                        return false;
                    }
                }
            }
        }

        if (definition.RequiresSupport) {
            for (int x = 0; x < size.x; x++) {
                for (int z = 0; z < size.z; z++) {
                    Vector3Int baseCell = origin + new Vector3Int(x, 0, z);

                    if (!HasSupport(baseCell)) {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    public bool TryPlace(Vector3Int origin, BuildingDefinition definition, int quarterTurns) {
        if (!CanPlace(origin, definition, quarterTurns)) {
            return false;
        }

        GameObject instance = Instantiate(
            definition.Prefab,
            GetPlacementPosition(origin, definition, quarterTurns),
            GetRotation(quarterTurns),
            transform
        );

        BuildingInstance building = instance.GetComponent<BuildingInstance>();
        building.Initialize(definition, origin, quarterTurns);

        UniversalIO universalIO = instance.GetComponent<UniversalIO>();

        if (universalIO != null) {
            universalIO.Initialize(definition.Size, cellSize);
        }

        Vector3Int size = GetRotatedSize(definition, quarterTurns);

        for (int x = 0; x < size.x; x++) {
            for (int y = 0; y < size.y; y++) {
                for (int z = 0; z < size.z; z++) {
                    Vector3Int cell = origin + new Vector3Int(x, y, z);
                    occupiedCells.Add(cell, instance);
                }
            }
        }

        PipeSegment pipe = instance.GetComponent<PipeSegment>();

        if (pipe != null && pipeConnectionMemory.TryGetValue(origin, out PipeConnections savedConnections)) {
            pipe.RestoreConnections(savedConnections);
        }

        EnergyCableSegment cable = instance.GetComponent<EnergyCableSegment>();

        if (cable != null && energyCableConnectionMemory.TryGetValue(origin, out PipeConnections savedCableConnections)) {
            cable.RestoreConnections(savedCableConnections);
        }

        if (universalIO != null) {
            foreach (UniversalPort port in universalIO.Ports) {
                var key = GetPortConnectionKey(universalIO, port);

                port.SetConnectionEnabled(portConnectionMemory.Contains(key));
            }
        }

        NotifyLogisticsChanged();

        return true;
    }

    public bool TryGetBuildingAt(Vector3Int cell, out BuildingInstance building) {
        building = null;

        if (!occupiedCells.TryGetValue(cell, out GameObject instance)) return false;

        building = instance.GetComponent<BuildingInstance>();

        return building != null;
    }

    public bool HasSupport(Vector3Int cell) {
        Vector3 center = CellToWorldCorner(cell) + Vector3.one * (cellSize * 0.5f);

        float halfSize = cellSize * 0.5f;
        float tolerance = cellSize * 0.01f;

        bool hasHit = Physics.Raycast(
            center,
            Vector3.down,
            out RaycastHit hit,
            halfSize + tolerance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore
        );

        if (!hasHit) {
            return false;
        }

        if (hit.collider.GetComponentInParent<BuildingInstance>() != null) {
            return false;
        }

        float expectedSurfaceHeight = center.y - halfSize;

        return Mathf.Abs(hit.point.y - expectedSurfaceHeight) <= tolerance;
    }

    public Vector3Int GetRotatedSize(BuildingDefinition definition, int quarterTurns) {
        Vector3Int size = definition.Size;

        return quarterTurns % 2 == 0 ? size : new Vector3Int(size.z, size.y, size.x);
    }

    public Quaternion GetRotation(int quarterTurns) {
        return Quaternion.Euler(0f, quarterTurns * 90f, 0f);
    }

    public Vector3 GetPlacementPosition(Vector3Int origin, BuildingDefinition definition, int quarterTurns) {
        Vector3Int size = definition.Size;

        Vector3 offset = quarterTurns switch {
            0 => Vector3.zero,
            1 => new Vector3(0f, 0f, size.x),
            2 => new Vector3(size.x, 0f, size.z),
            3 => new Vector3(size.z, 0f, 0f),
            _ => throw new System.ArgumentOutOfRangeException(nameof(quarterTurns))
        };

        return CellToWorldCorner(origin) + offset * cellSize;
    }

    public bool TryRemoveBuilding(BuildingInstance building) {
        if (building == null) return false;

        PipeSegment pipe = building.GetComponent<PipeSegment>();

        if (pipe != null) {
            pipeConnectionMemory[building.Origin] = pipe.Connections;
        }

        EnergyCableSegment cable = building.GetComponent<EnergyCableSegment>();

        if (cable != null) {
            energyCableConnectionMemory[building.Origin] = cable.Connections;
        }

        UniversalIO removedUniversalIO = building.GetComponent<UniversalIO>();

        if (removedUniversalIO != null) {
            foreach (UniversalPort port in removedUniversalIO.Ports) {
                var key = GetPortConnectionKey(removedUniversalIO, port);

                if (port.ConnectionEnabled) {
                    portConnectionMemory.Add(key);
                } else {
                    portConnectionMemory.Remove(key);
                }
            }
        }

        Vector3Int size = GetRotatedSize(building.Definition, building.QuarterTurns);

        for (int x = 0; x < size.x; x++) {
            for (int y = 0; y < size.y; y++) {
                for (int z = 0; z < size.z; z++) {
                    Vector3Int cell = building.Origin + new Vector3Int(x, y, z);
                    occupiedCells.Remove(cell);
                }
            }
        }

        building.gameObject.SetActive(false);
        Destroy(building.gameObject);

        NotifyLogisticsChanged();

        return true;
    }

    public bool CanRemoveBuilding(BuildingInstance building) {
        Vector3Int size = GetRotatedSize(building.Definition, building.QuarterTurns);

        for (int x = 0; x < size.x; x++) {
            for (int z = 0; z < size.z; z++) {
                Vector3Int cellAbove = building.Origin + new Vector3Int(x, size.y, z);

                if (occupiedCells.ContainsKey(cellAbove)) {
                    return false;
                }
            }
        }

        return true;
    }

    public void NotifyLogisticsChanged() {
        LogisticsChanged?.Invoke();
    }

    private (Vector3Int Cell, Vector3Int Direction) GetPortConnectionKey(UniversalIO universalIO, UniversalPort port) {
        Vector3 position = universalIO.GetWorldPosition(port);
        Vector3 direction = universalIO.GetWorldDirection(port);

        Vector3 machineCellCenter = position - direction * (cellSize * 0.5f);

        return (WorldToCell(machineCellCenter), Vector3Int.RoundToInt(direction));
    }

    public WorldSaveData CaptureWorldLayout() {
        WorldSaveData data = new WorldSaveData{
            cellSize = cellSize
        };

        HashSet<GameObject> capturedInstances = new();

        foreach (GameObject instance in occupiedCells.Values) {
            if (instance == null || !capturedInstances.Add(instance)) {
                continue;
            }

            BuildingInstance building = instance.GetComponent<BuildingInstance>();

            if (building == null || building.Definition == null) {
                throw new System.InvalidOperationException("Registered building has no valid BuildingInstance.");
            }

            if (string.IsNullOrWhiteSpace(building.Definition.BuildingId)) {
                throw new System.InvalidOperationException($"Missing building ID: {building.Definition.name}");
            }

            data.buildings.Add(CaptureBuildingData(building));
        }

        data.buildings.Sort((first, second) => {
            int comparison = first.origin.x.CompareTo(second.origin.x);

            if (comparison != 0) return comparison;

            comparison = first.origin.y.CompareTo(second.origin.y);

            return comparison != 0 ? comparison : first.origin.z.CompareTo(second.origin.z);
        });

        return data;
    }

    public bool ValidateWorldLayoutData(WorldSaveData data, GameDefinitionCatalog catalog) {
        bool Fail(string message) {
            Debug.LogWarning($"World layout validation failed: {message}", this);
            return false;
        }

        if (data == null || data.buildings == null) {
            return Fail("Missing world layout data.");
        }

        if (catalog == null || !catalog.IsValid) {
            return Fail("Invalid definition catalog.");
        }

        if (float.IsNaN(data.cellSize) || float.IsInfinity(data.cellSize) || data.cellSize <= 0f || !Mathf.Approximately(data.cellSize, cellSize)) {
            return Fail("Grid cell size does not match.");
        }

        HashSet<Vector3Int> savedOccupiedCells = new();

        foreach (BuildingSaveData savedBuilding in data.buildings) {
            if (savedBuilding == null) {
                return Fail("Missing building entry.");
            }

            if (savedBuilding.quarterTurns < 0 || savedBuilding.quarterTurns > 3) {
                return Fail("Invalid building rotation.");
            }

            if (!catalog.TryGetBuilding(savedBuilding.buildingId, out BuildingDefinition definition)) {
                return Fail($"Unknown building ID: {savedBuilding.buildingId}");
            }

            if (definition.Prefab == null || definition.Prefab.GetComponent<BuildingInstance>() == null) {
                return Fail($"Invalid building prefab: {definition.name}");
            }

            Vector3Int definitionSize = definition.Size;

            if (definitionSize.x <= 0 || definitionSize.y <= 0 || definitionSize.z <= 0) {
                return Fail($"Invalid building size: {definition.name}");
            }

            if (!ValidateBuildingConnections(savedBuilding, definition)) {
                return false;
            }

            Vector3Int size = GetRotatedSize(definition, savedBuilding.quarterTurns);
            Vector3Int origin = savedBuilding.origin;

            if ((long)origin.x + size.x - 1 > int.MaxValue || (long)origin.y + size.y - 1 > int.MaxValue || (long)origin.z + size.z - 1 > int.MaxValue) {
                return Fail("Building coordinates exceed the grid range.");
            }

            for (int x = 0; x < size.x; x++) {
                for (int y = 0; y < size.y; y++) {
                    for (int z = 0; z < size.z; z++) {
                        Vector3Int cell = origin + new Vector3Int(x, y, z);

                        if (!savedOccupiedCells.Add(cell)) {
                            return Fail($"Overlapping buildings at cell {cell}.");
                        }
                    }
                }
            }
        }

        return true;
    }

    private BuildingSaveData CaptureBuildingData(BuildingInstance building) {
        BuildingSaveData data = new BuildingSaveData {
            buildingId = building.Definition.BuildingId,
            origin = building.Origin,
            quarterTurns = building.QuarterTurns
        };

        PipeSegment pipe = building.GetComponent<PipeSegment>();
        EnergyCableSegment cable = building.GetComponent<EnergyCableSegment>();

        if (pipe != null) {
            data.connections = pipe.Connections;
        } else if (cable != null) {
            data.connections = cable.Connections;
        }

        UniversalIO universalIO = building.GetComponent<UniversalIO>();

        if (universalIO != null) {
            foreach (UniversalPort port in universalIO.Ports) {
                data.ports.Add(new UniversalPortSaveData {
                    localCell = port.LocalCell,
                    face = port.Face,
                    mode = port.Mode,
                    connectionEnabled = port.ConnectionEnabled,
                    outputSlotCursor = port.OutputSlotCursor,
                    routingMode = port.RoutingMode,
                    roundRobinDestinationCursor = port.RoundRobinDestinationCursor
                });
            }
        }

        return data;
    }

    private bool ValidateBuildingConnections(BuildingSaveData savedBuilding, BuildingDefinition definition) {
        bool Fail(string message) {
            Debug.LogWarning($"World layout validation failed: {definition.name}: {message}", this);
            return false;
        }

        const PipeConnections VALID_CONNECTIONS =
            PipeConnections.Left |
            PipeConnections.Right |
            PipeConnections.Front |
            PipeConnections.Back |
            PipeConnections.Top |
            PipeConnections.Bottom;

        if ((savedBuilding.connections & ~VALID_CONNECTIONS) != PipeConnections.None) {
            return Fail("Invalid connection flags.");
        }

        bool isPipe = definition.Prefab.GetComponent<PipeSegment>() != null;
        bool isCable = definition.Prefab.GetComponent<EnergyCableSegment>() != null;

        if (isPipe && isCable) {
            return Fail("Prefab contains both pipe and energy cable components.");
        }

        if (!isPipe && !isCable && savedBuilding.connections != PipeConnections.None) {
            return Fail("Unexpected segment connections.");
        }

        if (savedBuilding.ports == null) {
            return Fail("Missing port list.");
        }

        UniversalIO universalIO = definition.Prefab.GetComponent<UniversalIO>();

        if (universalIO == null) {
            return savedBuilding.ports.Count == 0 || Fail("Unexpected ports on a building without UniversalIO.");
        }

        HashSet<(Vector3Int Cell, PortFace Face)> expectedPorts = new();
        Vector3Int size = definition.Size;

        for (int x = 0; x < size.x; x++) {
            for (int z = 0; z < size.z; z++) {
                Vector3Int cell = new Vector3Int(x, 0, z);

                expectedPorts.Add((cell, PortFace.Bottom));

                if (x == 0) expectedPorts.Add((cell, PortFace.Left));
                if (x == size.x - 1) expectedPorts.Add((cell, PortFace.Right));
                if (z == 0) expectedPorts.Add((cell, PortFace.Back));
                if (z == size.z - 1) expectedPorts.Add((cell, PortFace.Front));
            }
        }

        if (savedBuilding.ports.Count != expectedPorts.Count) {
            return Fail("Port count does not match the building.");
        }

        foreach (UniversalPortSaveData port in savedBuilding.ports) {
            if (port == null) {
                return Fail("Missing port entry.");
            }

            if (!expectedPorts.Remove((port.localCell, port.face))) {
                return Fail("Invalid or duplicate port.");
            }

            if (!System.Enum.IsDefined(typeof(PortMode), port.mode)) {
                return Fail("Invalid port mode.");
            }

            if (!System.Enum.IsDefined(typeof(ItemRoutingMode), port.routingMode)) {
                return Fail("Invalid routing mode.");
            }

            if (port.outputSlotCursor < 0 || port.roundRobinDestinationCursor < 0) {
                return Fail("Negative routing cursor.");
            }
        }

        return true;
    }

    [ContextMenu("Debug/Print World Layout")]
    private void PrintWorldLayoutForDebug() {
        if (!Application.isPlaying) return;

        WorldSaveData data = CaptureWorldLayout();

        Debug.Log(JsonUtility.ToJson(data, true), this);
    }
}
