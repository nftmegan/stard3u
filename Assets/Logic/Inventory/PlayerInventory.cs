// Assets/Logic/Inventory/PlayerInventory.cs
using System;
using UnityEngine;
using Game.InventoryLogic;
using UI.Inventory;

[DisallowMultipleComponent]
public class PlayerInventory : MonoBehaviour
{
    [SerializeField] private InventoryComponent inventoryComp;
    [SerializeField] private ToolbarSelector    toolbarSelector;

    public ItemContainer Container => inventoryComp.Container;
    public ToolbarSelector Toolbar  => toolbarSelector;

    public event Action<InventoryItem> OnEquippedItemChanged;
    private InventoryItem equippedCache;

    private void Awake()
    {
        // auto‐find if missing
        if (inventoryComp   == null) inventoryComp   = GetComponent<InventoryComponent>();
        if (toolbarSelector == null) toolbarSelector = GetComponent<ToolbarSelector>();
    }

    private void Start()
    {
        toolbarSelector.OnIndexChanged += _ => RefreshEquipped();
        Container.OnSlotChanged        += _ => RefreshEquipped();
    }

    private void OnDestroy()
    {
        toolbarSelector.OnIndexChanged -= _ => RefreshEquipped();
        Container.OnSlotChanged        -= _ => RefreshEquipped();
    }

    public void Initialize() => RefreshEquipped();

    public void HandleInput(IPlayerInput input)
    {
        if (input == null) return;
        for (int i = 0; i < toolbarSelector.SlotCount; i++)
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                toolbarSelector.SetIndex(i);
                return;
            }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if      (scroll > 0f) toolbarSelector.Step(-1);
        else if (scroll < 0f) toolbarSelector.Step(+1);
    }

    private void RefreshEquipped()
    {
        var slot = GetSlotAt(toolbarSelector.CurrentIndex);
        var item = slot?.item;
        if (item != equippedCache)
        {
            equippedCache = item;
            OnEquippedItemChanged?.Invoke(item);
        }
    }

    public InventorySlot GetSlotAt(int i) =>
        (inventoryComp != null 
         && inventoryComp.Container != null 
         && i >= 0 
         && i < inventoryComp.Container.Size)
           ? inventoryComp.Container[i]
           : null;

    public int           GetEquippedIndex() => toolbarSelector.CurrentIndex;
    public InventoryItem GetEquippedItem()  => equippedCache;

    public void AddItem(InventoryItem item, int qty = 1)       => Container.AddItem(item, qty);
    public bool TryConsume(ItemData d, int a = 1)              => Container.TryConsumeItem(d, a);
    public bool Withdraw(ItemData d, int a, InventorySlot t)   => Container.Withdraw(d, a, t);
    public void MergeOrSwap(int a, int b)                      => Container.MergeOrSwap(a, b);
    public void Resize(int newSize)                            => Container.Resize(newSize);
}
