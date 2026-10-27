using UnityEngine;

[CreateAssetMenu(menuName = "Factory/Pipe Definition")]
public class PipeDefinition : ScriptableObject {
    [SerializeField] private PipeMedium medium;
    [SerializeField, Min(1)] private int tier = 1;
    [SerializeField, Min(1)] private float transferRate = 8f;

    public PipeMedium Medium => medium;
    public int Tier => tier;
    public float TransferRate => transferRate;
}