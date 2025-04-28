using UnityEngine;

public class InventoryUIManager : MonoBehaviour
{
    public PlayerInventory   playerInv;

    [Header("UI References")]
    [SerializeField] private InventoryItemUI itemPrefab;
    [SerializeField] private Transform       toolbarParent;
    [SerializeField] private Transform       bagParent;
    [SerializeField] private Color           selectedColor = Color.yellow;
    [SerializeField] private Color           normalColor   = Color.white;

    public InventorySlotUI[] toolbarSlots;
    public InventorySlotUI[] bagSlots;

    /// <summary>
    /// Call once after wiring references (e.g. from PlayerManager.Start).
    /// </summary>
    public void Initialize(PlayerInventory pi)
    {
        print("INITIALIZED MOTHER ASDASDASDASDA");


        playerInv = pi;

        toolbarSlots = toolbarParent.GetComponentsInChildren<InventorySlotUI>(true);
        bagSlots     = bagParent    .GetComponentsInChildren<InventorySlotUI>(true);

        int total = toolbarSlots.Length + bagSlots.Length;
        playerInv.ResizeInventory(total);

        // assign indices and manager reference
        for (int i = 0; i < toolbarSlots.Length; i++)
            toolbarSlots[i].Setup(i, this);
        for (int i = 0; i < bagSlots.Length; i++)
            bagSlots[i].Setup(i + toolbarSlots.Length, this);

        // subscribe
        playerInv.GetInventory().OnInventoryChanged           += RefreshUI;
        playerInv.GetToolbarSelector().OnSelectedIndexChanged += UpdateHighlight;

        // initial draw
        RefreshUI();
        UpdateHighlight(playerInv.GetSelectedIndex());
    }

    /// <summary>Called by InventorySlotUI on a drop.</summary>
    public void OnSlotDrop(int fromIndex, int toIndex)
    {
        playerInv.SwapItems(fromIndex, toIndex);
        ForceRefreshUI();
        playerInv.RefreshSelection(); // make equipment update if needed
    }

    public void RefreshUI()
    {
        var inv = playerInv.GetInventory();

        for (int i = 0; i < toolbarSlots.Length; i++)
            DrawSlot(toolbarSlots[i], inv.GetSlotAt(i));
        for (int i = 0; i < bagSlots.Length; i++)
            DrawSlot(bagSlots[i], inv.GetSlotAt(i + toolbarSlots.Length));
    }

    private void DrawSlot(InventorySlotUI ui, InventorySlot slot)
    {
        foreach (Transform c in ui.transform) Destroy(c.gameObject);

        if (slot != null && slot.item != null && slot.quantity > 0)
        {
            var v = Instantiate(itemPrefab, ui.transform);
            v.Initialize(slot.item.data.icon, slot.quantity, ui.GetSlotIndex());
        }
    }

    private void UpdateHighlight(int idx)
    {
        for (int i = 0; i < toolbarSlots.Length; i++)
            toolbarSlots[i].SetSelected(i == idx, selectedColor, normalColor);
    }

    public void ForceRefreshUI()
    {
        RefreshUI();
        UpdateHighlight(playerInv.GetSelectedIndex());
    }
}
