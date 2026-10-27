using System.Collections.Generic;
using UnityEngine;

public class UniversalIO : MonoBehaviour {
    [SerializeField] private List<UniversalPort> ports = new();

    private float cellSize = 1f;

    public IReadOnlyList<UniversalPort> Ports => ports;

    public void Initialize(Vector3Int size, float cellSize) {
        this.cellSize = cellSize;
        
        ports.Clear();

        for (int x = 0; x < size.x; x++) {
            for (int z = 0; z < size.z; z++) {
                Vector3Int localCell = new Vector3Int(x, 0, z);

                ports.Add(new UniversalPort(localCell, PortFace.Bottom));
                if (x == 0) ports.Add(new UniversalPort(localCell, PortFace.Left));
                if (x == size.x - 1) ports.Add(new UniversalPort(localCell, PortFace.Right));
                if (z == 0) ports.Add(new UniversalPort(localCell, PortFace.Back));
                if (z == size.z - 1) ports.Add(new UniversalPort(localCell, PortFace.Front));
            }
        }
    }

    private static Vector3 GetLocalDirection(PortFace face) {
        return face switch {
            PortFace.Left => Vector3.left,
            PortFace.Right => Vector3.right,
            PortFace.Front => Vector3.forward,
            PortFace.Back => Vector3.back,
            PortFace.Bottom => Vector3.down,
            _ => throw new System.ArgumentOutOfRangeException(nameof(face))
        };
    }

    public Vector3 GetWorldPosition(UniversalPort port) {
        Vector3 cellCenter = (Vector3)port.LocalCell + Vector3.one * 0.5f;
        Vector3 faceCenter = cellCenter + GetLocalDirection(port.Face) * 0.5f;

        return transform.TransformPoint(faceCenter * cellSize);
    }

    public Vector3 GetWorldDirection(UniversalPort port) {
        return transform.TransformDirection(GetLocalDirection(port.Face));
    }

    private void OnDrawGizmosSelected() {
        if (ports == null) return;

        Gizmos.color = Color.cyan;

        foreach (UniversalPort port in ports) {
            Vector3 position = GetWorldPosition(port);
            Vector3 direction = GetWorldDirection(port);

            Gizmos.DrawSphere(position, cellSize * 0.06f);
            Gizmos.DrawRay(position, direction * cellSize * 0.25f);
        }
    }

    public Vector3Int GetAdjacentCell(UniversalPort port, GridWorld gridWorld) {
        Vector3 position = GetWorldPosition(port);
        Vector3 direction = GetWorldDirection(port);

        Vector3 neighborCenter = position + direction * (cellSize * 0.5f);

        return gridWorld.WorldToCell(neighborCenter);
    }

    [ContextMenu("Debug/Print Port Count")]
    private void PrintPortCountForDebug() {
        Debug.Log($"Universal I/O ports: {ports.Count}", this);
    }

    [ContextMenu("Debug/Print Port Neighbors")]
    private void PrintPortNeighborsForDebug() {
        if (!Application.isPlaying) return;

        GridWorld gridWorld = GetComponentInParent<GridWorld>();

        if (gridWorld == null) return;

        foreach (UniversalPort port in ports) {
            Vector3Int adjacentCell = GetAdjacentCell(port, gridWorld);

            Debug.Log(
                $"Port {port.LocalCell} / {port.Face}" +
                $"-> adjacent cell {adjacentCell}",
                this
            );
        }
    }
}
