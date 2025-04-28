using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InventorySlotUI : MonoBehaviour, IDropHandler
{
    [SerializeField] private Image backgroundImage;
    private int slotIndex;

    public void Setup(int index)
    {
        slotIndex = index;
    }

    public void SetSelected(bool isSelected, Color selectedColor, Color normalColor)
    {
        if (backgroundImage != null)
            backgroundImage.color = isSelected ? selectedColor : normalColor;
    }

    public int GetSlotIndex()
    {
        return slotIndex;
    }

    public void SetSlotIndex(int index)
    {
        slotIndex = index;
    }

    public void OnDrop(PointerEventData eventData)
    {
        var draggedItem = eventData.pointerDrag?.GetComponent<InventoryItemUI>();
        if (draggedItem == null)
        {
            Debug.LogWarning("[InventorySlotUI] No valid item dragged.");
            return;
        }

        int fromIndex = draggedItem.GetSlotIndex();
        int toIndex = slotIndex;

        Debug.Log($"[InventorySlotUI] Dragged from {fromIndex} to {toIndex}");

        if (fromIndex == toIndex)
        {
            Debug.Log($"[InventorySlotUI] Dropped onto same slot {toIndex}, skipping swap.");
            return;
        }

        var playerManager = FindFirstObjectByType<PlayerManager>();
        if (playerManager == null)
        {
            Debug.LogError("[InventorySlotUI] PlayerManager not found!");
            return;
        }

        var inventory = playerManager.GetInventory();
        if (inventory == null)
        {
            Debug.LogError("[InventorySlotUI] PlayerInventory not found!");
            return;
        }

        inventory.SwapItems(fromIndex, toIndex);

        var inventoryUI = FindFirstObjectByType<InventoryUIManager>();
        if (inventoryUI != null)
        {
            inventoryUI.ForceRefreshUI(); // ✅ Refresh completely
        }
        else
        {
            Debug.LogError("[InventorySlotUI] InventoryUIManager not found!");
        }

        // Destroy the dragged UI item because it will be recreated properly
        Destroy(draggedItem.gameObject);
    }
}