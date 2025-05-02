using UnityEngine;

// Pickup for more complex items like firearms with pre-loaded state
public class PickupWeaponItem : PickupItem
{
    [Header("Base Weapon Definition")]
    [SerializeField] private FirearmItemData weaponData; // Assign FireArmItemData SO

    [Header("Initial State")]
    [SerializeField] [Min(0)] private int roundsInMag = 0;
    [SerializeField] private ItemData[] attachments = System.Array.Empty<ItemData>(); // Assign attachment ItemData SOs

    protected override InventoryItem GetItemToPickup()
    {
        if (weaponData == null)
        {
            Debug.LogError($"WeaponPickup '{name}' is missing WeaponData!", this);
            return null;
        }

        // 1. Create a new instance of the firearm using its factory method
        // This instance will have its own FirearmState payload.
        InventoryItem weaponInstance = weaponData.CreateInventoryItem(); // Assumes this method exists and returns InventoryItem
        if (weaponInstance == null || weaponInstance.runtime == null || !(weaponInstance.runtime is FirearmState))
        {
            Debug.LogError($"WeaponData '{weaponData.itemName}' failed to create a valid instance with FirearmState.", weaponData);
            return null;
        }

        FirearmState state = (FirearmState)weaponInstance.runtime;

        // 2. Pre-load the magazine if ammo type is defined
        if (roundsInMag > 0 && weaponData.ammoType != null)
        {
            int bulletsToAdd = Mathf.Clamp(roundsInMag, 0, weaponData.magazineSize);
            if (bulletsToAdd > 0)
            {
                // Create a stack of ammo and place it in the magazine slot
                state.magazine[0].item = new InventoryItem(weaponData.ammoType);
                state.magazine[0].quantity = bulletsToAdd;
            }
        }
        else if (roundsInMag > 0 && weaponData.ammoType == null)
        {
            Debug.LogWarning($"WeaponPickup '{name}' specifies roundsInMag but WeaponData '{weaponData.itemName}' has no Ammo Type defined.", weaponData);
        }

        // 3. Add specified attachments
        if (state.attachments != null && attachments != null)
        {
            for (int i = 0; i < state.attachments.Size && i < attachments.Length; i++)
            {
                if (attachments[i] != null)
                {
                    // Create attachment item (assuming attachments are simple items for now)
                    // A more complex system might require specific attachment runtime states
                    state.attachments[i].item = new InventoryItem(attachments[i]); // Use CreateStack if attachments have no state
                    state.attachments[i].quantity = 1;
                }
            }
        }

        // Return the fully configured weapon instance
        return weaponInstance;
    }

    protected override int GetQuantityToPickup()
    {
        // Complex items like weapons are typically not stackable themselves
        return 1;
    }
}