using System;
using UnityEngine;

[Serializable]
public class FluidCellData {
    [SerializeField] private FluidDefinition fluid;
    [SerializeField] private float amount;

    public FluidDefinition Fluid => fluid;
    public float Amount => amount;
    public float Capacity => 1f;

    public float Add(FluidDefinition fluidToAdd, float requestedAmount) {
        if (fluidToAdd == null || requestedAmount <= 0f) return 0f;
        if (fluid != null && fluid != fluidToAdd) return 0f;

        float availableSpace = Capacity - amount;
        float addedAmount = Mathf.Min(
            requestedAmount,
            availableSpace
        );

        if (addedAmount <= 0f) return 0f;

        fluid = fluidToAdd;
        amount += addedAmount;

        return addedAmount;
    }

    public float Remove(float requestedAmount) {
        if (requestedAmount <= 0f || amount == 0f) return 0f;

        float removedAmount = Mathf.Min(
            requestedAmount,
            amount
        );

        amount -= removedAmount;

        if (amount == 0f) {
            fluid = null;
        }

        return removedAmount;
    }
}
