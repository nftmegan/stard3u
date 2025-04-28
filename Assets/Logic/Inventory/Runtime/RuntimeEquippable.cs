using UnityEngine;

public class RuntimeEquippable : MonoBehaviour, IRuntimeItem
{
    [SerializeField] private string itemCode;
    private InventoryItem runtimeInventoryItem;

    public string GetItemCode() => itemCode;

    public ItemData GetItemData() => runtimeInventoryItem?.data;

    public void SetItemData(ItemData itemData)
    {
        runtimeInventoryItem = new InventoryItem
        {
            data = itemData,
            quantity = 1
        };
    }

    public void SetInventoryItem(InventoryItem inventoryItem)
    {
        runtimeInventoryItem = inventoryItem;
    }

    public InventoryItem GetInventoryItem()
    {
        return runtimeInventoryItem;
    }
}