using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "Factory/Power Storage Definition")]
public class PowerStorageDefinition : ScriptableObject {
    [SerializeField, Min(1)] private int tier = 1;
    
    [FormerlySerializedAs("capacityMWh")]
    [SerializeField, Min(0.001f)] private float capacityMW = 1000f;

    [SerializeField] private bool transferLimitEnabled;
    [SerializeField, Min(0.001f)] private float transferLimitMW = 10f;

    public int Tier => tier;
    public float CapacityMW => capacityMW;

    public bool TransferLimitEnabled => transferLimitEnabled;
    public float TransferLimitMW => transferLimitMW;
}
