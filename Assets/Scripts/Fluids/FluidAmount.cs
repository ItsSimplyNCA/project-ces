using System;
using UnityEngine;

[Serializable]
public class FluidAmount {
    [SerializeField] private FluidDefinition fluid;
    [SerializeField, Min(0.001f)] private float amount = 1f;

    public FluidDefinition Fluid => fluid;
    public float Amount => amount;
}