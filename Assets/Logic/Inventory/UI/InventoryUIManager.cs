using UnityEngine;

public class InventoryUIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private InventoryItemUI itemPrefab;
    [SerializeField] private Transform toolbarParent;
    [SerializeField] private Transform mainInventoryParent;
    [SerializeField] private Color selectedColor = Color.yellow;
    [SerializeField] private Color normalColor = Color.white;

    private PlayerInventory playerInventory;
    private InventorySlotUI[] toolbarSlots;
    private InventorySlotUI[] bagSlots;

    public void Initialize(PlayerInventory playerInventory)
    {
        this.playerInventory = playerInventory;

        if (itemPrefab == null || toolbarParent == null || mainInventoryParent == null)
        {
            Debug.LogError("[InventoryUIManager] Missing references!");
            return;
        }

        toolbarSlots = toolbarParent.GetComponentsInChildren<InventorySlotUI>(true);
        bagSlots = mainInventoryParent.GetComponentsInChildren<InventorySlotUI>(true);

        int totalSlots = toolbarSlots.Length + bagSlots.Length;
        playerInventory.ResizeInventory(totalSlots);

        // Assign correct slot indices
        for (int i = 0; i < toolbarSlots.Length; i++)
            toolbarSlots[i].Setup(i);

        for (int i = 0; i < bagSlots.Length; i++)
            bagSlots[i].Setup(i + toolbarSlots.Length);

        playerInventory.GetInventory().OnInventoryChanged += RefreshUI;
        playerInventory.GetToolbarSelector().OnSelectedIndexChanged += UpdateToolbarHighlight;

        RefreshUI();
        UpdateToolbarHighlight(playerInventory.GetSelectedIndex());
    }

    public void RefreshUI()
    {
        if (playerInventory == null) return;

        var inventory = playerInventory.GetInventory();

        for (int i = 0; i < toolbarSlots.Length; i++)
            RefreshSlot(toolbarSlots[i], inventory.GetItemAt(i));

        for (int i = 0; i < bagSlots.Length; i++)
            RefreshSlot(bagSlots[i], inventory.GetItemAt(i + toolbarSlots.Length));
    }

    private void RefreshSlot(InventorySlotUI slot, InventoryItem item)
    {
        foreach (Transform child in slot.transform)
            Destroy(child.gameObject);

        if (item != null)
        {
            var ui = Instantiate(itemPrefab, slot.transform);
            ui.Initialize(item.data.icon, item.quantity, slot.GetSlotIndex());
        }
    }

    private void UpdateToolbarHighlight(int selectedIndex)
    {
        for (int i = 0; i < toolbarSlots.Length; i++)
        {
            toolbarSlots[i].SetSelected(i == selectedIndex, selectedColor, normalColor);
        }
    }

    public void ForceRefreshUI()
    {
        Debug.Log("[InventoryUIManager] Force refreshing UI...");
        RefreshUI();
    }
}
