public interface IItemTransferEndpoint {
    int AddInput(ItemDefinition item, int requestedAmount);
    int OutputSlotCount { get; }

    ItemDefinition GetOutputSlotItem(int slotIndex);
    int GetOutputSlotAmount(int slotIndex);

    int RemoveOutputFromSlot(int slotIndex, int requestedAmount);
}
