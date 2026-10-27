using UnityEngine;

[CreateAssetMenu(menuName = "Factory/Silo Definition")]
public class SiloDefinition : ScriptableObject {
    [SerializeField, Min(1)] private int tier = 1;
    [SerializeField, Min(1)] private int capacity = 1000;

    public int Tier => tier;
    public int Capacity => capacity;
}