using UnityEngine;

[System.Serializable]
public class InventorySlot
{
    public InventoryItem item;
    public int           quantity;

    public InventorySlot(InventoryItem item, int qty)
    {
        this.item     = item;
        this.quantity = qty;
    }

    /* ---------- helpers ---------- */
    /// <summary>
    /// Add to the stack without exceeding its max size.
    /// Returns the number actually added.
    /// </summary>
    public int AddQuantity(int amt)
    {
        if (item == null || amt <= 0) return 0;

        int max    = item.data.stackable ? Mathf.Max(1, item.data.maxStack) : 1;
        int space  = max - quantity;
        int added  = Mathf.Clamp(amt, 0, space);

        quantity += added;
        return added;
    }

    public bool HasEnough(int amt) => quantity >= amt;

    public void ReduceQuantity(int amt)
    {
        quantity = Mathf.Max(0, quantity - amt);
        if (quantity == 0) Clear();
    }

    public bool IsEmpty() => item == null || quantity <= 0;

    public void Clear()
    {
        item     = null;
        quantity = 0;
    }

    public bool IsFull() => item != null &&
                            quantity >= (item.data.stackable
                                         ? Mathf.Max(1, item.data.maxStack)
                                         : 1);
}