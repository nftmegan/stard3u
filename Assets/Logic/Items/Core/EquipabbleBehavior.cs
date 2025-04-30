using UnityEngine;

// Removed namespace

/// <summary>
/// Abstract base class for behaviors attached to player-equipped items.
/// Handles receiving input and being initialized with specific item data
/// and the inventory container it belongs to.
/// </summary>
public abstract class EquippableBehavior : MonoBehaviour, IItemInputReceiver, IEquippableInstance
{
    /// <summary>
    /// The specific InventoryItem instance this behavior represents,
    /// potentially containing unique runtime state (e.g., durability, ammo).
    /// Accessible by derived classes.
    /// </summary>
    protected InventoryItem runtimeItem { get; private set; }

    /// <summary>
    /// The ItemContainer this equipped item belongs to (e.g., the player's main inventory).
    /// Used for actions like consuming ammo or checking inventory contents.
    /// Accessible by derived classes.
    /// </summary>
    protected ItemContainer ownerInventory { get; private set; }

    /// <summary>
    /// Initializes the equippable behavior with its specific item data
    /// and a reference to the inventory it originates from.
    /// Called by the EquipmentController when the item is equipped.
    /// </summary>
    /// <param name="itemInstance">The specific InventoryItem instance being equipped.</param>
    /// <param name="inventory">The ItemContainer the item belongs to.</param>
    public virtual void Initialize(InventoryItem itemInstance, ItemContainer inventory)
    {
        // --- MODIFIED ERROR CHECKING ---
        // It's expected that itemInstance might be null for the fallback/unarmed state (like Hands).
        // Don't log an error in that specific case.
        if (itemInstance == null)
        {
            // This is likely the unarmed state, which is okay.
            // Debug.Log($"[{this.GetType().Name}] Initialized with null itemInstance (Expected for unarmed/fallback).");
        }

        // Log an error if the inventory is null, ONLY IF an item instance *was* provided.
        // The unarmed state doesn't strictly need an inventory container reference.
        if (inventory == null && itemInstance != null)
        {
            // This is more likely a real problem - an actual item is being equipped
            // but the inventory reference is missing.
            Debug.LogError($"[{GetType().Name}] Initialized with a null owner Inventory container for item '{itemInstance.data?.itemName ?? "Unknown"}'!", this);
        }
        // --- End Modified Checks ---


        // Assign the values regardless of null checks (they might be intentionally null)
        runtimeItem = itemInstance;
        ownerInventory = inventory;
    }

    // --- IItemInputReceiver Implementation (Virtual methods for override) ---

    /// <summary>Called when the primary action input starts (e.g., left mouse button down).</summary>
    public virtual void OnFire1Down()   { }
    /// <summary>Called every frame the primary action input is held.</summary>
    public virtual void OnFire1Hold()   { }
    /// <summary>Called when the primary action input is released.</summary>
    public virtual void OnFire1Up()     { }

    /// <summary>Called when the secondary action input starts (e.g., right mouse button down).</summary>
    public virtual void OnFire2Down()   { }
    /// <summary>Called every frame the secondary action input is held.</summary>
    public virtual void OnFire2Hold()   { }
    /// <summary>Called when the secondary action input is released.</summary>
    public virtual void OnFire2Up()     { }

    /// <summary>Called when the utility action input starts (e.g., G key down).</summary>
    public virtual void OnUtilityDown() { }
    /// <summary>Called when the utility action input is released.</summary>
    public virtual void OnUtilityUp()   { }

    /// <summary>Called when the reload action input starts (e.g., R key down).</summary>
    public virtual void OnReloadDown()  { }

    // Optional: Add OnEnable/OnDisable if common setup/cleanup is needed across equippables
    protected virtual void OnEnable() { }
    protected virtual void OnDisable() { }
}