using System.Collections.Generic;
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
}
