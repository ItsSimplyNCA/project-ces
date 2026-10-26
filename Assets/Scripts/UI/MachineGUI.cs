using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

[DefaultExecutionOrder(-90)]
public class MachineGUI : MonoBehaviour {
    [SerializeField] private Camera playerCamera;
    [SerializeField] private PlayerUIState uiState;
    [SerializeField] private GameObject machinePanel;
    [SerializeField] private float interactionDistance = 6f;
    [SerializeField] private TMP_Dropdown recipeDropdown;
    [SerializeField] private Button onOffButton;
    [SerializeField] private TMP_Text onOffButtonText;
    [SerializeField] private TMP_Text machineProgressText;
    [SerializeField] private MachineInteraction machineInteraction;
    [SerializeField] private Button insertInputsButton;
    [SerializeField] private Button collectOutputsButton;
    [SerializeField] private TMP_Text machineInventoryText;
    [SerializeField] private Button returnInputsButton;
    [SerializeField] private PortConfigurationGUI portConfigurationGUI;
    [SerializeField] private MachineFluidGUI machineFluidGUI;

    private Machine machine;
    private bool isOpen;
    private readonly List<RecipeDefinition> displayedRecipes = new();

    private void Awake() {
        recipeDropdown.onValueChanged.AddListener(OnRecipeSelected);
        onOffButton.onClick.AddListener(ToggleMachine);
        insertInputsButton.onClick.AddListener(InsertInputs);
        collectOutputsButton.onClick.AddListener(CollectOutputs);
        returnInputsButton.onClick.AddListener(ReturnInputs);
    }

    private void Start() {
        machinePanel.SetActive(false);
    }

    private void Update() {
        if (isOpen) {
            bool closePressed = Keyboard.current != null && (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame);

            if (machine == null || closePressed) {
                Close();
            } else {
                RefreshMachineState();
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

        Machine target = hit.collider.GetComponentInParent<Machine>();

        if (target == null || !uiState.TryOpen(this)) return;

        machine = target;
        isOpen = true;

        PopulateRecipes();
        portConfigurationGUI.Bind(machine.GetComponent<UniversalIO>());
        machineFluidGUI.Bind(machine);
        RefreshMachineState();

        machinePanel.SetActive(true);
    }

    private void Close() {
        recipeDropdown.Hide();
        portConfigurationGUI.Unbind();
        machineFluidGUI.Unbind();

        machinePanel.SetActive(false);

        machine = null;
        isOpen = false;

        uiState.Close(this);
    }

    private void OnDestroy() {
        recipeDropdown.onValueChanged.RemoveListener(OnRecipeSelected);
        onOffButton.onClick.RemoveListener(ToggleMachine);
        insertInputsButton.onClick.RemoveListener(InsertInputs);
        collectOutputsButton.onClick.RemoveListener(CollectOutputs);
        returnInputsButton.onClick.RemoveListener(ReturnInputs);
    }

    private void PopulateRecipes() {
        displayedRecipes.Clear();

        List<string> options = new();

        displayedRecipes.Add(null);
        options.Add("None");

        foreach (RecipeDefinition recipe in machine.AvailableRecipes) {
            if (!machine.SupportsRecipe(recipe)) continue;

            displayedRecipes.Add(recipe);
            options.Add(recipe.RecipeName);
        }

        recipeDropdown.ClearOptions();
        recipeDropdown.AddOptions(options);

        int selectedIndex = displayedRecipes.IndexOf(machine.SelectedRecipe);

        recipeDropdown.SetValueWithoutNotify(selectedIndex >= 0 ? selectedIndex : 0);

        recipeDropdown.RefreshShownValue();
    }

    private void OnRecipeSelected(int index) {
        if (!isOpen || machine == null) return;

        machine.TrySelectRecipe(displayedRecipes[index]);
    }

    private void ToggleMachine() {
        if (!isOpen || machine == null) return;

        machine.SetOn(!machine.IsOn);
        RefreshMachineState();
    }

    private void RefreshMachineState() {
        onOffButtonText.text = machine.IsOn ? "Turn OFF" : "Turn ON";

        if (machine.ActiveRecipe != null) {
            int progress = Mathf.RoundToInt(machine.CraftingProgress * 100f);
            string status = machine.IsOn ? "Crafting": "Paused";
            machineProgressText.text =
                $"{status}: {machine.ActiveRecipe.RecipeName}\n" +
                $"Progress: {progress}%";
        } else {
            machineProgressText.text = machine.IsOn ? "Status: Idle" : "Status: OFF";
        }

        RefreshMachineInventory();
        machineFluidGUI.Refresh();
    }

    private void InsertInputs() {
        if (!isOpen || machine == null) return;

        machineInteraction.InsertRecipeInputs(machine);
        RefreshMachineState();
    }

    private void CollectOutputs() {
        if (!isOpen || machine == null) return;

        machineInteraction.CollectOutputs(machine);
        RefreshMachineState();
    }

    private void ReturnInputs() {
        if (!isOpen || machine == null) return;

        machineInteraction.CollectInputs(machine);
        RefreshMachineState();
    }

    private void RefreshMachineInventory() {
        string text = "Inputs:";

        for (int i = 0; i < machine.InputSlotCount; i++) {
            ItemDefinition item = machine.GetInputSlotItem(i);

            text += item != null 
                ? $"\n{i + 1}. {item.ItemName} × {machine.GetInputSlotAmount(i)}"
                : $"\n{i + 1}. Empty";
        }

        text += "\n\nOutputs:";

        for (int i = 0; i < machine.OutputSlotCount; i++) {
            ItemDefinition item = machine.GetOutputSlotItem(i);

            text += item != null 
                ? $"\n{i + 1}. {item.ItemName} × {machine.GetOutputSlotAmount(i)}"
                : $"\n{i + 1}. Empty";
        }

        text += "\n\nFluid Inputs:";

        for (int i = 0; i < machine.FluidInputSlotCount; i++) {
            FluidDefinition fluid = machine.GetFluidInputSlotFluid(i);
            float amount = machine.GetFluidInputSlotAmount(i);

            text += fluid != null
                ? $"\n{i + 1}. {fluid.FluidName} - {amount:0.###}L"
                : $"\n{i + 1}. Empty";
        }

        text += "\n\nFluid Outputs:";

        for (int i = 0; i < machine.FluidOutputSlotCount; i++) {
            FluidDefinition fluid = machine.GetFluidOutputSlotFluid(i);
            float amount = machine.GetFluidOutputSlotAmount(i);

            text += fluid != null
                ? $"\n{i + 1}. {fluid.FluidName} - {amount:0.###}L"
                : $"\n{i + 1}. Empty";
        }

        if (machineInventoryText.text != text) {
            machineInventoryText.text = text;
        }
    }
}
