using System;

/// <summary>
/// Interface for an entity that can hold equipment and inventory.
/// </summary>
public interface IEquipmentHolder
{
    event Action<InventoryItem> OnEquippedItemChanged; // Fires when the actively equipped item changes
    ItemContainer GetContainerForInventory();      // Provides access to the main inventory container (for reloading etc.)
    InventoryItem GetCurrentEquippedItem();        // Gets the item currently considered equipped
}