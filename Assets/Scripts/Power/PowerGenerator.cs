using UnityEngine;

public class PowerGenerator : MonoBehaviour {
    [SerializeField] private PowerGeneratorDefinition definition;
    [SerializeField] private bool isOn = true;

    private bool isTripped;

    public bool IsOn => isOn;
    public bool IsTripped => isTripped;
    public float AvailablePowerMW => definition != null && isOn && !isTripped ? definition.OutputMW : 0f;

    public void SetOn(bool value) {
        isOn = value;

        if (value) {
            isTripped = false;
        }
    }

    public void Trip() {
        if (isTripped) return;

        isTripped = true;
        isOn = false;

        Debug.LogWarning("Generator tripped: overload", this);
    }

    public PowerGeneratorSaveData CaptureSaveData() {
        return new PowerGeneratorSaveData {
            isOn = this.isOn,
            isTripped = this.isTripped
        };
    }

    public bool ValidateSaveData(PowerGeneratorSaveData data) {
        if (data == null) {
            Debug.LogWarning("Generator validation failed: missing data.", this);
            return false;
        }

        if (data.isTripped && data.isOn) {
            Debug.LogWarning("Generator validation failed: a tripped generator cannot be ON.", this);
            return false;
        }

        return true;
    }

    public bool TryRestoreSaveData(PowerGeneratorSaveData data) {
        if (!Application.isPlaying) {
            Debug.LogWarning("Generator state can only be restored in Play mode.", this);
            return false;
        }

        if (!ValidateSaveData(data)) {
            return false;
        }

        isOn = data.isOn;
        isTripped = data.isTripped;

        return true;
    }

    // DEBUG METHODS
    [ContextMenu("Debug/Toggle ON-OFF")]
    private void ToggleForDebug() {
        if (!Application.isPlaying) return;

        SetOn(!isOn);
    }
}
