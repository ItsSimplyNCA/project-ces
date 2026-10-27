using UnityEngine;

[CreateAssetMenu(menuName = "Factory/Item Definition")]
public class ItemDefinition : ScriptableObject {
    [SerializeField] private string itemId;
    [SerializeField] private string itemName;
    [SerializeField, Min(1)] private int maxStackSize = 100;
    [SerializeField] private bool isFluidCell;

    public string ItemId => itemId;
    public string ItemName => itemName;
    public int MaxStackSize => isFluidCell ? 1 : maxStackSize;
    public bool IsFluidCell => isFluidCell;
}
