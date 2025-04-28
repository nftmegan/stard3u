using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour, IDropHandler
{
    [SerializeField] private Image backgroundImage;

    [SerializeField] private int slotIndex;
    private InventoryUIManager uiManager;

    /// <summary>Called by InventoryUIManager during setup.</summary>
    public void Setup(int idx, InventoryUIManager manager)
    {
        slotIndex = idx;
        uiManager = manager;
    }

    public int GetSlotIndex() => slotIndex;

    public void SetSelected(bool isSelected, Color selColor, Color normColor)
    {
        if (backgroundImage != null)
            backgroundImage.color = isSelected ? selColor : normColor;
    }

    public void OnDrop(PointerEventData eventData)
    {
        var draggedUI = eventData.pointerDrag?.GetComponent<InventoryItemUI>();
        if (draggedUI == null) return;

        int from = draggedUI.GetSlotIndex();
        int to   = slotIndex;
        if (from == to) return;

        draggedUI.MarkAsDropped();
        uiManager.OnSlotDrop(from, to);
    }
}
