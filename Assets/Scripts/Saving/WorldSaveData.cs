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
    
    public MachineSaveData machine;

    public SiloSaveData silo;
    public TankSaveData tank;

    public PowerGeneratorSaveData powerGenerator;
    public PowerBreakerSaveData powerBreaker;
    public PowerStorageSaveData powerStorage;

    public WorkbenchSaveData workbench;
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
