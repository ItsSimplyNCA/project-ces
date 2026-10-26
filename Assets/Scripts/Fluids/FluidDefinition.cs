using UnityEngine;

[CreateAssetMenu(menuName = "Factory/Fluid Definition")]
public class FluidDefinition : ScriptableObject {
    [SerializeField] private string fluidId;
    [SerializeField] private string fluidName;

    public string FluidId => fluidId;
    public string FluidName => fluidName;
}