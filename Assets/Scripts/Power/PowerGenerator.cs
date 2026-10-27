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

    [ContextMenu("Debug/Toggle ON-OFF")]
    private void ToggleForDebug() {
        if (!Application.isPlaying) return;

        SetOn(!isOn);
    }
}
