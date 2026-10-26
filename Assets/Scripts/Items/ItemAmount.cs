using System;
using UnityEngine;

[Serializable]
public class ItemAmount {
    [SerializeField] private ItemDefinition item;
    [SerializeField, Min(1)] private int amount = 1;

    public ItemDefinition Item => item;
    public int Amount => amount;
}
