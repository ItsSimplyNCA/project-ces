using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotView : MonoBehaviour, IPointerClickHandler {
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private Image background;
    [SerializeField] private Color selectedColor = Color.yellow;

    private ItemDefinition displayedItem;
    private int displayedAmount = -1;
    private int slotIndex;
    private Color normalColor;
    private FluidDefinition displayedFluid;
    private float displayedFluidAmount = -1f;

    public event Action<int> Clicked;

    private void Awake() {
        normalColor = background.color;
    }

    public void Initialize(int slotIndex) {
        this.slotIndex = slotIndex;
    }

    public void Refresh(ItemDefinition item, int amount, FluidCellData fluidCell = null) {
        FluidDefinition fluid = fluidCell != null ? fluidCell.Fluid : null;
        float fluidAmount = fluidCell != null ? fluidCell.Amount : 0f;

        if (
            displayedItem == item &&
            displayedAmount == amount &&
            displayedFluid == fluid &&
            displayedFluidAmount == fluidAmount
        ) return;

        displayedItem = item;
        displayedAmount = amount;
        displayedFluid = fluid;
        displayedFluidAmount = fluidAmount;

        if (item == null) {
            itemNameText.text = string.Empty;
            amountText.text = string.Empty;
            return;
        }

        if (item.IsFluidCell) {
            string fluidName = fluid != null ? fluid.FluidName : "Empty";

            itemNameText.text = $"{item.ItemName}\n{fluidName}";
            amountText.text = $"{fluidAmount:0.###} L";
        } else {
            itemNameText.text = item.ItemName;
            amountText.text = amount.ToString();
        }
    }

    public void SetSelected(bool selected) {
        background.color = selected ? selectedColor : normalColor;
    }

    public void OnPointerClick(PointerEventData eventData) {
        if (eventData.button == PointerEventData.InputButton.Left) {
            Clicked?.Invoke(slotIndex);
        }
    }
}
