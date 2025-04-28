using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class InventoryItem : IItemContainer
{
    public ItemData data;
    public int quantity = 1;

    [SerializeReference] private List<InventoryItem> internalItems = new();

    public bool IsStackable => data != null && data.stackable;

    public bool TryInsert(ItemData item)
    {
        if (data == null || !(data.category == ItemCategory.Weapon || data.category == ItemCategory.Tool))
            return false;

        if (item == null) return false;

        internalItems.Add(new InventoryItem { data = item, quantity = 1 });
        return true;
    }

    public bool TryRemove(ItemData item, int amount = 1)
    {
        var found = internalItems.Find(i => i.data == item);
        if (found != null && found.quantity >= amount)
        {
            found.quantity -= amount;
            if (found.quantity <= 0)
                internalItems.Remove(found);
            return true;
        }
        return false;
    }

    public List<InventoryItem> GetContents() => internalItems;
}

