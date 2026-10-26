using System;
using UnityEngine;

[Serializable]
public class UniversalPort {
    [SerializeField] private Vector3Int localCell;
    [SerializeField] private PortFace face;
    [SerializeField] private PortMode mode;
    [SerializeField] private bool connectionEnabled;
    [SerializeField] private int outputSlotCursor;
    [SerializeField] private ItemRoutingMode itemRoutingMode = ItemRoutingMode.NearestPoint;
    [SerializeField] private int roundRobinDestinationCursor;

    public Vector3Int LocalCell => localCell;
    public PortFace Face => face;
    public PortMode Mode => mode;
    public bool ConnectionEnabled => connectionEnabled;
    public int OutputSlotCursor => outputSlotCursor;
    public ItemRoutingMode RoutingMode => itemRoutingMode;
    public int RoundRobinDestinationCursor => roundRobinDestinationCursor;

    public UniversalPort(Vector3Int localCell, PortFace face) {
        this.localCell = localCell;
        this.face = face;

        mode = PortMode.None;
        connectionEnabled = false;
        outputSlotCursor = 0;
        roundRobinDestinationCursor = 0;
    }

    public void SetMode(PortMode mode) {
        this.mode = mode;
    }

    public void SetConnectionEnabled(bool enabled) {
        connectionEnabled = enabled;
    }

    public void AdvanceOutputSlotCursor(int slotCount) {
        outputSlotCursor = (outputSlotCursor + 1) % slotCount;
    }

    public void SetRoutingMode(ItemRoutingMode mode) {
        itemRoutingMode = mode;
    }

    public void SetRoundRobinDestinationCursor(int index) {
        roundRobinDestinationCursor = index;
    } 
}