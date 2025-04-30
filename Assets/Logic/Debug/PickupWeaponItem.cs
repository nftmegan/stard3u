// Assets/Logic/Items/Pickups/WeaponPickup.cs
using Game.InventoryLogic;
using UnityEngine;

/// <summary>Drop-in prefab script for a firearm with pre-loaded mag / attachments.</summary>
public class WeaponPickup : PickupItem
{
    [Header("Base weapon (FireArmItemData)")]
    public FireArmItemData weaponData;

    [Header("Magazine")]
    [Min(0)] public int roundsInMag = 0;          // ≤ magazineSize

    [Header("Attachments")]
    public ItemData[] attachments = System.Array.Empty<ItemData>();

    // --------------------------------------------------------------------

    protected override InventorySlot BuildSlot()
    {
        if (!weaponData)
        {
            Debug.LogError($"{name}: WeaponData missing!");
            return null;
        }

        // 1) runtime instance with empty state
        var item  = weaponData.CreateInventoryItem();
        var state = item.runtime as FirearmState;

        // 2) fill magazine
        int bullets = Mathf.Clamp(roundsInMag, 0, weaponData.magazineSize);
        if (bullets > 0 && weaponData.ammoType)
        {
            state.magazine[0].item     = InventoryItem.CreateStack(weaponData.ammoType);
            state.magazine[0].quantity = bullets;
        }

        // 3) add attachments
        for (int i = 0; i < state.attachments.Size && i < attachments.Length; i++)
        {
            if (!attachments[i]) continue;
            state.attachments[i].item     = InventoryItem.CreateStack(attachments[i]);
            state.attachments[i].quantity = 1;
        }

        return new InventorySlot(item, 1);        // non-stackable, qty=1
    }
}