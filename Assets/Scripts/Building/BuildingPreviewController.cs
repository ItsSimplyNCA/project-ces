using UnityEngine;
using UnityEngine.InputSystem;

public class BuildingPreviewController : MonoBehaviour {
    [SerializeField] private Camera playerCamera;
    [SerializeField] private GridWorld gridWorld;
    [SerializeField] private BuildingDefinition buildingDefinition;
    [SerializeField] private float buildDistance = 6f;
    [SerializeField] private float deconstructDuration = 1f;
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private Material validPreviewMaterial;
    [SerializeField] private Material invalidPreviewMaterial;
    [SerializeField] private PlayerUIState uiState;
    [SerializeField] private BuildingDefinition[] availableBuildings;
    [SerializeField] private ConfirmationGUI confirmationGUI;

    private Renderer[] previewRenderers;
    private BuildMode mode = BuildMode.Build;
    private BuildingInstance deconstructTarget;
    private float deconstructElapsed;
    private GameObject preview;
    private int quarterTurns;

    public bool IsDeconstructMode => mode == BuildMode.Deconstruct;
    public bool IsConfigurationMode => mode == BuildMode.Configuration;
    public float DeconstructProgress => deconstructDuration > 0f
        ? Mathf.Clamp01(deconstructElapsed / deconstructDuration)
        : 0f;
    public BuildingDefinition SelectedBuilding => buildingDefinition;

    private void Start() {
        CreatePreview();
    }

    private void Update() {
        if (uiState.IsOpen) {
            preview.SetActive(false);
            ResetDeconstruction();
            return;
        }

        if (Keyboard.current != null) {
            if (Keyboard.current.xKey.wasPressedThisFrame) {
                mode = mode == BuildMode.Deconstruct ? BuildMode.Build : BuildMode.Deconstruct;
                ResetDeconstruction();
            } else if (Keyboard.current.cKey.wasPressedThisFrame) {
                mode = mode == BuildMode.Configuration ? BuildMode.Build : BuildMode.Configuration;
                ResetDeconstruction();
            }
        }

        if (mode == BuildMode.Configuration) {
            preview.SetActive(false);
            return;
        }

        if (mode == BuildMode.Deconstruct) {
            preview.SetActive(false);
            UpdateDeconstruction();
            return;
        }

        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) {
            quarterTurns = (quarterTurns + 1) % 4;
        }

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (!Physics.Raycast(ray, out RaycastHit hit, buildDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) {
            preview.SetActive(false);
            return;
        }

        Vector3 samplePoint = hit.point + hit.normal * 0.001f;
        Vector3Int origin = gridWorld.WorldToCell(samplePoint);

        preview.transform.SetPositionAndRotation(
            gridWorld.GetPlacementPosition(origin, buildingDefinition, quarterTurns),
            gridWorld.GetRotation(quarterTurns)
        );

        bool hasItem = playerInventory.GetAmount(buildingDefinition.Item) > 0;

        bool canPlace = hasItem && gridWorld.CanPlace(origin, buildingDefinition, quarterTurns);

        preview.SetActive(true);

        Material previewMaterial = canPlace ? validPreviewMaterial : invalidPreviewMaterial;

        foreach (Renderer previewRenderer in previewRenderers) {
            previewRenderer.sharedMaterial = previewMaterial;
        }

        if (canPlace && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) {
            bool removedItem = playerInventory.TryRemove(buildingDefinition.Item, 1);

            if (removedItem) {
                bool placed = gridWorld.TryPlace(origin, buildingDefinition, quarterTurns);

                if (!placed) {
                    playerInventory.Add(buildingDefinition.Item, 1);
                }
            }
        }
    }

    private void UpdateDeconstruction() {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        bool hasHit = Physics.Raycast(
            ray,
            out RaycastHit hit,
            buildDistance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore
        );

        BuildingInstance target = hasHit ? hit.collider.GetComponentInParent<BuildingInstance>() : null;

        if (target == null || Mouse.current == null || !Mouse.current.leftButton.isPressed) {
            ResetDeconstruction();
            return;
        }

        if (target != deconstructTarget) {
            deconstructTarget = target;
            deconstructElapsed = 0f;
        }

        deconstructElapsed += Time.deltaTime;

        if (deconstructElapsed >= deconstructDuration) {
            BuildingInstance building = deconstructTarget;

            ResetDeconstruction();
            RequestDeconstruction(building);
        }
    }

    private void ResetDeconstruction() {
        deconstructTarget = null;
        deconstructElapsed = 0f;
    }

    private void CreatePreview() {
        preview = Instantiate(buildingDefinition.Prefab);
        preview.name = "Building Preview";

        previewRenderers = preview.GetComponentsInChildren<Renderer>(true);

        foreach (Collider collider in preview.GetComponentsInChildren<Collider>()) {
            collider.enabled = false;
        }

        preview.SetActive(false);
    }

    public bool TrySelectBuilding(ItemDefinition item) {
        if (item == null) return false;

        foreach (BuildingDefinition definition in availableBuildings) {
            if (definition == null || definition.Item != item) continue;
            if (definition.Prefab == null) {
                Debug.LogError($"Missing prefab on building definition: {definition.name}", definition);
            }

            if (definition == buildingDefinition) {
                mode = BuildMode.Build;
                ResetDeconstruction();
                return true;
            }

            preview.SetActive(false);
            Destroy(preview);

            buildingDefinition = definition;
            quarterTurns = 0;
            mode = BuildMode.Build;

            ResetDeconstruction();
            CreatePreview();

            return true;
        }

        return false;
    }

    private void RequestDeconstruction(BuildingInstance building) {
        if (building == null) return;

        ItemDefinition item = building.Definition.Item;

        IDeconstructionContents contents = building.GetComponent<IDeconstructionContents>();

        bool losesContents = contents != null && contents.HasContents;
        bool losesBuildingItem = !playerInventory.CanAdd(item, 1);

        if (!losesContents && !losesBuildingItem) {
            CompleteDeconstruction(building, item);
            return;
        }

        string message = "Deconstruct this building";

        if (contents != null) {
            message += "\nAny contents or active crafting present when confirmed will be lost.";
        }

        if (losesBuildingItem) {
            message += "\nInventory full: the returned building item will be lost.";
        }

        confirmationGUI.Show(
            message,
            () => CompleteDeconstruction(building, item)
        );
    }

    private void CompleteDeconstruction(BuildingInstance building, ItemDefinition item) {
        if (building == null) return;

        if (gridWorld.TryRemoveBuilding(building)) {
            playerInventory.Add(item, 1);
        }
    }

    private void OnDestroy() {
        if (preview != null) {
            Destroy(preview);
        }
    }
}
