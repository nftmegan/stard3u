using Game.InventoryLogic;

public interface IEquippableInstance
{
    /// <summary>
    /// Injects the runtime inventory item and optionally the owning container.
    /// </summary>
    void Initialize(InventoryItem itemInstance, ItemContainer ownerInventory);
}