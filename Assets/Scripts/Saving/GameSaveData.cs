using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

[Serializable] public class GameSaveData {
    public int version = 1;
    public PlayerSaveData player = new();
}

[Serializable] public class PlayerSaveData {
    public Vector3 position;
    public Quaternion rotation = Quaternion.identity;
    public InventorySaveData inventory = new();
}

[Serializable] public class InventorySaveData {
    public int slotCount;
    public List<InventorySlotSaveData> slots = new();
}

[Serializable] public class InventorySlotSaveData {
    public string itemId = string.Empty;
    public int amount;

    public string fluidId = string.Empty;
    public float fluidAmount;
}
