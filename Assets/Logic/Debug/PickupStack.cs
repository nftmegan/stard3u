// Assets/Logic/Items/Pickups/PickupStack.cs
using Game.InventoryLogic;
using UnityEngine;

/// <summary>
/// Pickup for any simple, stackable ItemData (ammo, berries, coins…).  
/// Drag this onto a world object, assign the data and quantity, and you’re done.
/// </summary>
public sealed class PickupStack : PickupItem
{
    [Header("Stackable item")]
    public ItemData itemData;

    [Min(1)]
    public int quantity = 1;

    protected override InventorySlot BuildSlot()
    {
        if (!itemData)
        {
            Debug.LogError($"{name}: ItemData missing!");
            return null;
        }

        var stackItem = InventoryItem.CreateStack(itemData);
        return new InventorySlot(stackItem, quantity);
    }
}