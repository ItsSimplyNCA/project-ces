using UnityEngine;
using UnityEngine.Rendering;

public class BuildingTargetOutline : MonoBehaviour {
    [SerializeField] private Camera playerCamera;
    [SerializeField] private GridWorld gridWorld;
    [SerializeField] private PlayerUIState uiState;
    [SerializeField] private Material outlineMaterial;
    [SerializeField] private float interactionDistance = 6f;
    [SerializeField] private float lineWidth = 0.015f;
    [SerializeField] private float padding = 0.01f;

    private readonly LineRenderer[] edges = new LineRenderer[12];
    private readonly Vector3[] corners = new Vector3[8];

    private static readonly int[] edgeIndices = {
        0, 1, 1, 2, 2, 3, 3, 0,
        4, 5, 5, 6, 6, 7, 7, 4,
        0, 4, 1, 5, 2, 6, 3, 7
    };

    private void Awake() {
        for (int i = 0; i < edges.Length; i++) {
            GameObject edgeObject = new($"Outline Edge {i}");
            edgeObject.transform.SetParent(transform, false);

            LineRenderer line = edgeObject.AddComponent<LineRenderer>();

            line.sharedMaterial = outlineMaterial;
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.startWidth = lineWidth;
            line.endWidth = lineWidth;
            line.startColor = Color.white;
            line.endColor = Color.white;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.enabled = false;

            edges[i] = line;
        }
    }

    private void LateUpdate() {
        if (uiState.IsOpen) {
            Hide();
            return;
        }

        Ray ray = playerCamera.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f)
        );

        bool hasHit = Physics.Raycast(
            ray,
            out RaycastHit hit,
            interactionDistance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore
        );

        if (!hasHit) {
            Hide();
            return;
        }

        BuildingInstance building = hit.collider.GetComponentInParent<BuildingInstance>();

        if (building == null || building.Definition == null) {
            Hide();
            return;
        }

        Show(building);
    }

    private void Show(BuildingInstance building) {
        Vector3Int size = gridWorld.GetRotatedSize(
            building.Definition,
            building.QuarterTurns
        );

        Vector3 min = gridWorld.CellToWorldCorner(building.Origin)
            - Vector3.one * padding;

        Vector3 max = gridWorld.CellToWorldCorner(building.Origin)
            + (Vector3)size * gridWorld.CellSize
            + Vector3.one * padding;

        corners[0] = new Vector3(min.x, min.y, min.z);
        corners[1] = new Vector3(max.x, min.y, min.z);
        corners[2] = new Vector3(max.x, min.y, max.z);
        corners[3] = new Vector3(min.x, min.y, max.z);

        corners[4] = new Vector3(min.x, max.y, min.z);
        corners[5] = new Vector3(max.x, max.y, min.z);
        corners[6] = new Vector3(max.x, max.y, max.z);
        corners[7] = new Vector3(min.x, max.y, max.z);

        for (int i = 0; i < edges.Length; i++) {
            edges[i].SetPosition(0, corners[edgeIndices[i * 2]]);
            edges[i].SetPosition(1, corners[edgeIndices[i * 2 + 1]]);
            edges[i].enabled = true;
        }
    }

    private void Hide() {
        foreach (LineRenderer edge in edges) {
            edge.enabled = false;
        }
    }

    private void OnDisable() {
        Hide();
    }
}
