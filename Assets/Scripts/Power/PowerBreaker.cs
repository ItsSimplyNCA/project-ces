using UnityEngine;

public class PowerBreaker : MonoBehaviour {
    [SerializeField] private bool isClosed = true;

    private bool isTripped;

    public bool IsClosed => isClosed;
    public bool IsTripped => isTripped;
    public bool ConductsPower => isClosed && !isTripped;

    public void SetClosed(bool value) {
        bool wasConducting = ConductsPower;

        isClosed = value;

        if (value) {
            isTripped = false;
        }

        if (ConductsPower != wasConducting) {
            NotifyNetworkChanged();
        }
    }

    public void Trip() {
        if (isTripped) return;

        isTripped = true;
        isClosed = false;

        NotifyNetworkChanged();

        Debug.LogWarning("Breaker tripped: overload", this);
    }

    private void NotifyNetworkChanged() {
        GetComponentInParent<GridWorld>()?.NotifyLogisticsChanged();
    }

    [ContextMenu("Debug/Toggle Breaker")]
    private void ToggleForDebug() {
        if (!Application.isPlaying) return;
        SetClosed(!isClosed);
    }
}
