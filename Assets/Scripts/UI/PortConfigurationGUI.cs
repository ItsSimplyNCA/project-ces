using System.Collections.Generic;
using TMPro;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class PortConfigurationGUI : MonoBehaviour {
    [SerializeField] private TMP_Dropdown portDropdown;
    [SerializeField] private TMP_Dropdown portModeDropdown;
    [SerializeField] private TMP_Dropdown routingDropdown;

    private UniversalIO universalIO;
    private readonly List<PortMode> displayedPortModes = new();

    private void Awake() {
        List<string> modeOptions = new();

        foreach (PortMode mode in System.Enum.GetValues(typeof(PortMode))) {
            displayedPortModes.Add(mode);
            modeOptions.Add(mode.ToString());
        }

        portModeDropdown.ClearOptions();
        portModeDropdown.AddOptions(modeOptions);

        portDropdown.onValueChanged.AddListener(OnPortSelected);
        portModeDropdown.onValueChanged.AddListener(OnPortModeSelected);

        routingDropdown.ClearOptions();
        routingDropdown.AddOptions(new List<string> {
            "Nearest point",
            "Round Robin"
        });
        routingDropdown.onValueChanged.AddListener(OnRoutingModeSelected);
    }

    private void OnDestroy() {
        portDropdown.onValueChanged.RemoveListener(OnPortSelected);
        portModeDropdown.onValueChanged.RemoveListener(OnPortModeSelected);
        routingDropdown.onValueChanged.RemoveListener(OnRoutingModeSelected);
    }

    public void Bind(UniversalIO target) {
        routingDropdown.interactable = false;
        universalIO = target;

        bool hasPorts = universalIO != null && universalIO.Ports.Count > 0;

        portDropdown.interactable = hasPorts;
        portModeDropdown.interactable = hasPorts;

        List<string> options = new();

        if (hasPorts) {
            foreach (UniversalPort port in universalIO.Ports) {
                options.Add($"Cell {port.LocalCell} - {port.Face}");
            }
        } else {
            options.Add("No ports");
        }

        portDropdown.ClearOptions();
        portDropdown.AddOptions(options);
        portDropdown.SetValueWithoutNotify(0);
        portDropdown.RefreshShownValue();

        if (hasPorts) {
            OnPortSelected(0);
        }
    }

    public void Unbind() {
        portDropdown.Hide();
        portModeDropdown.Hide();
        routingDropdown.Hide();

        universalIO = null;

        portDropdown.interactable = false;
        portModeDropdown.interactable = false;
        routingDropdown.interactable = false;
    }

    private void OnPortSelected(int index) {
        if (universalIO == null || index < 0 || index >= universalIO.Ports.Count) return;

        UniversalPort port = universalIO.Ports[index];

        int modeIndex = displayedPortModes.IndexOf(port.Mode);

        portModeDropdown.SetValueWithoutNotify(modeIndex);
        portModeDropdown.RefreshShownValue();

        routingDropdown.interactable = port.Mode == PortMode.ItemOutput || port.Mode == PortMode.FluidOutput;

        routingDropdown.SetValueWithoutNotify(port.RoutingMode == ItemRoutingMode.NearestPoint ? 0 : 1);
        routingDropdown.RefreshShownValue();
    }

    private void OnPortModeSelected(int index) {
        if (universalIO == null || index < 0 || index >= displayedPortModes.Count) return;

        int portIndex = portDropdown.value;

        if (portIndex < 0 || portIndex >= universalIO.Ports.Count) return;

        universalIO.Ports[portIndex].SetMode(displayedPortModes[index]);

        universalIO.GetComponentInParent<GridWorld>()?.NotifyLogisticsChanged();

        OnPortSelected(portIndex);
    }

    private void OnRoutingModeSelected(int index) {
        if (universalIO == null || index < 0 || index > 1) return;

        int portIndex = portDropdown.value;

        if (portIndex < 0 || portIndex >= universalIO.Ports.Count) return;

        UniversalPort port = universalIO.Ports[portIndex];

        if (port.Mode != PortMode.ItemOutput && port.Mode != PortMode.FluidOutput) return;

        port.SetRoutingMode(index == 0 ? ItemRoutingMode.NearestPoint : ItemRoutingMode.RoundRobin);
    }
}
