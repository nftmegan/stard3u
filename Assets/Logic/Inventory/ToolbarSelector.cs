using UnityEngine;
using System;

public class ToolbarSelector : MonoBehaviour
{
    [SerializeField] private int toolbarSlotCount = 9;
    private Inventory inventory;
    private int selectedIndex = 0;

    public event Action<InventoryItem> OnSelectedItemChanged;
    public event Action<int> OnSelectedIndexChanged;

    public void Initialize(Inventory inventory)
    {
        this.inventory = inventory;
        NotifySelection();
    }

    public void UpdateSelection(int newIndex)
    {
        selectedIndex = newIndex;
        NotifySelection();
    }

    private void NotifySelection()
    {
        OnSelectedItemChanged?.Invoke(inventory?.GetItemAt(selectedIndex));
        OnSelectedIndexChanged?.Invoke(selectedIndex);
    }

    public int GetToolbarSlotCount() => toolbarSlotCount;
    public int GetSelectedIndex() => selectedIndex;
    public InventoryItem GetSelectedItem() => inventory?.GetItemAt(selectedIndex);
}
