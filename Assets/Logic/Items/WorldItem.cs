// In Assets/Scripts/Items/WorldItem.cs
using UnityEngine;

/// <summary>
/// Represents an item existing physically in the world (dropped).
/// Allows it to be picked up via the IGrabbable interface.
/// </summary>
[RequireComponent(typeof(Rigidbody))] // Dropped items should generally have physics
[RequireComponent(typeof(Collider))] // And a collider
public class WorldItem : MonoBehaviour, IGrabbable {

    [Tooltip("The static ItemData definition for this item.")]
    [SerializeField] private ItemData itemDefinition;

    // The specific instance data (durability, ammo etc.)
    // This needs to be *set* when the item is dropped from inventory.
    private InventoryItem _instanceItemData;

    private Rigidbody _rigidbody;
    private Collider[] _colliders;

    void Awake() {
        _rigidbody = GetComponent<Rigidbody>();
        _colliders = GetComponentsInChildren<Collider>(true);
         if (_rigidbody == null) Debug.LogError($"WorldItem on {gameObject.name} missing Rigidbody!", this);
         if (_colliders.Length == 0) Debug.LogError($"WorldItem on {gameObject.name} missing Collider!", this);
    }

    /// <summary>
    /// Call this immediately after instantiating the WorldItem prefab
    /// when dropping an item from inventory.
    /// </summary>
    public void Initialize(InventoryItem sourceInventoryItem) {
        if (sourceInventoryItem == null || sourceInventoryItem.data == null) {
            Debug.LogError($"WorldItem Initialize called with invalid InventoryItem!", this);
            Destroy(gameObject); // Destroy if invalid data
            return;
        }
        this.itemDefinition = sourceInventoryItem.data; // Set static data ref
        this._instanceItemData = sourceInventoryItem; // Store the whole item (with runtime state)

        // Update visuals if necessary based on item data/state
        gameObject.name = $"WorldItem_{itemDefinition.itemName}";
        MeshFilter mf = GetComponentInChildren<MeshFilter>();
        MeshRenderer mr = GetComponentInChildren<MeshRenderer>();
        // TODO: Set mesh/material based on itemDefinition or visual variants?
        // Example: if(mf != null && itemDefinition.worldMesh != null) mf.sharedMesh = itemDefinition.worldMesh;
    }


    // --- IGrabbable Implementation ---
    public InventoryItem GetInventoryItemData() {
        // Return the stored instance data (which includes runtime state)
        // If _instanceItemData is null, create a default one ONLY if itemDefinition exists
        if (_instanceItemData == null && itemDefinition != null) {
            // This might happen if placed in scene without Initialize being called
             Debug.LogWarning($"WorldItem {gameObject.name} returning default InventoryItem as instance data was null.", this);
             // Need a way to create default runtime state if ItemData requires it.
             // For now, just return a basic one. This path should ideally be avoided.
             _instanceItemData = new InventoryItem(itemDefinition);
        }
        return _instanceItemData;
    }

    public Transform GetTransform() {
        return this.transform;
    }

    public virtual bool CanGrab() {
        // Most world items can always be grabbed unless specific logic added
        return true;
    }

    public virtual void OnGrabbed(Transform grabberTransform) {
        if (_rigidbody != null) _rigidbody.isKinematic = true;
        if (_colliders != null) foreach (var col in _colliders) if (col != null) col.enabled = false;
        transform.SetParent(grabberTransform);
    }

    public virtual void OnDropped(Vector3 dropVelocity) {
        transform.SetParent(null);
        if (_colliders != null) foreach (var col in _colliders) if (col != null) col.enabled = true;
        if (_rigidbody != null) {
            _rigidbody.isKinematic = false;
            _rigidbody.linearVelocity = dropVelocity;
        }
    }

    public virtual void OnStored() {
        // Called by HandsBehavior just before Destroy(gameObject)
    }
    // --- End IGrabbable ---
}