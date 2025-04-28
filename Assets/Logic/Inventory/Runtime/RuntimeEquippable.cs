using UnityEngine;

public class RuntimeEquippable : MonoBehaviour, IRuntimeItem
{
    [SerializeField] private string itemCode;
    public InventoryItem runtimeInventoryItem;

    public string GetItemCode() => itemCode;

    public ItemData GetItemData() => runtimeInventoryItem?.data;

    public void SetItemData(ItemData itemData)
    {
        runtimeInventoryItem = new InventoryItem
        {
            data = itemData
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