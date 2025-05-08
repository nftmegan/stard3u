// In Assets/Scripts/Player/Equipment/IEquipmentHolder.cs (or your path)
using System;

/// <summary>
/// Interface for an entity that can hold and manage equipment and inventory.
/// Typically implemented by the PlayerInventory component or an AI equivalent.
/// </summary>
public interface IEquipmentHolder {
    // --- Equipment Selection & State ---
    event Action<InventoryItem> OnEquippedItemChanged;
    InventoryItem GetCurrentEquippedItem();
    ItemContainer GetContainerForInventory(); // Main inventory data container

    // --- Inventory Interaction Requests ---
    bool RequestAddItemToInventory(InventoryItem itemToAdd);
    bool CanAddItemToInventory(InventoryItem itemToCheck, int quantity = 1);
    bool RequestConsumeItem(ItemData itemData, int amount = 1);
    bool HasItemInInventory(ItemData itemData, int amount = 1);
}