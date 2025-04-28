using UnityEngine;

public class PickupBow : MonoBehaviour, IInteractable
{
    [Header("Pickup Settings")]
    [SerializeField] private InventoryItem bowItem;

    [Header("Feedback")]
    [SerializeField] private GameObject pickupEffect;

    public void Interact(PlayerManager player)
    {
        var inventory = player.GetInventory();

        if (inventory != null && bowItem != null && bowItem.data != null)
        {
            inventory.AddItem(bowItem.data, bowItem.quantity);

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