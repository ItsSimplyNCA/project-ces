using UnityEngine;

[CreateAssetMenu(menuName = "Factory/Building Definition")]
public class BuildingDefinition : ScriptableObject {
    [SerializeField] private GameObject prefab;
    [SerializeField] private Vector3Int size = Vector3Int.one;
    [SerializeField] private ItemDefinition item;
    [SerializeField] private bool requiresSupport = true;
    [SerializeField] private string buildingId;

    public GameObject Prefab => prefab;
    public Vector3Int Size => size;
    public ItemDefinition Item => item;
    public bool RequiresSupport => requiresSupport;
    public string BuildingId => buildingId;
}
