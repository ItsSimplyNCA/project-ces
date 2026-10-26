using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DefaultExecutionOrder(-90)]
public class SiloGUI : MonoBehaviour {
    [SerializeField] private Camera playerCamera;
    [SerializeField] private PlayerUIState uiState;
    [SerializeField] private GameObject siloPanel;
    [SerializeField] private TMP_Text siloStatusText;
    [SerializeField] private float interactionDistance = 6f;
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private TMP_Dropdown itemDropdown;
    [SerializeField] private Button insertButton;
    [SerializeField] private Button collectButton;
    [SerializeField] private Button lockButton;
    [SerializeField] private TMP_Text lockButtonText;
    [SerializeField] private PortConfigurationGUI portConfigurationGUI;

    private Silo silo;
    private bool isOpen;
    private readonly List<ItemDefinition> selectableItems = new();

    private void Awake() {
        insertButton.onClick.AddListener(InsertItems);
        collectButton.onClick.AddListener(CollectItems);
        itemDropdown.onValueChanged.AddListener(OnItemSelected);
        lockButton.onClick.AddListener(ToggleLock);
    }

    private void Start() {
        siloPanel.SetActive(false);
    }

    private void Update() {
        if (isOpen) {
            bool closePressed = Keyboard.current != null
                && (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame);

            if (silo == null || closePressed) Close();
            else RefreshStatus();

            return;
        }

        if (uiState.IsOpen || Keyboard.current == null) return;
        if (!Keyboard.current.eKey.wasPressedThisFrame) return;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        bool hasHit = Physics.Raycast(
            ray,
            out RaycastHit hit,
            interactionDistance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore
        );

        if (!hasHit) return;

        Silo target = hit.collider.GetComponentInParent<Silo>();

        if (target == null || !uiState.TryOpen(this)) return;

        silo = target;
        isOpen = true;

        portConfigurationGUI.Bind(silo.GetComponent<UniversalIO>());

        PopulateItems();
        RefreshStatus();
        siloPanel.SetActive(true);
    }

    private void OnDestroy() {
        insertButton.onClick.RemoveListener(InsertItems);
        collectButton.onClick.RemoveListener(CollectItems);
        itemDropdown.onValueChanged.RemoveListener(OnItemSelected);
        lockButton.onClick.RemoveListener(ToggleLock);
    }

    private void RefreshStatus() {
        string itemName = silo.Item != null
            ? silo.Item.ItemName
            : "None";

        string lockState = silo.IsLocked ? "Locked" : "Unlocked";

        ItemDefinition selectedItem = GetSelectedItem();

        string inventoryItemName = selectedItem != null
            ? selectedItem.ItemName
            : "None";

        int inventoryAmount = playerInventory.GetAmount(selectedItem);

        string text =
            $"Item: {itemName}\n" +
            $"Tier: {silo.Tier} | Amount: {silo.Amount} / {silo.Capacity}\n" +
            $"Type: {lockState}\n" +
            $"Inventory: {inventoryItemName} × {inventoryAmount}";

        if (siloStatusText.text != text) {
            siloStatusText.text = text;
        }

        insertButton.interactable = inventoryAmount > 0;
        collectButton.interactable = silo.Amount > 0;
        lockButton.interactable = silo.Item != null;
        lockButtonText.text = silo.IsLocked ? "Unlock type" : "Lock type";
    }

    private void Close() {
        portConfigurationGUI.Unbind();

        itemDropdown.Hide();
        siloPanel.SetActive(false);

        silo = null;
        isOpen = false;

        uiState.Close(this);
    }

    private ItemDefinition GetSelectedItem() {
        int index = itemDropdown.value;

        return index >= 0 && index < selectableItems.Count
            ? selectableItems[index]
            : null;
    }

    private void PopulateItems() {
        ItemDefinition previousItem = GetSelectedItem();

        selectableItems.Clear();

        List<string> options = new();

        for (int i = 0; i < playerInventory.SlotCount; i++) {
            ItemDefinition item = playerInventory.GetSlotItem(i);

            if (item == null || selectableItems.Contains(item)) continue;

            selectableItems.Add(item);
            options.Add(item.ItemName);
        }

        itemDropdown.ClearOptions();
        itemDropdown.interactable = selectableItems.Count > 0;

        if (selectableItems.Count == 0) {
            options.Add("No inventory items");
        }

        itemDropdown.AddOptions(options);

        int selectedIndex = selectableItems.IndexOf(previousItem);

        itemDropdown.SetValueWithoutNotify(selectedIndex >= 0 ? selectedIndex : 0);

        itemDropdown.RefreshShownValue();
    }

    private void OnItemSelected(int index) {
        if (isOpen && silo != null) RefreshStatus();
    }

    private void InsertItems() {
        if (!isOpen || silo == null) return;

        ItemDefinition item = GetSelectedItem();

        if (item == null) return;

        int availableAmount = playerInventory.GetAmount(item);
        int transferredAmount = playerInventory.TransferTo(silo, item, availableAmount);

        if (availableAmount > 0 && transferredAmount == 0) {
            Debug.Log("Silo cannot accept the selected item");
        }

        PopulateItems();
        RefreshStatus();
    }

    private void CollectItems() {
        if (!isOpen || silo == null || silo.Item == null || silo.Amount == 0) return;

        int availableAmount = silo.Amount;
        int collectedAmount = playerInventory.CollectFrom(silo);

        if (collectedAmount < availableAmount) {
            Debug.LogWarning("Inventory Full");
        }

        PopulateItems();
        RefreshStatus();
    }

    private void ToggleLock() {
        if (!isOpen || silo == null) return;

        silo.TrySetLocked(!silo.IsLocked);
        RefreshStatus();
    }
}
