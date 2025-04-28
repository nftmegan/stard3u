using System;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Inventory       inventory;
    [SerializeField] private ToolbarSelector toolbarSelector;
    public InventoryUIManager inventoryUIManager;

    /// <summary>
    /// Fired whenever the equipped slot’s item changes (due to slot swap,
    /// pickup into selected slot, drop out of selected slot, etc.).
    /// </summary>
    public event Action<InventoryItem> OnSelectedItemChanged;

    private int           selectedSlot;
    private InventoryItem equippedCache;

    private void Awake()
    {
        // auto-find if not wired
        inventory       ??= GetComponent<Inventory>();
        toolbarSelector ??= GetComponent<ToolbarSelector>();

        // listen for any inventory change
        inventory.OnInventoryChanged += OnInventoryChanged;
    }

    private void OnDestroy()
    {
        inventory.OnInventoryChanged -= OnInventoryChanged;
    }

    /// <summary>
    /// Call once at startup to wire UI & default slot.
    /// </summary>
    public void Initialize()
    {
        toolbarSelector.Initialize(inventory);
        SelectSlot(0);
        // initial UI draw
        inventoryUIManager?.RefreshUI();
    }

    /// <summary>
    /// Handle keyboard/scroll to switch toolbar slots.
    /// </summary>
    public void HandleInput(IPlayerInput input)
    {
        if (input == null) return;

        // 1–9 keys
        for (int i = 0; i < toolbarSelector.GetToolbarSlotCount(); i++)
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                { SelectSlot(i); return; }

        // scroll
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f)
            SelectSlot((selectedSlot + toolbarSelector.GetToolbarSlotCount() - 1) % toolbarSelector.GetToolbarSlotCount());
        else if (scroll < 0f)
            SelectSlot((selectedSlot + 1) % toolbarSelector.GetToolbarSlotCount());
    }

    private void SelectSlot(int idx)
    {
        if (idx == selectedSlot) return;
        selectedSlot = idx;
        NotifySelectionChanged();
    }

    private void NotifySelectionChanged()
    {
        var slot = inventory.GetSlotAt(selectedSlot);
        equippedCache = slot?.item;
        OnSelectedItemChanged?.Invoke(equippedCache);
        toolbarSelector.UpdateSelection(selectedSlot);
        inventoryUIManager?.RefreshUI();
    }

    /// <summary>
    /// Called whenever *any* slot in the inventory changes.
    /// We redraw the UI and re-check the selected slot’s item.
    /// </summary>
    private void OnInventoryChanged()
    {
        inventoryUIManager?.RefreshUI();
        CheckSelectedItemChanged();
    }

    /// <summary>
    /// If the selected‐slot’s item is now different from what we last had,
    /// fire the equip event so EquipmentController switches models.
    /// </summary>
    private void CheckSelectedItemChanged()
    {
        var slot    = inventory.GetSlotAt(selectedSlot);
        var current = slot?.item;
        if (current != equippedCache)
        {
            equippedCache = current;
            OnSelectedItemChanged?.Invoke(equippedCache);
        }
    }

    public void RefreshSelection()
    {
        var slot = inventory.GetSlotAt(selectedSlot);
        var item = slot?.item;

        if (item != equippedCache)
        {
            equippedCache = item;
            OnSelectedItemChanged?.Invoke(equippedCache);
        }
    }

    // ─── Public API ──────────────────────────────────────────────────────────

    /// <summary>Adds into your inventory (stacks if possible).</summary>
    public void AddItem(ItemData data, int amount = 1)
    {
        inventory.AddItem(data, amount);
    }

    /// <summary>Tries to consume that many from your inventory.</summary>
    public bool TryConsumeItem(ItemData data, int amount = 1)
        => inventory.TryConsumeItem(data, amount);

    /// <summary>Withdraws up to amount into the given slot.</summary>
    public bool Withdraw(ItemData data, int amount, InventorySlot targetSlot)
        => inventory.Withdraw(data, amount, targetSlot);

    /// <summary>Swap two slots (toolbar or bag).</summary>
    public void SwapItems(int a, int b)
        => inventory.SwapItems(a, b);

    /// <summary>Changes your total slot count (toolbar+bag).</summary>
    public void ResizeInventory(int newSize)
        => inventory.Resize(newSize);

    /// <summary>Finds the first slot containing at least one of that item.</summary>
    public InventorySlot GetSlotWithItem(ItemData data)
    {
        foreach (var s in inventory.Slots)
            if (s != null && s.item != null && s.item.data == data && s.quantity > 0)
                return s;
        return null;
    }

    /// <summary>Subscribe to selection changes (for equipment).</summary>
    public void SubscribeToSelectionChanges(Action<InventoryItem> cb)
        => OnSelectedItemChanged += cb;

    /// <summary>Unsubscribe.</summary>
    public void UnsubscribeFromSelectionChanges(Action<InventoryItem> cb)
        => OnSelectedItemChanged -= cb;

    public InventorySlot   GetSlotAt(int i)     => inventory.GetSlotAt(i);
    public int             GetSelectedIndex()   => selectedSlot;
    public InventoryItem   GetSelectedItem()    => inventory.GetSlotAt(selectedSlot)?.item;
    public Inventory       GetInventory()       => inventory;
    public ToolbarSelector GetToolbarSelector() => toolbarSelector;
}