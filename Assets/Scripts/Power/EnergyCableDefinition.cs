using UnityEngine;

[CreateAssetMenu(menuName = "Factory/Energy Cable Definition")]
public class EnergyCableDefinition : ScriptableObject {
    [SerializeField, Min(1)] private int tier = 1;
    [SerializeField, Min(0.001f)] private float capacityMW = 10f;

    public int Tier => tier;
    public float CapacityMW => capacityMW;
}