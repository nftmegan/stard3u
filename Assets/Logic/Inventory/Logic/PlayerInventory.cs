using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(InventoryComponent))]
public class PlayerInventory : MonoBehaviour, IInventoryViewDataSource, IEquipmentHolder
{
    [SerializeField] private InventoryComponent inventoryComp;
    [SerializeField] private ToolbarSelector toolbarSelector;

    public ToolbarSelector Toolbar => toolbarSelector;

    // --- IInventoryViewDataSource ---
    public int SlotCount => inventoryComp?.Container?.Size ?? 0;
    public InventorySlot GetSlotByIndex(int index) => GetSlotAt(index);

    // --- CORRECTED EVENT FORWARDING ---
    // This is the public event that external classes (like InventoryUIManager) subscribe to.
    public event Action<int> OnSlotChanged;

    // This private method handles the event from the actual ItemContainer.
    private void HandleInternalContainerSlotChanged(int index)
    {
        // Forward the event to external subscribers.
        OnSlotChanged?.Invoke(index);

        // Also handle the equipment refresh logic if needed for the selected slot.
        HandleContainerSlotChangedForEquip(index);
    }
    // --- END CORRECTION ---

    public void RequestMergeOrSwap(int fromIndex, int toIndex) => MergeOrSwap(fromIndex, toIndex);

    // --- IEquipmentHolder ---
    public event Action<InventoryItem> OnEquippedItemChanged;
    public ItemContainer GetContainerForInventory() => inventoryComp?.Container;
    public InventoryItem GetCurrentEquippedItem() => _equippedCache;

    // --- Private State ---
    private InventoryItem _equippedCache;

    private void Awake()
    {
        inventoryComp ??= GetComponent<InventoryComponent>();
        toolbarSelector ??= GetComponentInChildren<ToolbarSelector>(true);

        if (toolbarSelector == null) Debug.LogError("[PlayerInventory] ToolbarSelector missing!", this);
        if (inventoryComp == null) Debug.LogError("[PlayerInventory] InventoryComponent missing!", this);
    }

    // Start is reliable for accessing other components' Awake results
    private void Start()
    {
        if (inventoryComp?.Container == null)
        {
            Debug.LogError("[PlayerInventory] InventoryComponent's Container is null in Start! Check InventoryComponent's Awake method.", inventoryComp);
            // Potentially disable this component if container is vital and missing
            // this.enabled = false;
            // return;
        }

        // Subscribe internal handler to the actual container's event
        SubscribeToEvents();

        // Initialize equipment state
        InitializeEquipmentState();
    }


    private void OnEnable()
    {
        // Subscribe here as well in case the component is re-enabled
        // Start() will likely run first on initial load, but OnEnable handles re-activation
        SubscribeToEvents();
        // Re-sync equipment state if re-enabled
        if (inventoryComp?.Container != null) InitializeEquipmentState();
    }

    private void OnDisable()
    {
        UnsubscribeFromEvents();
    }

    private void SubscribeToEvents()
    {
        UnsubscribeFromEvents(); // Prevent double subscriptions

        if (toolbarSelector != null)
        {
            toolbarSelector.OnIndexChanged += HandleToolbarIndexChanged;
        }
        // Subscribe our internal handler to the ACTUAL container event
        if (inventoryComp?.Container != null)
        {
            inventoryComp.Container.OnSlotChanged += HandleInternalContainerSlotChanged;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (toolbarSelector != null)
        {
            toolbarSelector.OnIndexChanged -= HandleToolbarIndexChanged;
        }
        // Unsubscribe our internal handler
        if (inventoryComp?.Container != null)
        {
            inventoryComp.Container.OnSlotChanged -= HandleInternalContainerSlotChanged;
        }
    }


    // Initializes the currently equipped item based on toolbar state
    private void InitializeEquipmentState()
    {
        if (toolbarSelector != null)
        {
            HandleToolbarIndexChanged(toolbarSelector.CurrentIndex);
        }
        else
        {
            RefreshEquippedItem(); // Refresh with default state if no toolbar
        }
    }

    // --- Event Handlers ---
    // Called by ToolbarSelector when selection changes
    private void HandleToolbarIndexChanged(int newIndex)
    {
        RefreshEquippedItem();
    }

    // Renamed internal handler for clarity
    // Called internally when the ItemContainer reports a change
    private void HandleContainerSlotChangedForEquip(int index)
    {
        if (index < 0) return; // Ignore structural changes (-1) for equip refresh

        // Check if the change affects the currently selected toolbar slot
        if (toolbarSelector != null && index < toolbarSelector.SlotCount && index == toolbarSelector.CurrentIndex)
        {
            RefreshEquippedItem(); // Refresh equipment visuals
        }
    }

    // --- Input Handling Methods (Called by PlayerManager) ---
    public void HandleToolbarScroll(float scrollDirection) => toolbarSelector?.Step((int)scrollDirection);
    public void HandleToolbarSlotSelection(int slotIndex) => toolbarSelector?.SetIndex(slotIndex);

    // --- Core Logic ---
    private void RefreshEquippedItem()
    {
        if (toolbarSelector == null) { /* Equip nothing if no toolbar */ HandleEquipRefresh(null); return; }
        if (inventoryComp?.Container == null) { /* Equip nothing if no container */ HandleEquipRefresh(null); return; }

        int currentIndex = toolbarSelector.CurrentIndex;
        InventorySlot slot = GetSlotAt(currentIndex);
        InventoryItem item = slot?.item;

        HandleEquipRefresh(item); // Pass item (or null) to handler
    }

    // Centralized place to update cache and fire event
    private void HandleEquipRefresh(InventoryItem newItem)
    {
         if (newItem != _equippedCache)
        {
            _equippedCache = newItem;
            OnEquippedItemChanged?.Invoke(_equippedCache);
        }
    }

    // --- Public Inventory Actions ---
    public InventorySlot GetSlotAt(int i)
    {
        var slots = inventoryComp?.Container?.Slots;
        if (slots != null && i >= 0 && i < slots.Length) return slots[i];
        return null;
    }

    public void AddItem(InventoryItem item, int qty = 1) => inventoryComp?.Container?.AddItem(item, qty);
    public bool TryConsume(ItemData d, int a = 1) => inventoryComp?.Container?.TryConsumeItem(d, a) ?? false;
    public bool Withdraw(ItemData d, int a, InventorySlot t) => inventoryComp?.Container?.Withdraw(d, a, t) ?? false;
    public void MergeOrSwap(int a, int b) => inventoryComp?.Container?.MergeOrSwap(a, b);
    public void Resize(int newSize) => inventoryComp?.Container?.Resize(newSize);

    // --- Helpers ---
    public int GetEquippedToolbarIndex() => toolbarSelector?.CurrentIndex ?? -1;
}