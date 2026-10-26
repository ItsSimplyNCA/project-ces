using UnityEngine;

public class BuildingInstance : MonoBehaviour {
    public BuildingDefinition Definition { get; private set; }
    public Vector3Int Origin { get; private set; }
    public int QuarterTurns { get ; private set; }

    public void Initialize(BuildingDefinition definition, Vector3Int origin, int quarterTurns) {
        Definition = definition;
        Origin = origin;
        QuarterTurns = quarterTurns;   
    }
}
