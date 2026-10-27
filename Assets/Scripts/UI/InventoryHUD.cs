using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
public class InventoryHUD : MonoBehaviour {
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private Transform slotsContainer;
    [SerializeField] private InventorySlotView slotViewPrefab;
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private BuildingPreviewController buildingPreviewController;
    [SerializeField] private PlayerUIState uiState;

    private InventorySlotView[] slotViews;
    private bool isOpen;
    private int selectedSlotIndex = -1;

    public bool IsOpen => isOpen;
    public int SelectedSlotIndex => selectedSlotIndex;

    private void Start() {
        slotViews = new InventorySlotView[playerInventory.SlotCount];

        for (int i = 0; i < slotViews.Length; i++) {
            slotViews[i] = Instantiate(slotViewPrefab, slotsContainer);
            slotViews[i].Initialize(i);
            slotViews[i].Clicked += SelectSlot;
        }

        inventoryPanel.SetActive(false);
    }

    private void Update() {
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame) {
            if (isOpen) {
                uiState.Close(this);

                isOpen = false;
                inventoryPanel.SetActive(false);
            } else if (uiState.TryOpen(this)) {
                isOpen = true;
                inventoryPanel.SetActive(true);
            }
        } 

        if (!isOpen) return;

        for (int i = 0; i < slotViews.Length; i++) {
            slotViews[i].Refresh(
                playerInventory.GetSlotItem(i),
                playerInventory.GetSlotAmount(i),
                playerInventory.GetSlotFluidCell(i)
            );
        }
    }

    private void SelectSlot(int slotIndex) {
        ItemDefinition item = playerInventory.GetSlotItem(slotIndex);

        if (!buildingPreviewController.TrySelectBuilding(item)) return;

        selectedSlotIndex = slotIndex;

        for (int i = 0; i < slotViews.Length; i++) {
            slotViews[i].SetSelected(i == selectedSlotIndex);
        }
    }
}
