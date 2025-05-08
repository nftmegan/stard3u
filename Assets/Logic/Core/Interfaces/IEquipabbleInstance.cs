// In Assets/Scripts/Player/Equipment/IEquippableInstance.cs (or your path)

/// <summary>
/// Interface implemented by the MonoBehaviour component that handles the
/// actual behavior of an equipped item (like EquippableBehavior).
/// Defines the initialization method signature expected by RuntimeEquippable.
/// </summary>
public interface IEquippableInstance {
    /// <summary>
    /// Initializes the instance with its runtime data, the entity holding the equipment,
    /// and the aiming provider.
    /// </summary>
    void Initialize(
        InventoryItem itemInstance,         // The item being equipped
        IEquipmentHolder holder,          // The entity holding this item
        IAimProvider aimProvider       // The aiming system for the holder
    );
}