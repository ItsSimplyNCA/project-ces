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

    public PowerBreakerSaveData CaptureSaveData() {
        return new PowerBreakerSaveData {
            isClosed = this.isClosed,
            isTripped = this.isTripped
        };
    }

    public bool ValidateSaveData(PowerBreakerSaveData data) {
        if (data == null) {
            Debug.LogWarning("Breaker validation failed: missing data.", this);
            return false;
        }

        if (data.isTripped && data.isClosed) {
            Debug.LogWarning("Breaker validation failed: a tripped breaker cannot be closed.", this);
            return false;
        }

        return true;
    }

    public bool TryRestoreSaveData(PowerBreakerSaveData data) {
        if (!Application.isPlaying) {
            Debug.LogWarning("Breaker state can only be restored in Play mode.", this);
            return false;
        }

        if (!ValidateSaveData(data)) return false;

        bool wasConducting = ConductsPower;
        isClosed = data.isClosed;
        isTripped = data.isTripped;

        if (ConductsPower != wasConducting) {
            NotifyNetworkChanged();
        }

        return true;
    }

    // DEBUG METHODS
    [ContextMenu("Debug/Toggle Breaker")]
    private void ToggleForDebug() {
        if (!Application.isPlaying) return;
        SetClosed(!isClosed);
    }
}
