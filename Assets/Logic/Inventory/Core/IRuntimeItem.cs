// ========================
// IRuntimeItem.cs
// ========================
public interface IRuntimeItem
{
    InventoryItem GetInventoryItem();
    void SetInventoryItem(InventoryItem item);
    ItemData GetItemData(); // Optional helper for quick access
}