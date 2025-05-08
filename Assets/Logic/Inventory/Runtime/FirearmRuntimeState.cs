using UnityEngine;
using System; // For Serializable

[Serializable]
public class FirearmRuntimeState : IRuntimeState, ICloneableRuntimeState {

    [SerializeField] public ItemContainer magazine;
    [SerializeField] public ItemContainer attachments;
    [SerializeField] public int durability = 100; // Example simple durability

    // Constructor needed for setting container sizes
    public FirearmRuntimeState(int attachmentSlots = 0, int initialDurability = 100) {
        magazine = new ItemContainer(1); // Fixed size 1
        attachments = new ItemContainer(Mathf.Max(0, attachmentSlots));
        durability = initialDurability;
    }
    // Parameterless constructor needed by Unity Serializer sometimes
    public FirearmRuntimeState() : this(0, 100) {}

    public IRuntimeState Clone() {
        // Deep clone needed for containers
        FirearmRuntimeState clone = new FirearmRuntimeState(this.attachments?.Size ?? 0, this.durability);
        DeepCloneItemContainer(this.magazine, clone.magazine);
        DeepCloneItemContainer(this.attachments, clone.attachments);
        return clone;
    }

    // Deep Cloning Helper (Move to static Utility class recommended)
    private static void DeepCloneItemContainer(ItemContainer source, ItemContainer destination) {
         if (source == null || destination == null) return;
         if (destination.Size < source.Size) destination.Resize(source.Size);
         for (int i = 0; i < source.Size; i++) {
             InventorySlot sSlot = source.Slots[i];
             if (sSlot != null && !sSlot.IsEmpty() && sSlot.item != null && sSlot.item.data != null) {
                 IRuntimeState nestedClonedState = (sSlot.item.runtime as ICloneableRuntimeState)?.Clone();
                 InventoryItem clonedItem = new InventoryItem(sSlot.item.data, nestedClonedState);
                 destination.Slots[i] = new InventorySlot(clonedItem, sSlot.quantity);
             } else { destination.Slots[i] = new InventorySlot(null, 0); }
         }
         for (int i = source.Size; i < destination.Size; i++) { destination.Slots[i] = new InventorySlot(null, 0); }
    }
}