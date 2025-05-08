using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Abstract base for simulated vehicle parts. Inherits ItemInstance for base data/grabbing,
/// adds vehicle context (OwningVehicle), connection logic, and simulation tick hooks.
/// </summary>
public abstract class PartInstance : ItemInstance { // Inherit ItemInstance

    public VehicleRoot OwningVehicle { get; private set; }

    protected Dictionary<string, PartInstance> _connectedParts = new Dictionary<string, PartInstance>();
    protected List<MountPoint> _selfMountPoints = new List<MountPoint>();

    // Awake inherited from ItemInstance is usually sufficient

    /// <summary>
    /// PartInstance specific initialization AFTER base ItemInstance setup.
    /// Sets OwningVehicle, validates part-specific data, sets up mounts/physics.
    /// </summary>
    public virtual void Initialize(InventoryItem itemInstance, VehicleRoot vehicleRoot) {
        // Call base Initialize FIRST (assigns ItemInstanceData, basic validation, physics)
        base.Initialize(itemInstance);
        if (!this.enabled) return; // Check if base failed

        // Set PartInstance specific fields
        this.OwningVehicle = vehicleRoot;

        // Part-specific validation (ensure it's CarPartData and has IPartRuntimeState)
        if (!(GetItemData<ItemData>() is CarPartData)) { Debug.LogError($"[{gameObject.name}] PartInstance Error: ItemData not CarPartData!", this); enabled = false; return; }
        if (!(GetRuntimeState<IRuntimeState>() is IPartRuntimeState)) { Debug.LogError($"[{gameObject.name}] PartInstance Error: Runtime state not IPartRuntimeState!", this); enabled = false; return; }

        SetupMountsAndPhysics(); // Setup mounts

        // Call hook for derived class specific transient setup
        InitializeTransientState();
    }

    /// <summary> VIRTUAL HOOK for derived setup after Initialize </summary>
    protected virtual void InitializeTransientState() { }

    // UpdateInventoryItemRuntimeState inherited - override if derived part caches state

    protected void SetupMountsAndPhysics() { _selfMountPoints.Clear(); GetComponentsInChildren<MountPoint>(true, _selfMountPoints); foreach (var mp in _selfMountPoints) mp?.Initialize(this); UpdatePhysicsState(); }
    protected new void UpdatePhysicsState() { // Use 'new' as signature matches protected ItemInstance method
        if (_rigidbody != null) { bool k = (OwningVehicle != null) || _originalRigidbodyKinematicState; bool g = (OwningVehicle == null) && _originalRigidbodyGravityState; if (_rigidbody.isKinematic != k) _rigidbody.isKinematic = k; if (_rigidbody.useGravity != g) _rigidbody.useGravity = g; }
    }
    public virtual void OnPartConnected(string l, PartInstance c, string r) { if (!string.IsNullOrEmpty(l)) _connectedParts[l] = c; }
    public virtual void OnPartDisconnected(string l, PartInstance d) { if (!string.IsNullOrEmpty(l)) _connectedParts.Remove(l); }
    public virtual void SetOwningVehicle(VehicleRoot v) { this.OwningVehicle = v; UpdatePhysicsState(); foreach (var kvp in _connectedParts) kvp.Value?.SetOwningVehicle(v); }

    // --- Simulation Hooks (Specific to PartInstance) ---
    public virtual void PrePhysicsSimulateTick(float dt) { }
    public virtual void PostPhysicsSimulateTick(float dt) { }
    // --- End Simulation Hooks ---

    protected T GetConnectedPartViaInterface<T>(string id) where T : class { if (string.IsNullOrEmpty(id)) return null; if (_connectedParts.TryGetValue(id, out var p)) return p as T; return null; }
    protected PartInstance GetConnectedPart(string id) { if (string.IsNullOrEmpty(id)) return null; if (_connectedParts.TryGetValue(id, out var p)) return p; return null; }

    // --- Override IGrabbable ---
    public override bool CanGrab() { return this.OwningVehicle == null; } // Only grab unattached parts
    public override void OnGrabbed(Transform grabberTransform) { base.OnGrabbed(grabberTransform); /* Part specific layer logic? */ }
    public override void OnDropped(Vector3 dropVelocity) { base.OnDropped(dropVelocity); /* Part specific layer logic? */ }
}