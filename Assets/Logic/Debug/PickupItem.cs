using UnityEngine;

public class PickupItem : MonoBehaviour, IInteractable
{
    [Header("Pickup Settings")]
    [SerializeField] private InventorySlot inventorySlot;  // The InventorySlot to be picked up (including quantity)

    [Header("Feedback")]
    [SerializeField] private GameObject pickupEffect; // Effect when picking up

    public void Interact(PlayerManager player)
    {
        var inventory = player.GetInventory();

        if (inventory != null && inventorySlot != null && inventorySlot.item != null)
        {
            // Add the item to the inventory (using the quantity stored in InventorySlot)
            inventory.AddItem(inventorySlot.item.data, inventorySlot.quantity);

            // Show pickup effect (if any)
            if (pickupEffect != null)
                Instantiate(pickupEffect, transform.position, Quaternion.identity);

            // Destroy the pickup object after interacting
            Destroy(gameObject);
        }
        else
        {
            Debug.LogWarning("PickupItem: Missing item data or player inventory.");
        }
    }
}