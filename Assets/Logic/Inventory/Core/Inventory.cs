using UnityEngine;
using System;

public class Inventory : MonoBehaviour
{
    public event Action OnInventoryChanged;
    public InventoryItem[] Slots { get; private set; }

    private void Awake()
    {
        Slots = new InventoryItem[9]; // Default safety size
    }

    public void Resize(int newSize)
    {
        Slots = new InventoryItem[newSize];
        OnInventoryChanged?.Invoke();
        Debug.Log($"[Inventory] Resized to {newSize} slots.");
    }

    public void AddItem(ItemData newItem, int amount = 1)
    {
        if (newItem == null || amount <= 0) return;

        if (newItem.stackable)
        {
            for (int i = 0; i < Slots.Length; i++)
            {
                if (Slots[i] != null && Slots[i].data == newItem)
                {
                    Slots[i].quantity += amount;
                    OnInventoryChanged?.Invoke();
                    return;
                }
            }
        }

        for (int i = 0; i < Slots.Length; i++)
        {
            if (Slots[i] == null)
            {
                Slots[i] = new InventoryItem { data = newItem, quantity = amount };
                OnInventoryChanged?.Invoke();
                return;
            }
        }

        Debug.LogWarning("[Inventory] Inventory full.");
    }

    public void SwapItems(int indexA, int indexB)
    {
        if (!IsValidIndex(indexA) || !IsValidIndex(indexB) || indexA == indexB)
        {
            Debug.LogWarning($"[Inventory] Invalid swap: {indexA} ↔ {indexB}");
            return;
        }

        (Slots[indexA], Slots[indexB]) = (Slots[indexB], Slots[indexA]);
        OnInventoryChanged?.Invoke();
    }

    public InventoryItem GetItemAt(int index)
    {
        return IsValidIndex(index) ? Slots[index] : null;
    }

    public InventoryItem GetItemByCode(string itemCode)
    {
        if (string.IsNullOrWhiteSpace(itemCode)) return null;

        foreach (var item in Slots)
        {
            if (item != null && item.data != null && item.data.itemCode == itemCode)
            {
                return item;
            }
        }

        return null;
    }

    public bool TryConsumeItem(ItemData itemData, int amount = 1)
    {
        if (itemData == null || amount <= 0) return false;

        for (int i = 0; i < Slots.Length; i++)
        {
            var item = Slots[i];
            if (item != null && item.data == itemData && item.quantity >= amount)
            {
                item.quantity -= amount;
                if (item.quantity <= 0)
                    Slots[i] = null;

                OnInventoryChanged?.Invoke();
                return true;
            }
        }

        return false;
    }

    public int GetSlotCount() => Slots.Length;

    private bool IsValidIndex(int index) => index >= 0 && index < Slots.Length;
}
