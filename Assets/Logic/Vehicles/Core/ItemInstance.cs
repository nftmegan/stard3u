using UnityEngine;

/// <summary>
/// Abstract base class for ANY item that exists physically in the world
/// and can be grabbed. Holds InventoryItem data and manages basic physics/grabbing.
/// </summary>
[RequireComponent(typeof(Collider))]
public abstract class ItemInstance : MonoBehaviour, IGrabbable {

    [Tooltip("The core data for this instance. Set via Initialize.")]
    [SerializeField] // For Debug Inspector viewing
    protected InventoryItem itemInstanceData;
    public InventoryItem ItemInstanceData => itemInstanceData; // Read-only public access

    // Common components
    protected Rigidbody _rigidbody;
    protected Collider[] _colliders;
    protected bool _originalRigidbodyKinematicState;
    protected bool _originalRigidbodyGravityState;
    protected int _originalLayer;

    /// <summary>
    /// Base Awake: Finds components, caches physics state.
    /// </summary>
    protected virtual void Awake() {
        _rigidbody = GetComponent<Rigidbody>(); // Can be null
        if (_rigidbody != null) { _originalRigidbodyKinematicState = _rigidbody.isKinematic; _originalRigidbodyGravityState = _rigidbody.useGravity; }
        else { _originalRigidbodyKinematicState = true; _originalRigidbodyGravityState = false; } // Sensible defaults if no RB
        _colliders = GetComponentsInChildren<Collider>(true);
        _originalLayer = gameObject.layer;
    }

    /// <summary>
    /// Main Initialization method for ALL ItemInstances. Assigns the authoritative InventoryItem data.
    /// Derived classes override to perform specific setup *after* calling base.Initialize().
    /// </summary>
    public virtual void Initialize(InventoryItem itemDataToAssign) {
        if (!ValidateItemInstance_Base(itemDataToAssign, gameObject.name)) {
             Debug.LogError($"[{gameObject.name}] Initialize failed: Invalid InventoryItem.", this);
             enabled = false; return;
        }
        this.itemInstanceData = itemDataToAssign;
        gameObject.name = $"ItemInst_{itemInstanceData.data?.itemName ?? GetType().Name}";
        UpdatePhysicsForDrop(); // Set initial physics for a non-held item
        enabled = true;
    }

    /// <summary>
    /// VIRTUAL: Updates the state object within ItemInstanceData FROM internal variables if needed,
    /// just before the item state is read (e.g., grabbing, storing).
    /// Base implementation checks for nulls. Derived classes override if they cache state.
    /// </summary>
    protected virtual void UpdateInventoryItemRuntimeState() {
        if (this.itemInstanceData == null || this.itemInstanceData.runtime == null) {
            // This is okay if the item type simply has no runtime state.
            // Error logging here might be too noisy. Log in derived classes if state is *expected* but null.
        }
    }

    /// <summary>
    /// Base validation for InventoryItem structure.
    /// </summary>
    protected bool ValidateItemInstance_Base(InventoryItem itemInstance, string contextObjectName) {
         if (itemInstance == null) { Debug.LogError($"[{contextObjectName}] Validate FAIL: Null InventoryItem!", this); return false; }
         if (itemInstance.data == null) { Debug.LogError($"[{contextObjectName}] Validate FAIL: ItemData is null!", this); return false; }
         // Runtime state validation/creation is handled by Spawner or specific Initialize overrides
         return true;
    }

    // Physics helpers remain protected
    protected void UpdatePhysicsForDrop() { if (_rigidbody != null) { _rigidbody.isKinematic = _originalRigidbodyKinematicState; _rigidbody.useGravity = _originalRigidbodyGravityState; } }
    protected void UpdatePhysicsForGrab() { if (_rigidbody != null) { _originalRigidbodyKinematicState = _rigidbody.isKinematic; _originalRigidbodyGravityState = _rigidbody.useGravity; _rigidbody.isKinematic = true; _rigidbody.useGravity = false; } }

    // --- Getters ---
    public T GetItemData<T>() where T : ItemData => ItemInstanceData?.data as T;
    public T GetRuntimeState<T>() where T : class, IRuntimeState => ItemInstanceData?.runtime as T;

    #region IGrabbable Implementation (public virtual methods)
    // These provide default grabbing behavior for all ItemInstances

    public virtual InventoryItem GetInventoryItemData() {
        if (ItemInstanceData == null) { Debug.LogError($"[{gameObject.name}] GetInventoryItemData: ItemInstanceData is NULL!", this); return null; }
        UpdateInventoryItemRuntimeState(); // Ensure state is synced if derived class overrides
        return this.ItemInstanceData;
    }

    public virtual Transform GetTransform() { return this.transform; }

    // Base CanGrab allows grabbing if enabled and initialized. PartInstance overrides this.
    public virtual bool CanGrab() { return this.enabled && ItemInstanceData != null; }

    public virtual void OnGrabbed(Transform grabberTransform) {
        UpdateInventoryItemRuntimeState(); // Sync state first
        UpdatePhysicsForGrab(); // Make kinematic etc.
        // Consider moving layer change logic here if ALL grabbables should change layer
        // gameObject.layer = LayerMask.NameToLayer("GrabbedObject");
    }

    public virtual void OnDropped(Vector3 dropVelocity) {
        transform.SetParent(null);
        UpdatePhysicsForDrop(); // Restore physics
        if (_rigidbody != null) {
            _rigidbody.linearVelocity = dropVelocity;
            _rigidbody.angularVelocity = Random.insideUnitSphere * 2f;
        }
        // Restore original layer if needed
        // gameObject.layer = _originalLayer;
    }

    public virtual void OnStored() { /* Called just before Destroy after storing */ }

    #endregion
}