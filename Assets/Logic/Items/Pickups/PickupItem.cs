using UnityEngine;

// Base abstract class for pickups
public abstract class PickupItem : MonoBehaviour, IInteractable
{
    [Header("FX (optional)")]
    [SerializeField] private GameObject pickupEffect;

    // Child classes (PickupStack, WeaponPickup) implement this
    // to define what item(s) they represent.
    protected abstract InventoryItem GetItemToPickup(); // Changed signature
    protected abstract int GetQuantityToPickup();      // Changed signature

    public void Interact(PlayerManager playerManager)
    {
        if (playerManager == null) return;

        // Get the PlayerInventory component via the PlayerManager property
        PlayerInventory inventory = playerManager.Inventory;
        if (inventory == null)
        {
            Debug.LogError("PlayerManager does not have an Inventory component!", playerManager);
            return;
        }

        // Get the item details from the specific pickup type
        InventoryItem itemInstance = GetItemToPickup();
        int quantity = GetQuantityToPickup();

        if (itemInstance == null || quantity <= 0)
        {
            Debug.LogWarning($"Pickup {name} provided null item or zero quantity.", this);
            return;
        }

        // Add the item to the player's inventory
        inventory.AddItem(itemInstance, quantity);
        Debug.Log($"Picked up {quantity}x {itemInstance.data.itemName}");

        // Optional visual effect
        if (pickupEffect)
            Instantiate(pickupEffect, transform.position, Quaternion.identity);

        // Destroy the pickup object from the world
        Destroy(gameObject);
    }
}