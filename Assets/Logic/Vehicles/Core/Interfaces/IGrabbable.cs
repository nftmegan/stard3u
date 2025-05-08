// In Assets/Scripts/Core/Interfaces/IGrabbable.cs
using UnityEngine;

/// <summary>
/// Interface for objects in the world that can be picked up and manipulated by the player's hands
/// or potentially other interaction systems.
/// </summary>
public interface IGrabbable {
    /// <summary>
    /// The InventoryItem data (static definition + runtime state) associated with this grabbable object.
    /// </summary>
    InventoryItem GetInventoryItemData();

    /// <summary>
    /// The Transform of the GameObject representing this grabbable object in the world.
    /// </summary>
    Transform GetTransform();

    /// <summary>
    /// Called when the player successfully starts grabbing this object.
    /// The object should disable its physics simulation (make Rigidbody kinematic, disable colliders).
    /// </summary>
    /// <param name="grabberTransform">The transform that is now holding this object (e.g., player's handHoldPoint).</param>
    void OnGrabbed(Transform grabberTransform);

    /// <summary>
    /// Called when the player drops this object.
    /// The object should re-enable its physics simulation.
    /// </summary>
    /// <param name="dropVelocity">Optional velocity to apply when dropped.</param>
    void OnDropped(Vector3 dropVelocity);

    /// <summary>
    /// Called just before the object's GameObject is destroyed because it was successfully stored in an inventory.
    /// Allows for any final cleanup if needed.
    /// </summary>
    void OnStored();

    /// <summary>
    /// Can this item currently be grabbed? (e.g., not bolted down, not too heavy without tool)
    /// </summary>
    bool CanGrab(); // Added for checking pre-grab conditions
}