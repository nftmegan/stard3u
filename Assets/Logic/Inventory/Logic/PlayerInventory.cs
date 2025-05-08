// Assets/Logic/Inventory/Logic/PlayerInventory.cs
using System;
using UnityEngine;
using System.Linq;

[DisallowMultipleComponent]
[RequireComponent(typeof(InventoryComponent))]
public class PlayerInventory : MonoBehaviour, IInventoryViewDataSource, IEquipmentHolder {
    [Header("Components")]
    [SerializeField] private InventoryComponent inventoryComp;
    [SerializeField] private ToolbarSelector toolbarSelector;

    public ToolbarSelector Toolbar => toolbarSelector;
    public ItemContainer Container => inventoryComp?.Container;

    public event Action<int> OnSlotChanged;
    public event Action<InventoryItem> OnEquippedItemChanged;
    // Optional Event for external systems to know when an item was pulled out
    public event Action<InventoryItem> OnItemPulledFromInventory;


    public ItemContainer GetContainerForInventory() => inventoryComp?.Container;
    public InventoryItem GetCurrentEquippedItem() => _equippedCache;
    public bool RequestAddItemToInventory(InventoryItem itemToAdd) { /* ... (no changes needed) ... */
        if (Container == null || itemToAdd == null || itemToAdd.data == null) {
            Debug.LogWarning("[PlayerInventory] Cannot add item - Container or item/data is null.");
            return false;
        }
        if (itemToAdd.data.isBulky) {
            return false;
        }
        Container.AddItem(itemToAdd, 1);
        bool check = Container.Slots.Any(slot => slot.item == itemToAdd);
        if (!check && itemToAdd.IsStackable) {
            check = Container.HasItem(itemToAdd.data);
        }
        if (!check) Debug.LogWarning($"[PlayerInventory] AddItem verification failed for '{itemToAdd.data.itemName}'.");
        return check;
    }
    public bool TryStoreItemInSpecificSlot(InventoryItem itemToAdd, int slotIndex) { /* ... (no changes needed) ... */
        if (Container == null || itemToAdd == null || itemToAdd.data == null || slotIndex < 0 || slotIndex >= Container.Size) {
            return false;
        }
        if (itemToAdd.data.isBulky && slotIndex < (toolbarSelector?.SlotCount ?? 0)) {
            return false;
        }
        InventorySlot targetSlot = Container[slotIndex];
        if (targetSlot == null) {
            Debug.LogError($"[PlayerInventory] Target slot {slotIndex} is null in Container!");
            return false;
        }
        bool changed = false;
        if (targetSlot.IsEmpty()) {
            targetSlot.item = itemToAdd;
            targetSlot.quantity = 1;
            changed = true;
        } else if (targetSlot.item.data == itemToAdd.data && targetSlot.item.IsStackable && !targetSlot.IsFull() && itemToAdd.runtime == null && targetSlot.item.runtime == null) {
            int added = targetSlot.AddQuantity(1);
            if (added > 0) {
                changed = true;
            }
        }
        if (changed) {
            this.OnSlotChanged?.Invoke(slotIndex);
            if (toolbarSelector != null && slotIndex < toolbarSelector.SlotCount) {
                HandleContainerSlotChangedForEquip(slotIndex);
            }
            return true;
        }
        return false;
    }
    public bool CanAddItemToInventory(InventoryItem itemToCheck, int quantity = 1) { /* ... (no changes needed) ... */
        if (Container == null || itemToCheck == null || itemToCheck.data == null) return false;
        if (itemToCheck.data.isBulky) return false;
        if (!itemToCheck.IsStackable || itemToCheck.runtime is IPartRuntimeState || itemToCheck.runtime != null) {
            return Container.Slots.Any(slot => slot.IsEmpty());
        } else {
            int spaceAvailable = 0;
            foreach (var slot in Container.Slots) {
                if (slot.IsEmpty()) {
                    spaceAvailable += Mathf.Max(1, itemToCheck.data.maxStack);
                } else if (slot.item.data == itemToCheck.data && slot.item.runtime == null) {
                    spaceAvailable += Mathf.Max(1, slot.item.data.maxStack) - slot.quantity;
                }
                if (spaceAvailable >= quantity) return true;
            }
            return false;
        }
    }
    public bool RequestConsumeItem(ItemData itemData, int amount = 1) { /* ... (no changes needed) ... */
        return Container?.TryConsumeItem(itemData, amount) ?? false;
     }
     public bool HasItemInInventory(ItemData itemData, int amount = 1) { /* ... (no changes needed) ... */
        return Container?.HasItem(itemData, amount) ?? false;
     }

    // --- NEW METHOD ---
    /// <summary>
    /// Attempts to remove one item from the specified inventory slot (typically a toolbar slot).
    /// Handles stack splitting correctly.
    /// </summary>
    /// <param name="slotIndex">The index of the slot to pull from.</param>
    /// <param name="pulledItem">Output: The InventoryItem instance that was removed.</param>
    /// <returns>True if an item was successfully pulled, false otherwise.</returns>
    public bool TryPullItemFromSlot(int slotIndex, out InventoryItem pulledItem) {
        pulledItem = null;
        if (Container == null || toolbarSelector == null) return false;
        if (slotIndex < 0 || slotIndex >= Container.Size) return false; // Invalid index

        InventorySlot sourceSlot = Container[slotIndex];
        if (sourceSlot == null || sourceSlot.IsEmpty()) {
            return false; // Nothing to pull
        }

        // --- Logic to extract one item ---
        if (sourceSlot.quantity > 1 && sourceSlot.item.IsStackable) {
            // Create a new InventoryItem instance for the single item being pulled
            // For simple stackables, just copy ItemData. If stackables could have
            // complex runtime state, you'd need a way to copy that state here.
            pulledItem = new InventoryItem(sourceSlot.item.data);
            // TODO: If stackable items can have runtime state, implement state copying logic here.
            sourceSlot.ReduceQuantity(1); // Decrease quantity in the original slot
            this.OnSlotChanged?.Invoke(slotIndex); // Notify UI quantity changed
            OnItemPulledFromInventory?.Invoke(pulledItem); // Notify external systems
            return true;
        } else if (sourceSlot.quantity == 1) {
            // Take the only item reference from the slot
            pulledItem = sourceSlot.item;
            sourceSlot.Clear(); // Clear the source slot entirely
            this.OnSlotChanged?.Invoke(slotIndex); // Notify UI slot changed
            OnItemPulledFromInventory?.Invoke(pulledItem); // Notify external systems
            return true;
        } else {
            // Should not happen if IsEmpty() check passed
            Debug.LogError($"[PlayerInventory] PullItem: Slot {slotIndex} quantity logic error. Qty: {sourceSlot.quantity}");
            return false;
        }
    }
    // --- END NEW METHOD ---

    public int SlotCount => Container?.Size ?? 0;
    public InventorySlot GetSlotByIndex(int index) => GetSlotAt(index);
    public void RequestMergeOrSwap(int fromIndex, int toIndex) { /* ... (no changes needed) ... */
        Container?.MergeOrSwap(fromIndex, toIndex);
    }

    private InventoryItem _equippedCache;

    private void Awake() { /* ... (no changes needed from previous fixed version) ... */
        inventoryComp = GetComponent<InventoryComponent>();
        if (inventoryComp == null) {
             Debug.LogError($"[PlayerInventory AWAKE on {gameObject.name}] CRITICAL: inventoryComp is NULL after GetComponent! PlayerInventory cannot function. CHECK INSPECTOR ASSIGNMENT.", this);
             this.enabled = false;
             return;
        }
        toolbarSelector ??= GetComponentInChildren<ToolbarSelector>(true);
        if (toolbarSelector == null) Debug.LogError($"[PlayerInventory on {gameObject.name}] ToolbarSelector missing!", this);
    }
    private void Start() { /* ... (no changes needed from previous fixed version) ... */
         if (Container == null) {
             Debug.LogError($"[PlayerInventory START on {gameObject.name}] Container IS NULL. Check InventoryComponent's Awake/Initialization.", this);
         }
        SubscribeToEvents();
        InitializeEquipmentState();
    }
    private void OnEnable() { /* ... (no changes needed from previous fixed version) ... */
        if (inventoryComp?.Container != null) {
            SubscribeToEvents();
            InitializeEquipmentState();
        }
    }
    private void OnDisable() { UnsubscribeFromEvents(); }
    private void SubscribeToEvents() { /* ... (no changes needed) ... */
        UnsubscribeFromEvents();
        if (toolbarSelector != null) toolbarSelector.OnIndexChanged += HandleToolbarIndexChanged;
        if (inventoryComp?.Container != null) inventoryComp.Container.OnSlotChanged += HandleInternalContainerSlotChanged;
    }
    private void UnsubscribeFromEvents() { /* ... (no changes needed) ... */
        if (toolbarSelector != null) toolbarSelector.OnIndexChanged -= HandleToolbarIndexChanged;
        if (inventoryComp?.Container != null) inventoryComp.Container.OnSlotChanged -= HandleInternalContainerSlotChanged;
    }
    private void HandleInternalContainerSlotChanged(int index) { /* ... (no changes needed) ... */
        this.OnSlotChanged?.Invoke(index);
        HandleContainerSlotChangedForEquip(index);
    }
    private void HandleToolbarIndexChanged(int newIndex) { RefreshEquippedItem(); }
    private void HandleContainerSlotChangedForEquip(int index) { /* ... (no changes needed) ... */
         if (index < 0) { RefreshEquippedItem(); return; }
        if (toolbarSelector != null && index < toolbarSelector.SlotCount && index == toolbarSelector.CurrentIndex) { RefreshEquippedItem(); }
    }
    public void HandleToolbarScroll(float scrollDirection) => toolbarSelector?.Step((int)Mathf.Sign(scrollDirection));
    public void HandleToolbarSlotSelection(int slotIndex) => toolbarSelector?.SetIndex(slotIndex);
    private void InitializeEquipmentState() { /* ... (no changes needed) ... */
        if (toolbarSelector != null && Container != null) { HandleToolbarIndexChanged(toolbarSelector.CurrentIndex); } else { RefreshEquippedItem(); }
    }
    private void RefreshEquippedItem() { /* ... (no changes needed) ... */
        InventoryItem newItemInSlot = null;
        if (toolbarSelector != null && Container != null) {
            int idx = toolbarSelector.CurrentIndex;
            if (idx >= 0 && idx < Container.Size) {
                InventorySlot slot = GetSlotAt(idx);
                newItemInSlot = slot?.item;
            } else if (Container.Size > 0 && idx >= Container.Size) {
                 Debug.LogWarning($"[PlayerInventory] Toolbar index {idx} is out of bounds for container size {Container.Size}. Resetting equipped cache.");
            }
        }
        if (newItemInSlot != _equippedCache) {
            _equippedCache = newItemInSlot;
            OnEquippedItemChanged?.Invoke(_equippedCache);
        }
    }
    public InventorySlot GetSlotAt(int i) { /* ... (no changes needed) ... */
         if (Container != null && i >= 0 && i < Container.Size) return Container[i]; return null;
    }
    public int GetSelectedToolbarIndex() => toolbarSelector?.CurrentIndex ?? -1;
}
