// --- Assets/Logic/Items/Core/EquipabbleBehavior.cs ---
using UnityEngine;

public abstract class EquippableBehavior : MonoBehaviour, IItemInputReceiver, IEquippableInstance {
    protected InventoryItem runtimeItem { get; private set; }
    public InventoryItem RuntimeItemInstance => runtimeItem;

    protected IEquipmentHolder ownerEquipmentHolder { get; private set; }
    protected IAimProvider ownerAimProvider { get; private set; }
    
    // Keep PlayerGrabController accessible for derived classes like HandsBehavior
    protected PlayerGrabController playerGrabController { get; private set; }


    public virtual void Initialize(InventoryItem itemInstance, IEquipmentHolder holder, IAimProvider aimProvider) {
        this.runtimeItem = itemInstance;
        this.ownerEquipmentHolder = holder;
        this.ownerAimProvider = aimProvider;

        // Find PlayerGrabController - it's a common need for interaction
        if (holder is Component componentHolder)
        {
            PlayerManager pm = componentHolder.GetComponentInParent<PlayerManager>();
            if (pm != null)
            {
                this.playerGrabController = pm.GrabController;
            }
        }
        if (this.playerGrabController == null)
        {
            // Fallback if PlayerManager context isn't directly available via holder
            this.playerGrabController = FindFirstObjectByType<PlayerGrabController>();
        }
        if (this.playerGrabController == null && !(this is HandsBehavior)) // HandsBehavior specifically needs it
        {
            // Only log error if a non-Hands behavior couldn't find it, as HandsBehavior will log its own critical error.
            // Debug.LogWarning($"[{GetType().Name} on {gameObject.name}] PlayerGrabController not found. Some interactions might be limited.", this);
        }


        bool isConsideredFallback = (this is HandsBehavior) || itemInstance == null || itemInstance.data == null;

        if (this.ownerEquipmentHolder == null && !isConsideredFallback) {
            Debug.LogError($"[{GetType().Name} on {gameObject.name}] Initialize ERROR: Null IEquipmentHolder for non-fallback item '{itemInstance?.data?.itemName}'!", this);
            this.enabled = false; return;
        }
        if (this.ownerAimProvider == null) { 
            Debug.LogError($"[{GetType().Name} on {gameObject.name}] Initialize ERROR: Null IAimProvider!", this);
            this.enabled = false; return;
        }
    }

    // --- Input Handlers (Default implementations) ---
    public virtual void OnFire1Down() { }
    public virtual void OnFire1Hold() { }
    public virtual void OnFire1Up() { }
    public virtual void OnFire2Down() { }
    public virtual void OnFire2Hold() { }
    public virtual void OnFire2Up() { }
    public virtual void OnUtilityDown() { }
    public virtual void OnUtilityUp() { }
    public virtual void OnReloadDown() { }

    /// <summary>
    /// Base implementation for the Store action.
    /// Most equippables (like weapons) might only allow pulling from inventory if not grabbing.
    /// HandsBehavior will override this for full grab/store/pull functionality.
    /// </summary>
    public virtual void OnStoreDown()
    {
        if (playerGrabController == null) return;

        if (playerGrabController.IsGrabbing)
        {
            // If any equippable is active AND player is somehow also grabbing via PGC (unusual state),
            // default to trying to store what PGC is holding.
            playerGrabController.HandleStoreAction(); // PGC's HandleStoreAction will try to store.
        }
        else
        {
            // Default behavior for non-Hands equippables:
            // Allow pulling an item from inventory into the grab slot.
            playerGrabController.HandleStoreAction(); // PGC's HandleStoreAction will try to pull.
        }
    }

    protected virtual void OnEnable() { }
    protected virtual void OnDisable() { }
}