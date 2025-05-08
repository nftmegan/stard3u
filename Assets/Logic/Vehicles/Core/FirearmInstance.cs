using UnityEngine;

/// <summary>
/// Represents a firearm existing as a grabbable object in the world.
/// Inherits ItemInstance to handle data and grabbing.
/// Holds FirearmRuntimeState but performs no active simulation when dropped.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class FirearmInstance : ItemInstance { // Inherit directly from ItemInstance

    // No simulation variables needed for a dropped firearm instance

    protected override void Awake() {
        base.Awake();
    }

    /// <summary>
    /// Initialize: Calls base, then validates the specific state type.
    /// </summary>
    public override void Initialize(InventoryItem itemInstance) {
        base.Initialize(itemInstance); // Sets ItemInstanceData
        if (!this.enabled) return;

        // Validate that the runtime state is correct after base Initialize
        if (GetRuntimeState<FirearmRuntimeState>() == null) {
             // This indicates an issue with the InventoryItem provided or default creation
             Debug.LogError($"[{gameObject.name}] FirearmInstance initialized but FirearmRuntimeState is missing or invalid! Check Spawner/Item creation.", this);
             enabled = false;
        }
    }
}