using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Factory/Recipe Definition")]
public class RecipeDefinition : ScriptableObject {
    [SerializeField] private string recipeId;
    [SerializeField] private string recipeName;
    [SerializeField, Min(0.01f)] private float duration = 5f;
    [SerializeField, Min(1)] private float powerDemandMW = 1f;
    [SerializeField] private ItemAmount[] inputs;
    [SerializeField] private ItemAmount[] outputs;
    [SerializeField, Min(1)] private int minimumMachineTier = 1;
    [SerializeField] private FluidAmount[] fluidInputs = System.Array.Empty<FluidAmount>();
    [SerializeField] private FluidAmount[] fluidOutputs = System.Array.Empty<FluidAmount>();

    public string RecipeIp => recipeId;
    public string RecipeName => recipeName;
    public float Duration => duration;
    public int MinimumMachineTier => minimumMachineTier;
    public float PowerDemandMW => powerDemandMW;

    public IReadOnlyList<ItemAmount> Inputs => inputs;
    public IReadOnlyList<ItemAmount> Outputs => outputs;
    public IReadOnlyList<FluidAmount> FluidInputs => fluidInputs;
    public IReadOnlyList<FluidAmount> FluidOutputs => fluidOutputs;
}
