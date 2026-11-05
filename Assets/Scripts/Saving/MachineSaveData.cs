using System;
using System.Collections.Generic;

[Serializable] public class MachineSaveData {
    public bool isOn;

    public string selectedRecipeId = string.Empty;
    public string activeRecipeId = string.Empty;
    public float craftingElapsed;

    public InventorySaveData inputInventory = new();
    public InventorySaveData outputInventory = new();

    public List<FluidSlotSaveData> fluidInputSlots = new();
    public List<FluidSlotSaveData> fluidOutputSlots = new();
}

[Serializable] public class FluidSlotSaveData {
    public string fluidId = string.Empty;
    public float amount;
}
