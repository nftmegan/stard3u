// ========================
// IItemContainer.cs
// ========================
using System.Collections.Generic;

public interface IItemContainer
{
    bool TryInsert(ItemData item);
    bool TryRemove(ItemData item, int amount = 1);
    List<InventoryItem> GetContents();
}