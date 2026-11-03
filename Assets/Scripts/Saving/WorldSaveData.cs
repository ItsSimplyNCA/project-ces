using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable] public class WorldSaveData {
    public float cellSize = 1f;
    public List<BuildingSaveData> buildings = new();
}

[Serializable] public class BuildingSaveData {
    public string buildingId = string.Empty;
    public Vector3Int origin;
    public int quarterTurns;

    public PipeConnections connections;
    public List<UniversalPortSaveData> ports = new();
}

[Serializable] public class UniversalPortSaveData {
    public Vector3Int localCell;
    public PortFace face;
    public PortMode mode;
    public bool connectionEnabled;

    public int outputSlotCursor;
    public ItemRoutingMode routingMode;
    public int roundRobinDestinationCursor;
}
