// --- Start of script: Assets/Logic/Inventory/Logic/PlayerInventory.cs ---
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
    public event Action<InventoryItem> OnItemPulledFromInventory;

    private InventoryItem _equippedCache; // Cache of the item in the currently selected toolbar slot

    public ItemContainer GetContainerForInventory() => inventoryComp?.Container;
    public InventoryItem GetCurrentEquippedItem() => _equippedCache; // Return the cached item

    public bool RequestAddItemToInventory(InventoryItem itemToAdd) {
        if (Container == null || itemToAdd == null || itemToAdd.data == null) {
            Debug.LogWarning("[PlayerInventory] Cannot add item - Container or item/data is null.");
            return false;
        }
        if (itemToAdd.data.isBulky) {
             Debug.LogWarning($"[PlayerInventory] Cannot add bulky item '{itemToAdd.data.itemName}' to main inventory.");
            return false; // Cannot add bulky items this way
        }
        Container.AddItem(itemToAdd, itemToAdd.IsStackable ? 1 : 1); // Add appropriate quantity (usually 1)
        // Verification after AddItem (AddItem handles logging failure)
        bool check = Container.HasItem(itemToAdd.data); // Simpler check
        // if (!check) Debug.LogWarning($"[PlayerInventory] AddItem verification failed for '{itemToAdd.data.itemName}'.");
        return check; // Return true if item is now present (might have stacked)
    }

    public bool TryStoreItemInSpecificSlot(InventoryItem itemToAdd, int slotIndex) {
        if (Container == null || itemToAdd == null || itemToAdd.data == null || slotIndex < 0 || slotIndex >= Container.Size) {
            return false;
        }
         // Check if trying to put bulky item in toolbar
        if (itemToAdd.data.isBulky && toolbarSelector != null && slotIndex < toolbarSelector.SlotCount) {
            Debug.LogWarning($"[PlayerInventory] Cannot store bulky item '{itemToAdd.data.itemName}' in toolbar slot {slotIndex}.");
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
            targetSlot.quantity = 1; // Assume adding one non-stackable or the first of a stack
            changed = true;
        } else if (targetSlot.item.data == itemToAdd.data && targetSlot.item.IsStackable && !targetSlot.IsFull() && itemToAdd.runtime == null && targetSlot.item.runtime == null) {
            int added = targetSlot.AddQuantity(1); // Add one unit
            if (added > 0) {
                changed = true;
                // Note: If adding stackable item, we assume itemToAdd itself isn't kept, just its data/type is used.
            }
        }
        // else: Cannot store (slot occupied by different item, or full stackable)

        if (changed) {
            // IMPORTANT: Notify listeners AFTER making the change
            this.OnSlotChanged?.Invoke(slotIndex);
            // Check if the change affected the equipped item cache IMMEDIATELY
            HandleContainerSlotChangedForEquip(slotIndex); // Ensure cache updates if toolbar affected
            return true;
        }
        return false;
    }

    public bool CanAddItemToInventory(InventoryItem itemToCheck, int quantity = 1) {
        if (Container == null || itemToCheck == null || itemToCheck.data == null) return false;
        if (itemToCheck.data.isBulky) return false; // Cannot add bulky to main inventory this way

        // Check if there's space, considering stacking
        int remainingToAdd = quantity;

        // Check existing stacks
        if (itemToCheck.IsStackable) {
            foreach (var slot in Container.Slots) {
                if (!slot.IsEmpty() && slot.item.data == itemToCheck.data && !slot.IsFull() && slot.item.runtime == null) // Only stack simple items
                {
                    int canAdd = Mathf.Max(1, slot.item.data.maxStack) - slot.quantity;
                    remainingToAdd -= canAdd;
                    if (remainingToAdd <= 0) return true; // Enough space found in existing stacks
                }
            }
        }

        // Check empty slots
        foreach (var slot in Container.Slots) {
            if (slot.IsEmpty()) {
                int canAdd = itemToCheck.IsStackable ? Mathf.Max(1, itemToCheck.data.maxStack) : 1;
                remainingToAdd -= canAdd;
                if (remainingToAdd <= 0) return true; // Enough space found in empty slots
            }
        }

        return false; // Not enough space
     }

     public bool RequestConsumeItem(ItemData itemData, int amount = 1) {
        return Container?.TryConsumeItem(itemData, amount) ?? false;
     }
     public bool HasItemInInventory(ItemData itemData, int amount = 1) {
        return Container?.HasItem(itemData, amount) ?? false;
     }

    // --- MODIFIED METHOD ---
    /// <summary>
    /// Attempts to remove one item from the specified inventory slot.
    /// Handles stack splitting and updates the equipped cache if the selected slot changes.
    /// </summary>
    public bool TryPullItemFromSlot(int slotIndex, out InventoryItem pulledItem) {
        pulledItem = null;
        if (Container == null) return false;
        if (slotIndex < 0 || slotIndex >= Container.Size) return false;

        InventorySlot sourceSlot = Container[slotIndex];
        if (sourceSlot == null || sourceSlot.IsEmpty()) {
            return false; // Nothing to pull
        }

        bool success = false;
        if (sourceSlot.quantity > 1 && sourceSlot.item.IsStackable) {
            // --- Stack Splitting ---
            // Create a NEW InventoryItem instance for the single item being pulled.
             pulledItem = new InventoryItem(sourceSlot.item.data); // Simple copy for basic stackables
             // TODO: If stackable items *can* have runtime state, implement state cloning here.
             sourceSlot.ReduceQuantity(1); // Decrease quantity in the original slot
             success = true;
        } else if (sourceSlot.quantity == 1) {
            // --- Taking the Last Item ---
            pulledItem = sourceSlot.item; // Take the reference
            sourceSlot.Clear(); // Clear the source slot entirely
            success = true;
        } else {
            Debug.LogError($"[PlayerInventory] PullItem: Slot {slotIndex} quantity logic error. Qty: {sourceSlot.quantity}");
            success = false;
        }

        if (success) {
            // Notify listeners about the change *after* modifying the slot
            OnSlotChanged?.Invoke(slotIndex);
            OnItemPulledFromInventory?.Invoke(pulledItem);

            // *** CRITICAL: Immediately update equipped cache if this was the selected slot ***
            if (toolbarSelector != null && slotIndex == toolbarSelector.CurrentIndex) {
                RefreshEquippedItem(); // Force cache refresh NOW
            }
            return true;
        }
        return false;
    }
    // --- END MODIFIED METHOD ---

    // --- IInventoryViewDataSource Implementation ---
    public int SlotCount => Container?.Size ?? 0;
    public InventorySlot GetSlotByIndex(int index) => GetSlotAt(index);
    public void RequestMergeOrSwap(int fromIndex, int toIndex) {
        Container?.MergeOrSwap(fromIndex, toIndex);
        // Update cache if merge/swap affects selected slot
        int selectedIdx = GetSelectedToolbarIndex();
        if(fromIndex == selectedIdx || toIndex == selectedIdx) {
            RefreshEquippedItem();
        }
    }
    // --- End IInventoryViewDataSource ---

    // --- Initialization and Event Handling ---
    private void Awake() {
        inventoryComp = GetComponent<InventoryComponent>();
        if (inventoryComp == null) {
             Debug.LogError($"[PlayerInventory AWAKE on {gameObject.name}] CRITICAL: InventoryComponent is NULL! PlayerInventory cannot function.", this);
             this.enabled = false;
             return;
        }
        // Container should be initialized by InventoryComponent's Awake
        toolbarSelector ??= GetComponentInChildren<ToolbarSelector>(true);
        if (toolbarSelector == null) Debug.LogError($"[PlayerInventory on {gameObject.name}] ToolbarSelector missing!", this);
    }
    private void Start() {
         if (Container == null) {
             Debug.LogError($"[PlayerInventory START on {gameObject.name}] Container IS NULL. Check InventoryComponent's Awake/Initialization.", this);
         } else {
             // Subscribe after container confirmed non-null
            SubscribeToEvents();
            InitializeEquipmentState(); // Initial equip based on starting selection
         }
    }
    private void OnEnable() {
        // Re-subscribe and initialize state if re-enabled after Start
        if (inventoryComp?.Container != null) {
            SubscribeToEvents();
            InitializeEquipmentState();
        }
    }
    private void OnDisable() { UnsubscribeFromEvents(); }

    private void SubscribeToEvents() {
        UnsubscribeFromEvents(); // Prevent double subscription
        if (toolbarSelector != null) toolbarSelector.OnIndexChanged += HandleToolbarIndexChanged;
        if (inventoryComp?.Container != null) inventoryComp.Container.OnSlotChanged += HandleInternalContainerSlotChanged;
        else Debug.LogWarning("[PlayerInventory] Cannot subscribe to Container events - Container is null.");
    }
    private void UnsubscribeFromEvents() {
        if (toolbarSelector != null) toolbarSelector.OnIndexChanged -= HandleToolbarIndexChanged;
        // Check container exists before unsubscribing
        if (inventoryComp?.Container != null) inventoryComp.Container.OnSlotChanged -= HandleInternalContainerSlotChanged;
    }

    private void HandleInternalContainerSlotChanged(int index) {
        // Forward the event for the UI
        this.OnSlotChanged?.Invoke(index);
        // Check if the change affects the equipped item
        HandleContainerSlotChangedForEquip(index);
    }

    private void HandleToolbarIndexChanged(int newIndex) { RefreshEquippedItem(); }

    private void HandleContainerSlotChangedForEquip(int index) {
        // If index is -1 (structural change) or the changed slot is the currently selected toolbar slot, refresh equipped item
        if (index < 0) {
             RefreshEquippedItem();
             return;
        }
        if (toolbarSelector != null && index < toolbarSelector.SlotCount && index == toolbarSelector.CurrentIndex) {
            RefreshEquippedItem();
        }
    }

    public void HandleToolbarScroll(float scrollDirection) => toolbarSelector?.Step((int)Mathf.Sign(scrollDirection));
    public void HandleToolbarSlotSelection(int slotIndex) => toolbarSelector?.SetIndex(slotIndex);

    private void InitializeEquipmentState() {
        // Trigger initial check based on toolbar's starting index
        if (toolbarSelector != null && Container != null) {
             HandleToolbarIndexChanged(toolbarSelector.CurrentIndex);
        } else {
            RefreshEquippedItem(); // Fallback if toolbar/container not ready
        }
    }

    // --- MODIFIED METHOD ---
    /// <summary>
    /// Updates the _equippedCache based on the item currently in the selected toolbar slot.
    /// Explicitly reads the slot content NOW.
    /// </summary>
    private void RefreshEquippedItem() {
        InventoryItem newItemInSlot = null;
        int selectedIdx = GetSelectedToolbarIndex();

        if (selectedIdx >= 0 && Container != null && selectedIdx < Container.Size) {
            InventorySlot slot = GetSlotAt(selectedIdx); // Get the current state of the slot
            if (slot != null && !slot.IsEmpty()) {
                newItemInSlot = slot.item; // Get the item reference directly from the slot
            }
            // If slot is null or empty, newItemInSlot remains null
        }
        // else: No valid selection or container, newItemInSlot remains null

        // Update cache and fire event ONLY if the item reference has actually changed
        if (newItemInSlot != _equippedCache) {
            _equippedCache = newItemInSlot; // Update the cache
            // Debug.Log($"[PlayerInventory] Equipped Cache Updated. Now: {(_equippedCache?.data?.itemName ?? "NULL")}");
            OnEquippedItemChanged?.Invoke(_equippedCache); // Notify EquipmentController
        }
    }
    // --- END MODIFIED METHOD ---

    public InventorySlot GetSlotAt(int i) {
         if (Container != null && i >= 0 && i < Container.Size) return Container[i]; return null;
    }
    public int GetSelectedToolbarIndex() => toolbarSelector?.CurrentIndex ?? -1;

}