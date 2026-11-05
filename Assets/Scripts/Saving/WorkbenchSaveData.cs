using System;

[Serializable] public class WorkbenchSaveData {
    public string activeRecipeId = string.Empty;
    public float craftingElapsed;

    public InventorySaveData takenInputs = new();
}