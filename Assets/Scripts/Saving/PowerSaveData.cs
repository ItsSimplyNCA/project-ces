using System;
using UnityEngine;

[Serializable] public class PowerGeneratorSaveData {
    public bool isOn;
    public bool isTripped;
}

[Serializable] public class PowerBreakerSaveData {
    public bool isClosed;
    public bool isTripped;
}

[Serializable] public class PowerStorageSaveData {
    public float storedPower;
}