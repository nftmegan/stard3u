// Assets/Logic/Items/Pickups/PickupItem.cs
using Game.InventoryLogic;
using UnityEngine;

/// <summary>
/// Base class for any object the player can pick up.
/// Children must override <see cref="BuildSlot"/> and return a *fresh*
/// InventorySlot representing their contents.
/// </summary>
public abstract class PickupItem : MonoBehaviour, IInteractable
{
    [Header("FX (optional)")]
    [SerializeField] private GameObject pickupEffect;

    /** Child classes provide the slot contents here */
    protected abstract InventorySlot BuildSlot();

    public void Interact(PlayerManager player)
    {
        var bag  = player.GetInventory();
        var slot = BuildSlot();                  // ← ask the subclass

        if (bag == null || slot?.item == null) return;

        bag.AddItem(slot.item, Mathf.Max(1, slot.quantity));

        if (pickupEffect)
            Instantiate(pickupEffect, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}