using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MachineFluidGUI : MonoBehaviour {
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private TMP_Dropdown fluidCellDropdown;
    [SerializeField] private TMP_Dropdown fluidOutputDropdown;
    [SerializeField] private Button drainCellButton;
    [SerializeField] private Button fillCellButton;
    [SerializeField] private TMP_Text fluidTransferStatusText;
    [SerializeField] private TMP_Dropdown fluidInputDropdown;
    [SerializeField] private Button returnFluidInputButton;

    private readonly List<int> cellSlots = new();
    private Machine machine;

    private void Awake() {
        drainCellButton.onClick.AddListener(DrainCell);
        fillCellButton.onClick.AddListener(FillCell);
        returnFluidInputButton.onClick.AddListener(ReturnFluidInput);

        fluidCellDropdown.onValueChanged.AddListener(OnSelectionChanged);
        fluidOutputDropdown.onValueChanged.AddListener(OnSelectionChanged);
        fluidInputDropdown.onValueChanged.AddListener(OnSelectionChanged);
    }

    private void OnDestroy() {
        drainCellButton.onClick.RemoveListener(DrainCell);
        fillCellButton.onClick.RemoveListener(FillCell);
        returnFluidInputButton.onClick.RemoveListener(ReturnFluidInput);

        fluidCellDropdown.onValueChanged.RemoveListener(OnSelectionChanged);
        fluidOutputDropdown.onValueChanged.RemoveListener(OnSelectionChanged);
        fluidInputDropdown.onValueChanged.RemoveListener(OnSelectionChanged);
    }

    public void Bind(Machine target) {
        machine = target;

        PopulateCells();
        PopulateOutputs();
        PopulateInputs();
        Refresh();
    }

    public void Unbind() {
        fluidCellDropdown.Hide();
        fluidOutputDropdown.Hide();
        fluidInputDropdown.Hide();

        machine = null;
        cellSlots.Clear();

        drainCellButton.interactable = false;
        fillCellButton.interactable = false;
        returnFluidInputButton.interactable = false;
    }

    private int GetSelectedCellSlot() {
        int index = fluidCellDropdown.value;
        return index >= 0 && index < cellSlots.Count ? cellSlots[index] : -1;
    }

    private FluidCellData GetSelectedCell() {
        int slotIndex = GetSelectedCellSlot();
        return slotIndex >= 0 ? playerInventory.GetSlotFluidCell(slotIndex) : null;
    }

    private void PopulateCells() {
        int previousSlot = GetSelectedCellSlot();

        cellSlots.Clear();
        List<string> options = new();

        for (int i = 0; i < playerInventory.SlotCount; i++) {
            FluidCellData cell = playerInventory.GetSlotFluidCell(i);

            if (cell == null) continue;

            cellSlots.Add(i);
            options.Add(GetCellLabel(i, cell));
        }

        fluidCellDropdown.ClearOptions();

        if (options.Count == 0) {
            options.Add("No fluid cells");
        }

        fluidCellDropdown.AddOptions(options);
        fluidCellDropdown.interactable = cellSlots.Count > 0;

        int selectedIndex = cellSlots.IndexOf(previousSlot);

        fluidCellDropdown.SetValueWithoutNotify(selectedIndex >= 0 ? selectedIndex : 0);
        fluidCellDropdown.RefreshShownValue();
    }

    private static string GetCellLabel(int slotIndex, FluidCellData cell) {
        string contents = cell.Fluid != null
            ? $"{cell.Fluid.FluidName} - {cell.Amount:0.###}L"
            : "Empty";
        return $"Slot {slotIndex + 1}: {contents}";
    }

    private void PopulateOutputs() {
        List<string> options = new();

        for (int i = 0; i < machine.FluidOutputSlotCount; i++) {
            options.Add($"Output {i + 1}");
        }

        fluidOutputDropdown.ClearOptions();

        if (options.Count == 0) {
            options.Add("No fluid outputs");
        }

        fluidOutputDropdown.AddOptions(options);
        fluidOutputDropdown.SetValueWithoutNotify(0);
        fluidOutputDropdown.RefreshShownValue();
        fluidOutputDropdown.interactable = machine.FluidOutputSlotCount > 0;
    }

    private void PopulateInputs() {
        List<string> options = new();

        for (int i = 0; i < machine.FluidInputSlotCount; i++) {
            options.Add($"Input {i + 1}");
        }

        fluidInputDropdown.ClearOptions();

        if (options.Count == 0) {
            options.Add("No fluid inputs");
        }

        fluidInputDropdown.AddOptions(options);
        fluidInputDropdown.SetValueWithoutNotify(0);
        fluidInputDropdown.RefreshShownValue();

        fluidInputDropdown.interactable = machine.FluidInputSlotCount > 0;
    }

    public void Refresh() {
        if (machine == null) return;

        for (int i = 0; i < cellSlots.Count; i++) {
            int slotIndex = cellSlots[i];
            FluidCellData inventoryCell = playerInventory.GetSlotFluidCell(slotIndex);
            
            string label = inventoryCell != null
                ? GetCellLabel(slotIndex, inventoryCell)
                : $"Slot {slotIndex + 1}: Unavailable";

            fluidCellDropdown.options[i].text = label;
        }

        fluidCellDropdown.RefreshShownValue();

        FluidCellData cell = GetSelectedCell();
        int outputIndex = fluidOutputDropdown.value;
        bool hasOutput = outputIndex >= 0 && outputIndex < machine.FluidOutputSlotCount;

        FluidDefinition outputFluid = hasOutput
            ? machine.GetFluidOutputSlotFluid(outputIndex)
            : null;
        
        float outputAmount = hasOutput
            ? machine.GetFluidOutputSlotAmount(outputIndex)
            : 0f;

        string outputContents = outputFluid != null
            ? $"{outputFluid.FluidName} - {outputAmount:0.###}L"
            : "Empty";

        fluidTransferStatusText.text = cell == null
            ? $"Select a Fluid Cell\nOutput: {outputContents}"
            : $"{GetCellLabel(GetSelectedCellSlot(), cell)}\nOutput: {outputContents}";

        bool acceptsCellFluid = false;

        if (cell != null && cell.Fluid != null && machine.SelectedRecipe != null) {
            foreach (FluidAmount input in machine.SelectedRecipe.FluidInputs) {
                if (input.Fluid == cell.Fluid) {
                    acceptsCellFluid = true;
                    break;
                }
            }
        }

        drainCellButton.interactable = cell != null
            && cell.Amount > 0f
            && acceptsCellFluid;

        fillCellButton.interactable = cell != null
            && cell.Amount < cell.Capacity
            && outputFluid != null
            && outputAmount > 0f
            && (cell.Fluid == null || cell.Fluid == outputFluid);

        int inputIndex = fluidInputDropdown.value;
        bool hasInput = inputIndex >= 0 && inputIndex < machine.FluidInputSlotCount;

        FluidDefinition inputFluid = hasInput
            ? machine.GetFluidInputSlotFluid(inputIndex)
            : null;

        float inputAmount = hasInput
            ? machine.GetFluidInputSlotAmount(inputIndex)
            : 0f;

        string inputContents = inputFluid != null
            ? $"{inputFluid.FluidName} - {inputAmount:0.###}L"
            : "Empty";

        fluidTransferStatusText.text += $"\nInput: {inputContents}";

        returnFluidInputButton.interactable =
            cell != null
            && cell.Amount < cell.Capacity
            && inputFluid != null
            && inputAmount > 0f
            && (cell.Fluid == null || cell.Fluid == inputFluid);
    }

    private void OnSelectionChanged(int index) {
        Refresh();
    }

    private void DrainCell() {
        if (machine == null) return;

        float transferred = playerInventory.DrainFluidCellTo(
            GetSelectedCellSlot(),
            machine
        );

        if (transferred <= 0f) {
            Debug.Log("Machine input cannot accept this fluid");
        }

        Refresh();
    }

    private void FillCell() {
        if (machine == null) return;

        playerInventory.FillFluidCellFrom(
            GetSelectedCellSlot(),
            machine,
            fluidOutputDropdown.value
        );

        Refresh();
    }

    private void ReturnFluidInput() {
        if (machine == null) return;

        playerInventory.FillFluidCellFromMachineInput(
            GetSelectedCellSlot(),
            machine,
            fluidInputDropdown.value
        );

        Refresh();
    }
}
