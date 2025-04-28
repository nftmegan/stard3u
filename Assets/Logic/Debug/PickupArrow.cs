using UnityEngine;

public class PickupArrow : MonoBehaviour, IInteractable
{
    [Header("Pickup Settings")]
    [SerializeField] private InventoryItem arrowItem;

    [Header("Feedback")]
    [SerializeField] private GameObject pickupEffect;

    public void Interact(PlayerManager player)
    {
        var inventory = player.GetInventory();

        if (inventory != null && arrowItem != null && arrowItem.data != null)
        {
            inventory.AddItem(arrowItem.data, arrowItem.quantity);

            if (pickupEffect != null)
                Instantiate(pickupEffect, transform.position, Quaternion.identity);

            Destroy(gameObject);
        }
        else
        {
            Debug.LogWarning("PickupArrow: Missing item data or player inventory.");
        }
    }
}