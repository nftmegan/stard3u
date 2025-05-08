// In Assets/Scripts/Inventory/Core/ItemContainer.cs (or your path)
using System;
using UnityEngine;

/// <summary>
/// A reusable stack-based container (backpack, toolbar, magazine, etc.).
/// Manages an array of InventorySlots.
/// </summary>
[Serializable]
public class ItemContainer {
    public event Action<int> OnSlotChanged;  // -1 => structural change

    [SerializeField] private InventorySlot[] _slots;
    // Public accessor to view slots, but prevents replacing the array externally
    public InventorySlot[] Slots => _slots;

    #region Constructor
    /// <summary>
    /// Creates a new ItemContainer with a specified size, initializing all slots as empty.
    /// </summary>
    public ItemContainer(int size) {
        size = Mathf.Max(1, size); // Ensure size is at least 1
        _slots = new InventorySlot[size];
        for (int i = 0; i < size; i++) {
            // Ensure each slot is properly initialized
            _slots[i] = new InventorySlot(null, 0);
        }
    }
    #endregion

    #region Structure & Size
    /// <summary>
    /// Gets the current number of slots in the container.
    /// </summary>
    public int Size => _slots?.Length ?? 0; // Handles potential null _slots array

    /// <summary>
    /// Resizes the container's internal slot array to the new specified size.
    /// Preserves existing items up to the smaller of the old and new sizes.
    /// Initializes any newly created slots as empty.
    /// WARNING: Shrinking the size will discard items in the removed slots.
    /// </summary>
    /// <param name="newSize">The desired number of slots.</param>
    public void Resize(int newSize) {
        newSize = Mathf.Max(1, newSize); // Ensure new size is valid
        int oldSize = this.Size;

        if (newSize == oldSize) return; // No change needed

        // Create backup if needed (optional, Array.Resize handles copying)
        // InventorySlot[] oldSlotsBackup = new InventorySlot[oldSize];
        // Array.Copy(_slots, oldSlotsBackup, oldSize);

        // Use Array.Resize - automatically preserves elements up to Min(oldSize, newSize)
        Array.Resize(ref _slots, newSize);

        // If expanding, initialize the newly added slots
        if (newSize > oldSize) {
            for (int i = oldSize; i < newSize; i++) {
                // Check if the slot is null (Array.Resize might leave them null) and initialize
                if (_slots[i] == null) {
                    _slots[i] = new InventorySlot(null, 0);
                }
            }
        }
        // If shrinking, elements beyond newSize are automatically removed by Array.Resize.

        OnSlotChanged?.Invoke(-1); // Notify listeners of the structural change
    }

    /// <summary>
    /// Accessor for getting a specific slot by index.
    /// </summary>
    public InventorySlot this[int i] {
        get {
            if (_slots != null && i >= 0 && i < _slots.Length) {
                return _slots[i];
            }
            Debug.LogError($"[ItemContainer] Index {i} out of range (Size: {Size}). Returning null.");
            return null; // Return null or throw exception for invalid index
        }
        // Optionally add a 'set' accessor if you want to allow replacing entire slots, use with caution.
        // set { ... }
    }
    #endregion

    #region Item Management Helpers
    /// <summary>
    /// Adds an incoming InventoryItem (with quantity) to the container.
    /// Handles stacking for stackable items and places non-stackables in empty slots.
    /// Correctly handles adding non-stackable items with existing runtime state by reference.
    /// </summary>
    public void AddItem(InventoryItem incoming, int quantity = 1) {
        if (incoming == null || incoming.data == null || quantity <= 0) {
             Debug.LogWarning($"[ItemContainer] AddItem called with invalid item or quantity.");
             return;
        }

        // --- Non-Stackable Item Handling (Parts, Weapons, Tools with state) ---
        if (!incoming.IsStackable) {
            if (quantity > 1) Debug.LogWarning($"[ItemContainer] Non-stackable '{incoming.data.itemName}' added with quantity > 1. Storing only 1.");
            quantity = 1;

            // Find the first empty slot and place the *exact* InventoryItem reference
            for (int i = 0; i < Slots.Length; i++) {
                if (Slots[i].IsEmpty()) {
                    Slots[i].item = incoming; // Store the reference including runtime state
                    Slots[i].quantity = 1;
                    OnSlotChanged?.Invoke(i);
                    // Debug.Log($"[ItemContainer] Added non-stackable '{incoming.data.itemName}' to slot {i}.");
                    return; // Item placed
                }
            }
            // If no empty slot found
            Debug.LogWarning($"[ItemContainer] Inventory full – couldn’t add non-stackable item '{incoming.data.itemName}'.");
            return;
        }
        // --- End Non-Stackable Handling ---

        // --- Stackable Item Handling ---
        int maxPerStack = Mathf.Max(1, incoming.data.maxStack);
        int remaining = quantity;

        // 1. Try to top-up existing stacks of the same item type
        for (int i = 0; i < Slots.Length && remaining > 0; i++) {
            InventorySlot s = Slots[i];
            // Can stack if item data matches, slot isn't full, and runtime states are compatible
            // (Basic check: only stack if incoming item has NO runtime state, assumes existing stack also doesn't or it's okay)
            bool canStackOnto = !s.IsEmpty() && s.item.data == incoming.data && !s.IsFull() && (incoming.runtime == null); // TODO: More robust runtime state check if needed

            if (canStackOnto) {
                remaining -= s.AddQuantity(remaining); // AddQuantity handles clamping to maxStack
                if(remaining < quantity) OnSlotChanged?.Invoke(i); // Fire event only if quantity changed
            }
        }

        // 2. Place remaining into empty slots
        for (int i = 0; i < Slots.Length && remaining > 0; i++) {
            if (Slots[i].IsEmpty()) {
                int amountToAdd = Mathf.Min(maxPerStack, remaining);
                // For simple stackables (no runtime state), create a new InventoryItem instance for the slot
                // If it *did* have runtime state but was stackable (rare), you'd pass 'incoming' directly.
                InventoryItem itemForNewSlot = (incoming.runtime == null) ? new InventoryItem(incoming.data) : incoming;

                Slots[i] = new InventorySlot(itemForNewSlot, amountToAdd); // Create new slot entry
                remaining -= amountToAdd;
                OnSlotChanged?.Invoke(i);
            }
        }

        // Log if any items couldn't be added
        if (remaining > 0) {
            Debug.LogWarning($"[ItemContainer] Inventory full – couldn’t add {remaining} × {incoming.data.itemName}.");
        }
    }

    /// <summary>
    /// Attempts to consume (remove) a specified amount of an item type from the container.
    /// Will remove from multiple stacks if necessary.
    /// </summary>
    /// <returns>True if the full amount was consumed, false otherwise.</returns>
    public bool TryConsumeItem(ItemData data, int amount = 1) {
        if (data == null || amount <= 0) return false;

        // Check total available first (optimization)
        if (!HasItem(data, amount)) {
             // Debug.LogWarning($"[ItemContainer] Cannot consume {amount}x {data.itemName} - Not enough available.");
             return false;
        }

        // Burn across stacks
        int remainingToConsume = amount;
        for (int i = 0; i < Slots.Length && remainingToConsume > 0; i++) {
            InventorySlot s = Slots[i];
            if (!s.IsEmpty() && s.item.data == data) {
                int take = Mathf.Min(s.quantity, remainingToConsume);
                s.ReduceQuantity(take); // ReduceQuantity handles clearing the slot if it becomes empty
                remainingToConsume -= take;
                OnSlotChanged?.Invoke(i); // Notify slot changed
            }
        }
        // Should always return true if HasItem check passed, unless logic error
        return remainingToConsume == 0;
    }

    /// <summary>
    /// Attempts to withdraw a quantity of items matching ItemData from this container
    /// and add it to a target InventorySlot. Used primarily for splitting stacks.
    /// </summary>
    /// <returns>True if the full amount was successfully withdrawn, false otherwise.</returns>
    public bool Withdraw(ItemData data, int amount, InventorySlot targetSlot) {
        if (data == null || amount <= 0 || targetSlot == null) return false;
        if (!HasItem(data, amount)) return false; // Check if enough exists first

        int remainingToWithdraw = amount;
        for (int i = 0; i < Slots.Length && remainingToWithdraw > 0; i++) {
            InventorySlot src = Slots[i];
            if (!src.IsEmpty() && src.item.data == data) {
                int take = Mathf.Min(src.quantity, remainingToWithdraw);

                // Add to target slot (handles creating item if target is empty)
                if (targetSlot.item == null) targetSlot.item = new InventoryItem(data); // Assume simple stackable for withdrawal target
                int actuallyAdded = targetSlot.AddQuantity(take); // Use target slot's logic

                // Only reduce source if successfully added to target
                if (actuallyAdded > 0) {
                     src.ReduceQuantity(actuallyAdded); // Reduce source by amount added
                     remainingToWithdraw -= actuallyAdded;
                     OnSlotChanged?.Invoke(i); // Notify source slot changed
                }

                 // If target slot became full, stop trying to add more to it in this iteration
                 if (targetSlot.IsFull()) break; // Exit withdraw loop early if target is full

                 // Safety break if AddQuantity somehow didn't add anything when it should have
                 if (actuallyAdded == 0 && take > 0) {
                     Debug.LogWarning($"[ItemContainer Withdraw] targetSlot.AddQuantity added 0 despite available space/quantity. Aborting withdraw.");
                     break; // Avoid infinite loop
                 }
            }
        }
        // Return true only if the full requested amount was withdrawn
        return remainingToWithdraw == 0;
    }

    /// <summary>
    /// Merges stackable items or swaps items between two slot indices.
    /// </summary>
    public void MergeOrSwap(int indexA, int indexB) {
        if (indexA < 0 || indexB < 0 || indexA >= Size || indexB >= Size || indexA == indexB) return;

        InventorySlot slotA = Slots[indexA];
        InventorySlot slotB = Slots[indexB];

        // --- Attempt Merge (Only if both are same STACKABLE item type) ---
        if (!slotA.IsEmpty() && !slotB.IsEmpty() &&
            slotA.item.data == slotB.item.data &&
            slotA.item.data.stackable) // Only merge stackables
        {
            int maxStack = Mathf.Max(1, slotA.item.data.maxStack);
            int spaceInB = maxStack - slotB.quantity;

            if (spaceInB > 0) { // If slot B has space
                int amountToMove = Mathf.Min(slotA.quantity, spaceInB); // Move as much as possible from A
                if (amountToMove > 0) {
                    slotB.AddQuantity(amountToMove); // Add to B
                    slotA.ReduceQuantity(amountToMove); // Remove from A
                    OnSlotChanged?.Invoke(indexA);
                    OnSlotChanged?.Invoke(indexB);
                    return; // Merge successful
                }
            }
            // If B was full, try merging into A instead (optional optimization)
            int spaceInA = maxStack - slotA.quantity;
             if (spaceInA > 0) {
                 int amountToMove = Mathf.Min(slotB.quantity, spaceInA);
                 if (amountToMove > 0) {
                     slotA.AddQuantity(amountToMove);
                     slotB.ReduceQuantity(amountToMove);
                     OnSlotChanged?.Invoke(indexA);
                     OnSlotChanged?.Invoke(indexB);
                     return;
                 }
             }
        }

        // --- If Merge wasn't possible or didn't happen, SWAP the slots ---
        (_slots[indexA], _slots[indexB]) = (_slots[indexB], _slots[indexA]); // C# Tuple Swap
        OnSlotChanged?.Invoke(indexA);
        OnSlotChanged?.Invoke(indexB);
    }

    /// <summary>
    /// Checks if the container holds at least a certain amount of a specific item type.
    /// </summary>
    /// <returns>True if the required amount is found, false otherwise.</returns>
    public bool HasItem(ItemData itemData, int amount = 1) {
        if (itemData == null || amount <= 0) return false;
        int count = 0;
        if (_slots == null) return false; // Safety check

        foreach (var slot in Slots) {
            if (slot != null && !slot.IsEmpty() && slot.item.data == itemData) {
                count += slot.quantity;
                if (count >= amount) return true; // Exit early if enough found
            }
        }
        return false; // Went through all slots, not enough found
    }
    #endregion

} // End of ItemContainer class