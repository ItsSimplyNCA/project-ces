using UnityEngine;

[RequireComponent(typeof(PipeSegment))]
public class PipeVisual : MonoBehaviour {
    [SerializeField] private GameObject leftArm;
    [SerializeField] private GameObject rightArm;
    [SerializeField] private GameObject frontArm;
    [SerializeField] private GameObject backArm;
    [SerializeField] private GameObject topArm;
    [SerializeField] private GameObject bottomArm;

    private PipeSegment pipeSegment;

    private void Awake() {
        pipeSegment = GetComponent<PipeSegment>();
    }

    private void OnEnable() {
        pipeSegment.ConnectionsChanged += Refresh;
        Refresh();
    }

    private void OnDisabled() {
        pipeSegment.ConnectionsChanged -= Refresh;
    }

    private void Refresh() {
        RefreshArm(leftArm, Vector3.left);
        RefreshArm(rightArm, Vector3.right);
        RefreshArm(frontArm, Vector3.forward);
        RefreshArm(backArm, Vector3.back);
        RefreshArm(topArm, Vector3.up);
        RefreshArm(bottomArm, Vector3.down);
    }

    private void RefreshArm(GameObject arm, Vector3 localDirection) {
        if (arm == null) return;

        Vector3Int worldDirection = Vector3Int.RoundToInt(
            transform.TransformDirection(localDirection)
        );

        PipeConnections face = PipeSegment.GetFaceFromOffset(worldDirection);

        arm.SetActive(pipeSegment.HasConnection(face));
    }
}
