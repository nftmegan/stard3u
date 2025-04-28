using UnityEngine;
/// <summary>
/// Implement this interface on any MonoBehaviour that needs access to an inventory (e.g., tools, weapons).
/// It allows systems like EquipmentController to inject the owning inventory at runtime.
/// </summary>
public interface IInventoryUser
{
    void SetInventory(Inventory inventory);
}

public abstract class EquippableBehavior : MonoBehaviour, IItemInputReceiver, IInventoryUser
{
    protected Inventory inventory;

    public virtual void SetInventory(Inventory inventory)
    {
        this.inventory = inventory;
    }

    // Optionally override these in child classes
    public virtual void OnFire1Down() {}
    public virtual void OnFire1Hold() {}
    public virtual void OnFire1Up() {}

    public virtual void OnFire2Down() {}
    public virtual void OnFire2Hold() {}
    public virtual void OnFire2Up() {}

    public virtual void OnUtilityDown() {}
    public virtual void OnUtilityUp() {}

    public virtual void OnReloadDown() {}
}