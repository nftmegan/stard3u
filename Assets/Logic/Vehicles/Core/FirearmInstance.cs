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
        // No firearm-specific Awake logic needed here typically
    }

    /// <summary>
    /// Initialize: Calls base, then validates the specific state type.
    /// </summary>
    public override void Initialize(InventoryItem itemInstance) {
        base.Initialize(itemInstance); // Sets ItemInstanceData, name, calls UpdatePhysicsForDrop
        if (!this.enabled) return; // Check if base Initialize failed

        // Validate that the runtime state is the correct type after base Initialize
        if (GetRuntimeState<FirearmRuntimeState>() == null) {
             // This indicates an issue with the InventoryItem provided (likely missing state or wrong type)
             Debug.LogError($"[{gameObject.name}] FirearmInstance initialized but FirearmRuntimeState is missing or invalid! Check Spawner/Item creation.", this);
             enabled = false; // Disable if state is wrong
        }
        // No other Firearm-specific initialization needed for the *dropped* instance.
    }

    // Optional: Override UpdateInventoryItemRuntimeState if needed, but typically
    // the state doesn't change while the firearm is just lying on the ground.
    // protected override void UpdateInventoryItemRuntimeState() {
    //     base.UpdateInventoryItemRuntimeState();
    //     // If there were any state changes possible while dropped (unlikely for firearm), sync here.
    // }

     // Optional: Override OnGrabbed/OnDropped/OnStored if FirearmInstance needs
     // specific behavior beyond the base ItemInstance implementation during these events.
     // public override void OnGrabbed(Transform grabberTransform) { base.OnGrabbed(grabberTransform); /* Firearm specific grab logic? */ }
     // public override void OnDropped(Vector3 dropVelocity) { base.OnDropped(dropVelocity); /* Firearm specific drop logic? */ }
     // public override void OnStored() { base.OnStored(); /* Firearm specific store logic? */ }
}