using UnityEngine;
using System;

public class PlayerInventory : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Inventory inventory;
    [SerializeField] private ToolbarSelector toolbarSelector;
    public InventoryUIManager inventoryUIManager; // ✅ Direct reference to UI Manager!

    public event Action<InventoryItem> OnSelectedItemChanged;

    private int selectedSlot = 0;
    private InventoryItem equippedItemCache; // ✅ Track currently equipped item

    private void Awake()
    {
        if (inventory == null)
            inventory = GetComponent<Inventory>();

        if (toolbarSelector == null)
            toolbarSelector = GetComponent<ToolbarSelector>();
    }

    public void Initialize()
    {
        toolbarSelector.Initialize(inventory);
        SelectSlot(0);
        inventory.OnInventoryChanged += RefreshUI;

        // Initialize cache
        equippedItemCache = inventory.GetItemAt(selectedSlot);
    }

    public void HandleInput(IPlayerInput input)
    {
        if (input == null) return;

        for (int i = 0; i < 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                SelectSlot(i);
                return;
            }
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll < 0f) // Scroll down → next
        {
            if (selectedSlot < toolbarSelector.GetToolbarSlotCount() - 1)
                SelectSlot(selectedSlot + 1);
        }
        else if (scroll > 0f) // Scroll up → previous
        {
            if (selectedSlot > 0)
                SelectSlot(selectedSlot - 1);
        }
    }

    private void SelectSlot(int newIndex)
    {
        if (newIndex < 0 || newIndex >= toolbarSelector.GetToolbarSlotCount())
            return;

        if (selectedSlot != newIndex)
        {
            selectedSlot = newIndex;
            NotifySelection();
        }
    }

    private void NotifySelection()
    {
        InventoryItem selectedItem = inventory.GetItemAt(selectedSlot);
        equippedItemCache = selectedItem; // ✅ Update cache
        OnSelectedItemChanged?.Invoke(selectedItem);

        toolbarSelector.UpdateSelection(selectedSlot);
        inventoryUIManager?.RefreshUI();
    }

    private void RefreshUI()
    {
        inventoryUIManager?.RefreshUI();
        CheckSelectedItem(); // ✅ Check if item changed after inventory refresh
    }

    private void CheckSelectedItem()
    {
        InventoryItem newSelectedItem = inventory.GetItemAt(selectedSlot);
        if (newSelectedItem != equippedItemCache)
        {
            Debug.Log("[PlayerInventory] Detected slot item change → Re-equipping.");
            equippedItemCache = newSelectedItem;
            OnSelectedItemChanged?.Invoke(newSelectedItem);
        }
    }

    // === Public Methods ===
    public void AddItem(ItemData itemData, int amount = 1)
    {
        inventory?.AddItem(itemData, amount);
        inventoryUIManager?.RefreshUI();
        CheckSelectedItem(); // ✅
    }

    public bool HasItem(ItemData itemData)
    {
        if (itemData == null) return false;

        foreach (var item in inventory.Slots)
        {
            if (item != null && item.data == itemData && item.quantity > 0)
                return true;
        }
        return false;
    }

    public InventoryItem FindItem(ItemData itemData)
    {
        if (itemData == null) return null;

        foreach (var item in inventory.Slots)
        {
            if (item != null && item.data == itemData && item.quantity > 0)
                return item;
        }
        return null;
    }

    public bool TryConsumeItem(ItemData itemData)
    {
        bool result = inventory?.TryConsumeItem(itemData) ?? false;
        if (result)
        {
            inventoryUIManager?.RefreshUI();
            CheckSelectedItem(); // ✅
        }
        return result;
    }

    public void SwapItems(int indexA, int indexB)
    {
        inventory?.SwapItems(indexA, indexB);
        inventoryUIManager?.RefreshUI();
        CheckSelectedItem(); // ✅
    }

    public InventoryItem GetSelectedItem() => inventory.GetItemAt(selectedSlot);
    public int GetSelectedIndex() => selectedSlot;
    public InventoryItem GetItemAt(int index) => inventory.GetItemAt(index);
    public void ResizeInventory(int newSize) => inventory?.Resize(newSize);

    public Inventory GetInventory() => inventory;
    public ToolbarSelector GetToolbarSelector() => toolbarSelector;

    // === Selection Subscription for Equipment Controller ===
    public void SubscribeToSelectionChanges(Action<InventoryItem> callback)
    {
        OnSelectedItemChanged += callback;
    }

    public void UnsubscribeFromSelectionChanges(Action<InventoryItem> callback)
    {
        OnSelectedItemChanged -= callback;
    }
}
