using System;
using UnityEngine;

/// <summary>A reusable stack-based container (backpack, toolbar, magazine, etc.).</summary>
/// 
[Serializable]
public class ItemContainer
{
    public event Action<int> OnSlotChanged;  // -1  => structural change

    private InventorySlot[] _slots;
    public  InventorySlot[]  Slots => _slots;   // expose but don’t allow ref

    /* ───────────────────────── ctor ───────────────────────── */
    public ItemContainer(int size)
    {
        size = Mathf.Max(1, size);
        _slots = new InventorySlot[size];
        for (int i = 0; i < size; i++)
            _slots[i] = new InventorySlot(null, 0);
    }

    /* ───────────────────── structure ─────────────────────── */
    public int Size => Slots.Length;

    public void Resize(int newSize)
    {
        newSize = Mathf.Max(1, newSize);
        Array.Resize(ref _slots, newSize);          // ✅ now a field
        for (int i = 0; i < newSize; i++)
            if (_slots[i] == null) _slots[i] = new InventorySlot(null, 0);

        OnSlotChanged?.Invoke(-1);
    }

    public InventorySlot this[int i] => _slots[i];

    /* ───────────────────── helpers (ported from old Inventory) ───────────────────── */

    public void AddItem(InventoryItem incoming, int quantity = 1)
    {
        if (incoming == null || quantity <= 0) return;

        bool isSimpleStack = incoming.runtime == null && incoming.IsStackable;

        // ── (A) stackables → merge / split ───────────────────────────
        if (isSimpleStack)
        {
            int maxPerStack = Mathf.Max(1, incoming.data.maxStack);
            int remaining   = quantity;

            // top-up
            for (int i = 0; i < Slots.Length && remaining > 0; i++)
            {
                var s = Slots[i];
                if (s.item == null || s.item.data != incoming.data || s.IsFull()) continue;

                remaining -= s.AddQuantity(remaining);
                OnSlotChanged?.Invoke(i);
            }

            // into empty slots
            for (int i = 0; i < Slots.Length && remaining > 0; i++)
            {
                if (Slots[i].item != null && Slots[i].quantity > 0) continue;

                int toPlace = Mathf.Min(maxPerStack, remaining);
                Slots[i] = new InventorySlot(incoming, toPlace);
                remaining -= toPlace;
                OnSlotChanged?.Invoke(i);
            }

            if (remaining > 0)
                Debug.LogWarning($"[ItemContainer] Full – couldn’t add {remaining} × {incoming.data.itemName}");
            return;
        }

        // ── (B) complex item → store single reference ───────────────
        if (quantity > 1)
            Debug.LogWarning("[ItemContainer] Complex items must have quantity = 1. Ignoring extras.");

        for (int i = 0; i < Slots.Length; i++)
        {
            if (Slots[i].item == null || Slots[i].quantity == 0)
            {
                Slots[i] = new InventorySlot(incoming, 1);
                OnSlotChanged?.Invoke(i);
                return;
            }
        }
        Debug.LogWarning("[ItemContainer] Full – couldn’t add complex item");
    }

    public bool TryConsumeItem(ItemData data, int amount = 1)
    {
        if (data == null || amount <= 0) return false;

        /* total available? */
        int total = 0;
        foreach (var s in Slots)
            if (s.item != null && s.item.data == data)
                total += s.quantity;
        if (total < amount) return false;

        /* burn across stacks */
        int remaining = amount;
        for (int i = 0; i < Slots.Length && remaining > 0; i++)
        {
            var s = Slots[i];
            if (s.item == null || s.item.data != data) continue;

            int take = Mathf.Min(s.quantity, remaining);
            s.ReduceQuantity(take);
            remaining -= take;
            OnSlotChanged?.Invoke(i);
        }
        return true;
    }

    public bool Withdraw(ItemData data, int amount, InventorySlot targetSlot)
    {
        if (data == null || amount <= 0 || targetSlot == null) return false;

        int remaining = amount;
        for (int i = 0; i < Slots.Length && remaining > 0; i++)
        {
            var src = Slots[i];
            if (src.item == null || src.item.data != data) continue;

            int take = Mathf.Min(src.quantity, remaining);

            if (targetSlot.item == null)
                targetSlot.item = InventoryItem.CreateStack(data);

            targetSlot.AddQuantity(take);
            src.ReduceQuantity(take);

            remaining -= take;
            OnSlotChanged?.Invoke(i);
        }
        return remaining == 0;
    }

    public void MergeOrSwap(int a, int b)
    {
        if (a < 0 || b < 0 || a >= Slots.Length || b >= Slots.Length || a == b) return;

        var src = Slots[a];
        var dst = Slots[b];

        /* merge if same stackable item */
        if (src.item != null && dst.item != null &&
            src.item.data == dst.item.data &&
            src.item.data.stackable)
        {
            int max   = Mathf.Max(1, src.item.data.maxStack);
            int space = max - dst.quantity;

            if (space > 0)
            {
                int moved = Mathf.Min(space, src.quantity);
                dst.AddQuantity(moved);
                src.ReduceQuantity(moved);
                OnSlotChanged?.Invoke(a);
                OnSlotChanged?.Invoke(b);
                return;
            }
        }

        /* otherwise swap */
        (Slots[a], Slots[b]) = (Slots[b], Slots[a]);
        OnSlotChanged?.Invoke(a);
        OnSlotChanged?.Invoke(b);
    }

    public bool HasItem(ItemData itemData, int amount = 1)
    {
        if (itemData == null || amount <= 0) return false;
        int count = 0;
        foreach (var slot in Slots) // Assumes 'Slots' is the public accessor for _slots
        {
            if (slot != null && !slot.IsEmpty() && slot.item.data == itemData)
            {
                count += slot.quantity;
                if (count >= amount) return true;
            }
        }
        return false;
    }
}