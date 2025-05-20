// --- Assets/Logic/Vehicles/Core/ItemInstance.cs ----------------------
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
    // Cache other original physics properties if needed (e.g., drag, angularDrag, constraints)
    // protected float _originalDrag;
    // protected float _originalAngularDrag;
    protected int _originalLayer;


    /// <summary>
    /// Base Awake: Finds components, caches physics state.
    /// </summary>
    protected virtual void Awake() {
        _rigidbody = GetComponent<Rigidbody>(); 
        if (_rigidbody != null) {
            _originalRigidbodyKinematicState = _rigidbody.isKinematic;
            _originalRigidbodyGravityState = _rigidbody.useGravity;
            // _originalDrag = _rigidbody.drag;
            // _originalAngularDrag = _rigidbody.angularDrag;
        }
        else { // Sensible defaults if no RB, though IGrabbable usually implies a physics object
            _originalRigidbodyKinematicState = true; 
            _originalRigidbodyGravityState = false;
        }
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
        
        // If this item is instantiated directly into the world (not via player grabbing then dropping),
        // ensure its physics state is correctly set based on its original prefab settings.
        UpdatePhysicsForInitialSpawn();
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
        }
    }

    /// <summary>
    /// Base validation for InventoryItem structure.
    /// </summary>
    protected bool ValidateItemInstance_Base(InventoryItem itemInstance, string contextObjectName) {
         if (itemInstance == null) { Debug.LogError($"[{contextObjectName}] Validate FAIL: Null InventoryItem!", this); return false; }
         if (itemInstance.data == null) { Debug.LogError($"[{contextObjectName}] Validate FAIL: ItemData is null!", this); return false; }
         return true;
    }

    /// <summary>
    /// Sets the Rigidbody state when the item is initially spawned into the world
    /// (e.g., by an ItemSpawner, not when dropped by player after grabbing).
    /// </summary>
    protected void UpdatePhysicsForInitialSpawn() {
        if (_rigidbody != null) {
            _rigidbody.isKinematic = _originalRigidbodyKinematicState;
            _rigidbody.useGravity = _originalRigidbodyGravityState;
            // _rigidbody.drag = _originalDrag;
            // _rigidbody.angularDrag = _originalAngularDrag;
        }
    }

    /// <summary>
    /// Sets the Rigidbody state when the item is grabbed by the player.
    /// It's important that the _original... states are cached *before* this is called if not already done in Awake.
    /// </summary>
    protected void UpdatePhysicsForGrab() {
        if (_rigidbody != null) {
            // Cache current state if not already done (e.g. if Awake didn't run or was overridden)
            // This is a safety net, Awake should handle the primary caching.
            if(!Application.isPlaying || Time.frameCount < 2) // rough check if awake might not have set them yet
            {
                 _originalRigidbodyKinematicState = _rigidbody.isKinematic;
                 _originalRigidbodyGravityState = _rigidbody.useGravity;
            }

            _rigidbody.isKinematic = true;
            _rigidbody.useGravity = false;
            // Optionally, you might want to set drag/angularDrag to 0 while held
            // _rigidbody.drag = 0f;
            // _rigidbody.angularDrag = 0.05f; // Small angular drag can help stabilize
        }
    }


    // --- Getters ---
    public T GetItemData<T>() where T : ItemData => ItemInstanceData?.data as T;
    public T GetRuntimeState<T>() where T : class, IRuntimeState => ItemInstanceData?.runtime as T;

    #region IGrabbable Implementation (public virtual methods)

    public virtual InventoryItem GetInventoryItemData() {
        if (ItemInstanceData == null) { Debug.LogError($"[{gameObject.name}] GetInventoryItemData: ItemInstanceData is NULL!", this); return null; }
        UpdateInventoryItemRuntimeState(); 
        return this.ItemInstanceData;
    }

    public virtual Transform GetTransform() { return this.transform; }

    public virtual bool CanGrab() { return this.enabled && ItemInstanceData != null; }

    public virtual void OnGrabbed(Transform grabberTransform) {
        UpdateInventoryItemRuntimeState(); 
        UpdatePhysicsForGrab(); 
    }

    /// <summary>
    /// Called by PlayerGrabController when the item is dropped.
    /// PlayerGrabController is responsible for making the Rigidbody dynamic and applying initial velocities.
    /// This method should handle unparenting and any item-specific logic on being dropped.
    /// </summary>
    public virtual void OnDropped(Vector3 dropVelocity) {
        transform.SetParent(null);
        // DO NOT call UpdatePhysicsForInitialSpawn() or UpdatePhysicsForDrop() here if PGC manages the drop physics state.
        // The Rigidbody's kinematic and gravity state should have already been set by PlayerGrabController.
        // If you need to restore other specific Rigidbody properties (like constraints, drag, if modified by OnGrabbed),
        // do that here selectively, ensuring not to override isKinematic or useGravity.
        // For example:
        // if (_rigidbody != null) {
        //     _rigidbody.drag = _originalDrag;
        //     _rigidbody.angularDrag = _originalAngularDrag;
        // }
    }

    public virtual void OnStored() { /* Called just before Destroy after storing */ }

    #endregion
}