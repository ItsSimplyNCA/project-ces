using UnityEngine;

[CreateAssetMenu(menuName = "Factory/Tank Definition")]
public class TankDefinition : ScriptableObject {
    [SerializeField, Min(1)] private int tier = 1;
    [SerializeField, Min(0.001f)] private float capacity = 1000f;

    public int Tier => tier;
    public float Capacity => capacity;
}
