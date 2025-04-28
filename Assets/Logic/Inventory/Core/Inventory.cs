using UnityEngine;

public class Inventory : MonoBehaviour
{
    public event System.Action OnInventoryChanged;
    public InventorySlot[] Slots { get; private set; }

    void Awake() => Slots = new InventorySlot[9];

    public void Resize(int newSize)
    {
        Slots = new InventorySlot[newSize];
        OnInventoryChanged?.Invoke();
    }

    /* ---------------- Add ---------------- */
    public void AddItem(ItemData newItem, int amount = 1)
    {
        if (newItem == null || amount <= 0) return;

        /* try stack */
        for (int i = 0; i < Slots.Length; i++)
        {
            var s = Slots[i];
            if (s != null && s.item != null && s.item.data == newItem)
            {
                s.AddQuantity(amount);
                OnInventoryChanged?.Invoke();
                return;
            }
        }

        /* first truly empty slot */
        for (int i = 0; i < Slots.Length; i++)
        {
            if (Slots[i] == null || Slots[i].item == null)
            {
                Slots[i] = new InventorySlot(new InventoryItem { data = newItem }, amount);
                OnInventoryChanged?.Invoke();
                return;
            }
        }

        Debug.LogWarning("[Inventory] Inventory full.");
    }

    /* ------------- Withdraw ------------- */
    public bool Withdraw(ItemData itemData, int amount, InventorySlot targetSlot)
    {
        if (itemData == null || amount <= 0 || targetSlot == null)
            return false;

        int remaining = amount;
        bool anyTaken = false;

        for (int i = 0; i < Slots.Length && remaining > 0; i++)
        {
            var src = Slots[i];
            if (src == null || src.item == null) continue;
            if (src.item.data != itemData || src.quantity == 0) continue;

            int take = Mathf.Min(src.quantity, remaining);

            /* ── ensure magazine holds a proper InventoryItem ─────────────── */
            if (targetSlot.item == null)
                targetSlot.item = src.quantity == take && remaining == take
                                ? src.item                 // move reference
                                : new InventoryItem();     // create new

            targetSlot.item.data = itemData;  // make sure data is correct
            targetSlot.AddQuantity(take);

            src.ReduceQuantity(take);
            remaining  -= take;
            anyTaken    = true;

            /* if we moved the whole stack, clear the backpack slot completely */
            if (src.IsEmpty())
                src.Clear();
        }

        if (anyTaken)
            OnInventoryChanged?.Invoke();

        return anyTaken;
    }

    /* ------------- Consume -------------- */
    public bool TryConsumeItem(ItemData data, int amount = 1)
    {
        if (data == null || amount <= 0) return false;

        for (int i = 0; i < Slots.Length; i++)
        {
            var s = Slots[i];
            if (s == null || s.item == null) continue;
            if (s.item.data != data || s.quantity < amount) continue;

            s.ReduceQuantity(amount);
            OnInventoryChanged?.Invoke();
            return true;
        }
        return false;
    }

    /* ------------ misc helpers ---------- */
    public void SwapItems(int a, int b)
    {
        if (a < 0 || b < 0 || a >= Slots.Length || b >= Slots.Length || a == b) return;
        (Slots[a], Slots[b]) = (Slots[b], Slots[a]);
        OnInventoryChanged?.Invoke();
    }

    public InventorySlot GetSlotAt(int i) => (i >= 0 && i < Slots.Length) ? Slots[i] : null;
}
