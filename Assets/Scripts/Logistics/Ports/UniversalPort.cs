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
        if (slotCount <= 0) {
            outputSlotCursor = 0;
            return;
        }

        int currentIndex = Mathf.Max(0, outputSlotCursor) % slotCount;
        outputSlotCursor = (currentIndex + 1) % slotCount;
    }

    public void SetRoutingMode(ItemRoutingMode mode) {
        itemRoutingMode = mode;
    }

    public void SetRoundRobinDestinationCursor(int index) {
        roundRobinDestinationCursor = index;
    }

    public bool ValidateSaveData(UniversalPortSaveData data) {
        bool Fail(string message) {
            Debug.LogWarning($"Port validation failed ({localCell} / {face}): {message}");
            return false;
        }

        if (data == null) {
            return Fail("Missing port data.");
        }

        if (data.localCell != localCell || data.face != face) {
            return Fail("Saved port identity does not match.");
        }

        if (!Enum.IsDefined(typeof(PortMode), data.mode)) {
            return Fail("Invalid port mode.");
        }

        if (!Enum.IsDefined(typeof(ItemRoutingMode), data.mode)) {
            return Fail("Invalid routing mode.");
        }

        if (data.outputSlotCursor < 0 || data.roundRobinDestinationCursor < 0) {
            return Fail("Invalid routing cursor.");
        }

        return true;
    }

    public bool TryRestoreSaveData(UniversalPortSaveData data) {
        if (!ValidateSaveData(data)) return false;

        mode = data.mode;
        connectionEnabled = data.connectionEnabled;
        outputSlotCursor = data.outputSlotCursor;
        itemRoutingMode = data.routingMode;
        roundRobinDestinationCursor = data.roundRobinDestinationCursor;

        return true;
    }
}