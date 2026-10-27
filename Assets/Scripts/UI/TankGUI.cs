using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DefaultExecutionOrder(-90)]
public class TankGUI : MonoBehaviour {
    [SerializeField] private Camera playerCamera;
    [SerializeField] private PlayerUIState uiState;
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private float interactionDistance = 6f;

    [SerializeField] private GameObject tankPanel;
    [SerializeField] private TMP_Text tankStatusText;
    [SerializeField] private TMP_Dropdown cellDropdown;
    [SerializeField] private Button fillCellButton;
    [SerializeField] private Button drainCellButton;
    [SerializeField] private Button lockButton;
    [SerializeField] private TMP_Text lockButtonText;
    [SerializeField] private PortConfigurationGUI portConfigurationGUI;

    private readonly List<int> selectableCellSlots = new();
    private Tank tank;
    private bool isOpen;

    private void Awake() {
        fillCellButton.onClick.AddListener(FillCell);
        drainCellButton.onClick.AddListener(DrainCell);
        lockButton.onClick.AddListener(ToggleLock);
        cellDropdown.onValueChanged.AddListener(OnCellSelected);
    }

    private void Start() {
        tankPanel.SetActive(false);
    }

    private void Update() {
        if (isOpen) {
            bool closePressed = Keyboard.current != null && (
                Keyboard.current.escapeKey.wasPressedThisFrame ||
                Keyboard.current.eKey.wasPressedThisFrame
            );

            if (tank == null || closePressed) {
                Close();
            } else {
                RefreshStatus();
            }

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

        Tank target = hit.collider.GetComponentInParent<Tank>();

        if (target == null || !uiState.TryOpen(this)) return;

        tank = target;
        isOpen = true;

        portConfigurationGUI.Bind(tank.GetComponent<UniversalIO>());

        PopulateCells();
        RefreshStatus();
        tankPanel.SetActive(true);
    }

    private void OnDestroy() {
        fillCellButton.onClick.RemoveListener(FillCell);
        drainCellButton.onClick.RemoveListener(DrainCell);
        lockButton.onClick.RemoveListener(ToggleLock);
        cellDropdown.onValueChanged.RemoveListener(OnCellSelected);

        if (isOpen) uiState.Close(this);
    }

    private void Close() {
        cellDropdown.Hide();
        portConfigurationGUI.Unbind();
        tankPanel.SetActive(false);

        tank = null;
        isOpen = false;

        uiState.Close(this);
    }

    private int GetSelectedSlotIndex() {
        int index = cellDropdown.value;

        return index >= 0 && index < selectableCellSlots.Count 
            ? selectableCellSlots[index]
            : -1;
    }

    private FluidCellData GetSelectedCell() {
        int slotIndex = GetSelectedSlotIndex();

        return slotIndex >= 0
            ? playerInventory.GetSlotFluidCell(slotIndex)
            : null;
    }

    private void PopulateCells() {
        int previousSlotIndex = GetSelectedSlotIndex();

        selectableCellSlots.Clear();
        List<string> options = new();

        for (int i = 0; i < playerInventory.SlotCount; i++) {
            FluidCellData cell = playerInventory.GetSlotFluidCell(i);

            if (cell == null) continue;

            selectableCellSlots.Add(i);

            string contents = cell.Fluid != null
                ? $"{cell.Fluid.FluidName} - {cell.Amount:0.###} L"
                : $"Empty cell";

            options.Add($"Slot {i + 1}: {contents}");
        }

        cellDropdown.ClearOptions();
        cellDropdown.interactable = selectableCellSlots.Count > 0;

        if (selectableCellSlots.Count == 0) {
            options.Add("No fluid cells");
        }

        cellDropdown.AddOptions(options);

        int selectedIndex = selectableCellSlots.IndexOf(previousSlotIndex);

        cellDropdown.SetValueWithoutNotify(selectedIndex >= 0 ? selectedIndex : 0);
        cellDropdown.RefreshShownValue();
    }

    private void RefreshStatus() {
        string fluidName = tank.Fluid != null
            ? tank.Fluid.FluidName
            : "None";

        string lockState = tank.IsLocked ? "Locked" : "Unlocked";

        FluidCellData cell = GetSelectedCell();

        string cellContents = cell == null
            ? "None"
            : cell.Fluid != null
                ? $"{cell.Fluid.FluidName} - {cell.Amount:0.###} L"
                : "Empty";

        string text =
            $"Fluid: {fluidName}\n" +
            $"Tier: {tank.Tier} | Amount: {tank.Amount:0.###} / {tank.Capacity:0.###} L\n" +
            $"Type: {lockState}\n" +
            $"Selected cell: {cellContents}";

        if (tankStatusText.text != text) {
            tankStatusText.text = text;
        }

        fillCellButton.interactable =
            cell != null &&
            tank.Amount > 0f &&
            cell.Amount < cell.Capacity &&
            (cell.Fluid == null || cell.Fluid == tank.Fluid);

        drainCellButton.interactable =
            cell != null &&
            cell.Amount > 0f &&
            tank.Amount < tank.Capacity &&
            (tank.Fluid == null || tank.Fluid == cell.Fluid);

        lockButton.interactable = tank.Fluid != null;
        lockButtonText.text = tank.IsLocked ? "Unlock Type" : "Lock Type";
    }

    private void OnCellSelected(int index) {
        if (isOpen && tank != null) {
            RefreshStatus();
        }
    }

    private void FillCell() {
        if (!isOpen || tank == null) return;

        tank.FillCell(GetSelectedCell());

        PopulateCells();
        RefreshStatus();
    }

    private void DrainCell() {
        if (!isOpen || tank == null) return;

        tank.DrainCell(GetSelectedCell());

        PopulateCells();
        RefreshStatus();
    }

    private void ToggleLock() {
        if (!isOpen || tank == null) return;

        tank.TrySetLocked(!tank.IsLocked);
        RefreshStatus();
    }
}
