using UnityEngine;

public abstract class EquippableBehavior : MonoBehaviour, IItemInputReceiver, IEquippableInstance {
    protected InventoryItem runtimeItem { get; private set; }
    public InventoryItem RuntimeItemInstance => runtimeItem;

    // Contexts directly available to all behaviors
    protected IEquipmentHolder ownerEquipmentHolder { get; private set; }
    protected IAimProvider ownerAimProvider { get; private set; }
    // Removed: protected EquipmentController _equipmentControllerInternal;

    // Simplified Initialize - No need to find EquipmentController here anymore
    public virtual void Initialize(InventoryItem itemInstance, IEquipmentHolder holder, IAimProvider aimProvider) {
        this.runtimeItem = itemInstance;
        this.ownerEquipmentHolder = holder;
        this.ownerAimProvider = aimProvider;

        bool isConsideredFallback = (this is HandsBehavior) || itemInstance == null || itemInstance.data == null;

        // Validate essential contexts
        if (this.ownerEquipmentHolder == null && !isConsideredFallback) {
            Debug.LogError($"[{GetType().Name} on {gameObject.name}] Initialize ERROR: Null IEquipmentHolder for non-fallback item '{itemInstance?.data?.itemName}'!", this);
            this.enabled = false; return;
        }
        if (this.ownerAimProvider == null) { // Aim provider is generally always needed
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

    // Base OnStoreDown does nothing by default now.
    // HandsBehavior MUST override this for its store/pull logic.
    // Other behaviors could override if they have a specific 'Store' action.
    public virtual void OnStoreDown() { }
    // --- Lifecycle ---
    protected virtual void OnEnable() { }
    protected virtual void OnDisable() { }
}