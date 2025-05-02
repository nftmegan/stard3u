using UnityEngine;
using UnityEngine.UI;

/// <summary>Very simple inspector window.  Expand later with attachment slots.</summary>
public class ItemDetailUI : MonoBehaviour
{
    [Header("Widgets")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Text  nameText;
    [SerializeField] private Text  quantityText;
    [SerializeField] private GameObject root;   // panel root to toggle

    private int inspectedSlot = -1;

    internal void ShowFor(InventoryItem item, int qty, int slotIndex)
    {
        inspectedSlot = slotIndex;

        iconImage.sprite = item.data.sprite;
        nameText.text    = item.data.itemName;
        quantityText.text = $"x{qty}";

        root.SetActive(true);
    }

    public void Hide() => root.SetActive(false);
}