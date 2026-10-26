using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DefaultExecutionOrder(-90)]
public class WorkbenchGUI : MonoBehaviour {
    [SerializeField] private Camera playerCamera;
    [SerializeField] private PlayerUIState uiState;
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private float interactionDistance = 6f;
    
    [SerializeField] GameObject workbenchPanel;
    [SerializeField] TMP_Dropdown recipeDropdown;
    [SerializeField] TMP_Text recipeText;
    [SerializeField] TMP_Text progressText;
    [SerializeField] Button craftButton;

    private readonly List<RecipeDefinition> displayedRecipes = new();

    private Workbench workbench;
    private bool isOpen;

    private void Awake() {
        craftButton.onClick.AddListener(StartCrafting);

        recipeDropdown.onValueChanged.AddListener(OnRecipeSelected);
    }

    private void Start() {
        workbenchPanel.SetActive(false);
    }

    private void Update() {
        if (isOpen) {
            bool closePressed = Keyboard.current != null && (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame);

            if (workbench == null || closePressed) {
                Close();
            } else {
                Refresh();
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

        Workbench target = hit.collider.GetComponentInParent<Workbench>();

        if (target == null || !uiState.TryOpen(this)) return;

        workbench = target;
        isOpen = true;

        PopulateRecipes();
        Refresh();

        workbenchPanel.SetActive(true);
    }

    private void OnDisable() {
        if (isOpen) Close();
    }

    private void OnDestroy() {
        craftButton.onClick.RemoveListener(StartCrafting);

        recipeDropdown.onValueChanged.RemoveListener(OnRecipeSelected);
    }

    private void Close() {
        if (workbench != null && workbench.HasContents) {
            workbench.CancelCrafting();
        }

        recipeDropdown.Hide();
        workbenchPanel.SetActive(false);

        workbench = null;
        isOpen = false;

        uiState.Close(this);
    }

    private RecipeDefinition GetSelectedRecipe() {
        int index = recipeDropdown.value;

        return index >= 0 && index < displayedRecipes.Count
            ? displayedRecipes[index]
            : null;
    }

    private void PopulateRecipes() {
        displayedRecipes.Clear();
        List<string> options = new();

        foreach (RecipeDefinition recipe in workbench.AvailableRecipes) {
            if (!workbench.SupportsRecipe(recipe)) continue;

            displayedRecipes.Add(recipe);
            options.Add(recipe.RecipeName);
        }

        recipeDropdown.ClearOptions();

        if (options.Count == 0) {
            options.Add("No recipes available");
        }

        recipeDropdown.AddOptions(options);
        recipeDropdown.SetValueWithoutNotify(0);
        recipeDropdown.RefreshShownValue();
    }

    private void OnRecipeSelected(int index) {
        if (isOpen && workbench != null) {
            Refresh();
        }
    }

    private void StartCrafting() {
        if (!isOpen || workbench == null) return;

        RecipeDefinition recipe = GetSelectedRecipe();

        if (recipe == null) return;

        workbench.TryStartCrafting(recipe, playerInventory);
        Refresh();
    }

    private void Refresh() {
        RecipeDefinition recipe = GetSelectedRecipe();
        
        craftButton.interactable = recipe != null && !workbench.HasContents;
        recipeDropdown.interactable = displayedRecipes.Count > 0 && !workbench.HasContents;

        if (workbench.IsCrafting) {
            int progress = Mathf.RoundToInt(workbench.CraftingProgress * 100f);

            progressText.text =
                $"Crafting: {workbench.ActiveRecipe.RecipeName}\n" +
                $"Progress: {progress}%";
        } else {
            progressText.text = workbench.HasContents
                ? "Status: Waiting to return ingredients"
                : "Status: Idle";
        }

        if (recipe == null) {
            recipeText.text = "No recipes available";
            return;
        }

        string text =
            $"{recipe.RecipeName} | Duration: {recipe.Duration:0.##}s\n\n" +
            "Ingredients:";

        foreach (ItemAmount input in recipe.Inputs) {
            int availableAmount = playerInventory.GetAmount(input.Item);

            text +=
                $"\n{input.Item.ItemName} × {input.Amount}" +
                $" (Inventory: {availableAmount})";
        }

        text += "\n\nProducts:";

        foreach (ItemAmount output in recipe.Outputs) {
            text += $"\n{output.Item.ItemName} × {output.Amount}";
        }

        if (recipeText.text != text) {
            recipeText.text = text;
        }
    }
}
