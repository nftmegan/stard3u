// In Assets/Scripts/Player/Inventory/InventoryComponent.cs (or your path)
using UnityEngine;
using System;

[DisallowMultipleComponent]
public class InventoryComponent : MonoBehaviour
{
    [Tooltip("The desired size of the inventory container when the game starts.")]
    [SerializeField] private int initialSize = 30;

    [Tooltip("The actual data container for inventory items. Can be pre-populated in Inspector.")]
    [SerializeField] private ItemContainer container; // Serialized field

    public ItemContainer Container => container;

    private void Awake()
    {
        // Debug.Log($"[InventoryComponent AWAKE on {gameObject.name}] Called.", this); // Removed
        InitializeContainer();
        // Debug.Log($"[InventoryComponent AWAKE on {gameObject.name}] Finished. Container: {(container == null ? "NULL" : $"Size={container.Size}")}", this); // Removed
    }

    private void InitializeContainer()
    {
        initialSize = Mathf.Max(1, initialSize);
        // Debug.Log($"[InventoryComponent InitializeContainer] Current: {(container == null ? "NULL" : "EXISTS")}, Target: {initialSize}", this); // Removed

        if (container == null)
        {
            // Debug.Log($"[InventoryComponent InitializeContainer] Container was null. Creating new size {initialSize}.", this); // Removed
            container = new ItemContainer(initialSize);
        }
        else
        {
            int currentSerializedSize = container.Slots != null ? container.Slots.Length : 0;
            if (currentSerializedSize != initialSize)
            {
                // Debug.Log($"[InventoryComponent InitializeContainer] Resizing from {currentSerializedSize} to {initialSize}.", this); // Removed
                container.Resize(initialSize);
            }
            else
            {
                 // Optional: Check for null slots within existing container - keep warning if useful
                 bool neededInitialization = false;
                 for(int i = 0; i < container.Slots.Length; i++) {
                     if(container.Slots[i] == null) {
                         container.Slots[i] = new InventorySlot(null, 0);
                         neededInitialization = true;
                     }
                 }
                 if(neededInitialization) Debug.LogWarning($"[InventoryComponent on {gameObject.name}] Initialized null slots within existing container.", this); // Keep useful warning
            }
        }
        // Error checking after initialization attempt
        if (container == null) Debug.LogError($"[InventoryComponent InitializeContainer] CONTAINER IS STILL NULL AFTER INIT!", this); // Keep error
        else if (container.Slots == null) Debug.LogError($"[InventoryComponent InitializeContainer] CONTAINER.SLOTS IS NULL AFTER INIT!", this); // Keep error
        // else Debug.Log($"[InventoryComponent InitializeContainer] Container ready. Slot count: {container.Slots.Length}", this); // Removed success log
    }
}