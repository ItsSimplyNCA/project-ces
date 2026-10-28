using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Factory/Game Definition Catalog")]
public class GameDefinitionCatalog : ScriptableObject {
    [SerializeField] private ItemDefinition[] items = Array.Empty<ItemDefinition>();
    [SerializeField] private FluidDefinition[] fluids = Array.Empty<FluidDefinition>();
    [SerializeField] private RecipeDefinition[] recipes = Array.Empty<RecipeDefinition>();
    [SerializeField] private BuildingDefinition[] buildings = Array.Empty<BuildingDefinition>();

    private readonly Dictionary<string, ItemDefinition> itemIndex = new(StringComparer.Ordinal);
    private readonly Dictionary<string, FluidDefinition> fluidIndex = new(StringComparer.Ordinal);
    private readonly Dictionary<string, RecipeDefinition> recipeIndex = new(StringComparer.Ordinal);
    private readonly Dictionary<string, BuildingDefinition> buildingIndex = new(StringComparer.Ordinal);

    private bool initialized;
    private bool isValid;

    public bool IsValid {
        get {
            EnsureInitialized();
            return isValid;
        }
    }

    private void OnEnable() {
        initialized = false;
    }

    private void OnValidate() {
        initialized = false;
    }

    private void EnsureInitialized() {
        if (initialized) return;

        initialized = true;
        isValid = true;

        isValid &= BuildIndex(items, itemIndex, item => item.ItemId);
        isValid &= BuildIndex(fluids, fluidIndex, fluid => fluid.FluidId);
        isValid &= BuildIndex(recipes, recipeIndex, recipe => recipe.RecipeId);
        isValid &= BuildIndex(buildings, buildingIndex, building => building.BuildingId);
    }

    private bool BuildIndex<T>(T[] definitions, Dictionary<string, T> index, Func<T, string> getId) where T : UnityEngine.Object {
        index.Clear();

        if (definitions == null) {
            Debug.LogError($"Missing definition list: {typeof(T).Name}", this);
            return false;
        }

        bool valid = true;

        foreach (T definition in definitions) {
            if (definition == null) {
                Debug.LogError($"Empty entry in catalog: {typeof(T).Name}", this);

                valid = false;
                continue;
            }

            string id = getId(definition);

            if (string.IsNullOrWhiteSpace(id)) {
                Debug.LogError($"Missing ID: {definition.name}", definition);

                valid = false;
                continue;
            }

            if (index.ContainsKey(id)) {
                Debug.LogError($"Duplicate {typeof(T).Name} ID: {id}", definition);

                valid = false;
                continue;
            }

            index.Add(id, definition);
        }

        return valid;
    }

    public bool TryGetItem(string id, out ItemDefinition definition) {
        definition = null;
        return IsValid && !string.IsNullOrWhiteSpace(id) && itemIndex.TryGetValue(id, out definition);
    }

    public bool TryGetFluid(string id, out FluidDefinition definition) {
        definition = null;
        return IsValid && !string.IsNullOrWhiteSpace(id) && fluidIndex.TryGetValue(id, out definition);
    }

    public bool TryGetRecipe(string id, out RecipeDefinition definition) {
        definition = null;
        return IsValid && !string.IsNullOrWhiteSpace(id) && recipeIndex.TryGetValue(id, out definition);
    }

    public bool TryGetBuilding(string id, out BuildingDefinition definition) {
        definition = null;
        return IsValid && !string.IsNullOrWhiteSpace(id) && buildingIndex.TryGetValue(id, out definition);
    }

    [ContextMenu("Validate Catalog")]
    private void ValidateCatalog() {
        initialized = false;
        if (IsValid) {
            Debug.Log("Definition catalog is valid", this);
        }
    }
}