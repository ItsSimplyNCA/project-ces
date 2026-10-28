using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "Factory/Power Storage Definition")]
public class PowerStorageDefinition : ScriptableObject {
    [SerializeField, Min(1)] private int tier = 1;
    
    [SerializeField, Min(0.001f)] private float capacityMW = 1000f;

    public int Tier => tier;
    public float CapacityMW => capacityMW;
}
