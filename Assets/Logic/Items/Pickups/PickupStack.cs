using UnityEngine;

// Pickup for simple stackable items
public sealed class PickupStack : PickupItem
{
    [Header("Stackable Item Details")]
    [SerializeField] private ItemData itemData; // Assign the stackable ItemData SO
    [SerializeField] [Min(1)] private int quantity = 1;

    protected override InventoryItem GetItemToPickup()
    {
        if (itemData == null)
        {
            Debug.LogError($"PickupStack '{name}' is missing ItemData!", this);
            return null;
        }
        if (!itemData.stackable)
        {
            Debug.LogWarning($"PickupStack '{name}' has non-stackable ItemData '{itemData.itemName}'. This might not behave as expected.", this);
            // Proceeding anyway, but might be better handled differently
        }

        // Use the static factory for simple stackable items
        return InventoryItem.CreateStack(itemData);
    }

    protected override int GetQuantityToPickup()
    {
        return quantity;
    }
}