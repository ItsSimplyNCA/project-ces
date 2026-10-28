using System.Collections.Generic;
using UnityEngine;

public class Workbench : MonoBehaviour{
    [SerializeField] private RecipeDefinition[] availableRecipes = System.Array.Empty<RecipeDefinition>();

    private RecipeDefinition activeRecipe;
    private PlayerInventory playerInventory;
    private List<InventorySlot> takenInputs;
    private float craftingElapsed;

    public IReadOnlyList<RecipeDefinition> AvailableRecipes => availableRecipes;
    public RecipeDefinition ActiveRecipe => activeRecipe;
    public bool IsCrafting => activeRecipe != null;
    public float CraftingProgress => activeRecipe != null
        ? Mathf.Clamp01(craftingElapsed / activeRecipe.Duration)
        : 0f;

    public bool HasContents {
        get {
            if (activeRecipe != null) return true;
            if (takenInputs == null) return false;

            foreach (InventorySlot slot in takenInputs) {
                if (!slot.IsEmpty) return true;
            }

            return false;
        }
    }

    public bool SupportsRecipe(RecipeDefinition recipe) {
        if (recipe == null) return false;
        if (recipe.FluidInputs.Count > 0 || recipe.FluidOutputs.Count > 0) return false;

        foreach (RecipeDefinition availableRecipe in availableRecipes) {
            if (availableRecipe == recipe) return true;
        }

        return false;
    }

    public bool TryStartCrafting(RecipeDefinition recipe, PlayerInventory inventory) {
        if (inventory == null || HasContents) return false;
        if (!SupportsRecipe(recipe)) return false;

        if (!inventory.HasRecipeInputs(recipe)) {
            Debug.Log("Missing ingredients", this);
            return false;
        }

        if (!inventory.CanFitRecipeOutputs(recipe)) {
            Debug.LogWarning("Inventory full", this);
            return false;
        }

        if (!inventory.TryTakeRecipeInputs(recipe, out var inputs)) {
            return false;
        }

        playerInventory = inventory;
        takenInputs = inputs;
        activeRecipe = recipe;
        craftingElapsed = 0f;

        return true;
    }

    private void Update() {
        if (Time.deltaTime <= 0f) return;
        if (playerInventory == null) return;

        if (activeRecipe == null) {
            if (takenInputs != null && playerInventory.ReturnRecipeInputs(takenInputs)) {
                ClearProcess();
            }

            return;
        }

        craftingElapsed = Mathf.Min(
            craftingElapsed + Time.deltaTime,
            activeRecipe.Duration
        );

        if (craftingElapsed < activeRecipe.Duration) return;
        if (!playerInventory.CanFitRecipeOutputs(activeRecipe)) return;

        foreach (ItemAmount output in activeRecipe.Outputs) {
            playerInventory.Add(output.Item, output.Amount);
        }

        Debug.Log($"Crafting completed: {activeRecipe.RecipeName}", this);

        ClearProcess();
    }

    public bool CancelCrafting() {
        if (!HasContents) return true;

        activeRecipe = null;
        craftingElapsed = 0f;

        Debug.Log("Crafting cancelled", this);

        if (playerInventory == null) return false;
        if (!playerInventory.ReturnRecipeInputs(takenInputs)) return false;

        ClearProcess();
        return true;
    }

    private void ClearProcess() {
        activeRecipe = null;
        playerInventory = null;
        takenInputs = null;
        craftingElapsed = 0f;
    }
}
