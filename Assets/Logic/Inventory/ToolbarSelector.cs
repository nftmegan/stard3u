using UnityEngine;
using System;

public class ToolbarSelector : MonoBehaviour
{
    [SerializeField] private int toolbarSlotCount = 9; // Number of slots on the toolbar
    private Inventory inventory;
    private int selectedIndex = 0;

    public event Action<InventoryItem> OnSelectedItemChanged;
    public event Action<int> OnSelectedIndexChanged;

    // Initialize ToolbarSelector with the player's inventory
    public void Initialize(Inventory inventory)
    {
        this.inventory = inventory;
        NotifySelection();
    }

    // Update the selected slot
    public void UpdateSelection(int newIndex)
    {
        selectedIndex = newIndex;
        NotifySelection();
    }

    // Notify about the selection change
    private void NotifySelection()
    {
        // Get the InventorySlot at the selected index and invoke the event
        InventorySlot selectedSlot = inventory?.GetSlotAt(selectedIndex);
        if (selectedSlot != null)
        {
            OnSelectedItemChanged?.Invoke(selectedSlot.item);
        }
        OnSelectedIndexChanged?.Invoke(selectedIndex);
    }

    // Get the count of toolbar slots
    public int GetToolbarSlotCount() => toolbarSlotCount;

    // Get the index of the selected slot
    public int GetSelectedIndex() => selectedIndex;

    // Get the selected InventoryItem (from the selected slot)
    public InventoryItem GetSelectedItem()
    {
        InventorySlot selectedSlot = inventory?.GetSlotAt(selectedIndex);
        return selectedSlot?.item;
    }
}
