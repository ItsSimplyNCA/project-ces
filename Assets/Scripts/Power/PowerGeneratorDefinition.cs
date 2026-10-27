using UnityEngine;

[CreateAssetMenu(menuName = "Factory/Power Generator Definition")]
public class PowerGeneratorDefinition : ScriptableObject {
    [SerializeField, Min(0.001f)] private float outputMW = 10f;

    public float OutputMW => outputMW;
}
