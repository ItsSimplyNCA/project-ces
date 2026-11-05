using System;
using System.Collections.Generic;

[Serializable] public class SiloSaveData {
    public string itemId = string.Empty;
    public int amount;
    public bool isLocked;

    public List<FluidSlotSaveData> fluidCells = new();
}

[Serializable] public class TankSaveData {
    public string fluidId = string.Empty;
    public float amount;
    public bool isLocked;
}
