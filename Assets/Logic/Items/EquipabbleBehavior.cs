using Game.InventoryLogic;
using UnityEngine;

public abstract class EquippableBehavior : MonoBehaviour, IItemInputReceiver, IEquippableInstance
{
    protected InventoryItem runtimeItem;
    protected ItemContainer ownerInventory;

    public virtual void SetInventory(ItemContainer container)
    {
        ownerInventory = container;
    }

    public virtual void Initialize(InventoryItem itemInstance, ItemContainer ownerInventory)
    {
        this.runtimeItem = itemInstance;
        this.ownerInventory = ownerInventory;
    }

    // Input methods to override
    public virtual void OnFire1Down()   { }
    public virtual void OnFire1Hold()   { }
    public virtual void OnFire1Up()     { }

    public virtual void OnFire2Down()   { }
    public virtual void OnFire2Hold()   { }
    public virtual void OnFire2Up()     { }

    public virtual void OnUtilityDown() { }
    public virtual void OnUtilityUp()   { }

    public virtual void OnReloadDown()  { }
}