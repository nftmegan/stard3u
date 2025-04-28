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
    public void AddQuantity(int amt)     => quantity += amt;
    public bool HasEnough(int amt)       => quantity >= amt;

    /// <summary>Subtract and auto-clear when empty.</summary>
    public void ReduceQuantity(int amt)
    {
        quantity = Mathf.Max(0, quantity - amt);
        if (quantity == 0) Clear();
    }

    public bool IsEmpty() => item == null || quantity <= 0;

    /// <summary>Return this slot to a completely empty state.</summary>
    public void Clear()
    {
        item     = null;
        quantity = 0;
    }
}