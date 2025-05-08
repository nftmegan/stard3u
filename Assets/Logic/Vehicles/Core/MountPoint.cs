using UnityEngine;
using System.Linq; // For Any

/// <summary>
/// Represents a connection point on a PartInstance where other parts can be attached.
/// Handles compatibility checks and attachment/detachment logic.
/// </summary>
public class MountPoint : MonoBehaviour {

    [Tooltip("Unique ID for this mount point within its parent PartData (e.g., 'EngineOutput', 'WheelHub_FL'). MUST match an ID in the ParentPartInstance's CarPartData.providedMountPoints list.")]
    public string mountPointDefinitionID; // Set this in the Inspector on the prefab

    private MountPointDefinition _definition; // Cached definition

    // --- Properties ---
    /// <summary>
    /// The PartInstance that owns this MountPoint.
    /// </summary>
    public PartInstance ParentPartInstance { get; private set; }

    /// <summary>
    /// The PartInstance currently attached to this MountPoint (null if empty).
    /// </summary>
    public PartInstance CurrentlyAttachedPart { get; private set; }

    /// <summary>
    /// Gets the MountPointDefinition associated with this mount point ID from the ParentPartInstance's data.
    /// Caches the result for performance.
    /// </summary>
    public MountPointDefinition Definition {
        get {
            // Only search if definition is null AND we have a valid parent
            if (_definition == null && ParentPartInstance != null) {
                // Use the correct generic GetItemData method inherited by PartInstance
                CarPartData parentDef = ParentPartInstance.GetItemData<CarPartData>();
                if (parentDef != null) {
                    if (parentDef.providedMountPoints != null) {
                        _definition = parentDef.providedMountPoints.Find(mpd => mpd.mountPointID == mountPointDefinitionID);
                        if (_definition == null) {
                             // Keep warning, this is a setup error
                             Debug.LogWarning($"MountPoint '{mountPointDefinitionID}' on '{ParentPartInstance.name}' not found in its CarPartData.providedMountPoints list. Check definition IDs.", ParentPartInstance);
                        }
                    } else {
                         Debug.LogWarning($"MountPoint '{mountPointDefinitionID}' on '{ParentPartInstance.name}': Parent CarPartData '{parentDef.name}' has a null providedMountPoints list.", ParentPartInstance);
                    }
                }
                // else: ParentPartInstance doesn't have CarPartData (shouldn't happen if validation is correct)
            }
            return _definition;
        }
    }

    /// <summary>
    /// Initializes the MountPoint with its parent PartInstance.
    /// Called by PartInstance during its setup.
    /// </summary>
    public void Initialize(PartInstance parent) {
        this.ParentPartInstance = parent;
        // Validate required fields
        if (string.IsNullOrEmpty(mountPointDefinitionID)) {
            Debug.LogError($"MountPoint on '{parent?.name ?? "Unknown Parent"}' is missing its required 'Mount Point Definition ID'!", this);
        }
        _definition = null; // Clear cached definition on re-initialization
    }

    /// <summary>
    /// Checks if the given partToAttach is compatible with this MountPoint's definition.
    /// </summary>
    public bool IsCompatible(PartInstance partToAttach) {
        // Ensure definition is resolved and part exists
        MountPointDefinition currentDef = this.Definition; // Use property to resolve if needed
        if (currentDef == null) {
             Debug.LogWarning($"[{ParentPartInstance?.name ?? "UnknownParent"}.{mountPointDefinitionID}] IsCompatible check failed: MountPointDefinition is unresolved. Check ID and parent's data.", this);
             return false;
        }
        if (partToAttach == null) {
             // Debug.LogWarning($"[{ParentPartInstance?.name ?? "UnknownParent"}.{mountPointDefinitionID}] IsCompatible check failed: partToAttach is null.", this);
             return false;
        }

        // Ensure part to attach has valid data
        CarPartData dataOfPartToAttach = partToAttach.GetItemData<CarPartData>();
        if (dataOfPartToAttach == null) {
             Debug.LogWarning($"[{ParentPartInstance?.name ?? "UnknownParent"}.{mountPointDefinitionID}] IsCompatible check failed: Part '{partToAttach.name}' is missing CarPartData.", partToAttach);
             return false;
        }

        // Check if already occupied
        if (CurrentlyAttachedPart != null) return false;

        // Check PartType compatibility
        // If acceptedPartTypes list is empty, it accepts *any* PartType (use carefully).
        bool typeMatch = !currentDef.acceptedPartTypes.Any() || currentDef.acceptedPartTypes.Contains(dataOfPartToAttach.partTypeEnum);
        if (!typeMatch) {
            // Debug.Log($"Type mismatch: Mount '{mountPointDefinitionID}' accepts [{string.Join(",", Definition.acceptedPartTypes)}] but got {dataOfPartToAttach.partTypeEnum}");
            return false;
        }

        // Optional: Check required interfaces (using Reflection, can be slow)
        if (currentDef.requiredInterfaces != null && currentDef.requiredInterfaces.Count > 0) {
            foreach (string interfaceName in currentDef.requiredInterfaces) {
                if (string.IsNullOrEmpty(interfaceName)) continue;
                // Robust type lookup requires searching assemblies if not in default
                System.Type interfaceType = System.Type.GetType(interfaceName, false); // false = don't throw exception
                 if (interfaceType == null) {
                     // Try searching loaded assemblies (more robust but slower first time)
                     // interfaceType = AppDomain.CurrentDomain.GetAssemblies()
                     //                  .SelectMany(asm => asm.GetTypes())
                     //                  .FirstOrDefault(t => t.IsInterface && t.Name == interfaceName);
                      Debug.LogWarning($"MountPoint '{mountPointDefinitionID}': Could not find required interface type '{interfaceName}'. Check spelling/assembly.", this);
                      // Decide: fail compatibility or ignore missing interface? Fail is safer.
                      return false;
                 }

                if (!interfaceType.IsInterface) {
                     Debug.LogWarning($"MountPoint '{mountPointDefinitionID}': Required interface name '{interfaceName}' is not actually an interface type.", this);
                     return false;
                }

                // Check if the PartInstance's script implements the interface
                if (!interfaceType.IsAssignableFrom(partToAttach.GetType())) {
                    // Debug.Log($"Part {dataOfPartToAttach.itemName} ({partToAttach.GetType().Name}) does not implement required interface {interfaceName} for mount {mountPointDefinitionID}");
                    return false; // Interface requirement not met
                }
            }
        }

        // All checks passed
        return true;
    }

    /// <summary>
    /// Attempts to attach the given part to this mount point.
    /// Performs compatibility check, parents the transform, and notifies connected parts.
    /// </summary>
    /// <returns>True if attachment was successful, false otherwise.</returns>
    public bool TryAttach(PartInstance partToAttach) {
        if (!IsCompatible(partToAttach)) {
            // Debug.Log($"Attach failed: {partToAttach?.name ?? "NullPart"} not compatible with {ParentPartInstance?.name ?? "NullParent"}.{mountPointDefinitionID}");
            return false;
        }

        CurrentlyAttachedPart = partToAttach;
        Transform partTransform = partToAttach.transform;

        // Attach physically
        partTransform.SetParent(this.transform, false); // Use worldPositionStays = false
        partTransform.localPosition = Vector3.zero;   // Snap to mount point origin
        partTransform.localRotation = Quaternion.identity; // Snap to mount point rotation

        // Notify parts about connection (simplistic remote ID handling)
        string remoteMountID = "unknown_connection"; // TODO: Implement proper handshake if needed
        ParentPartInstance?.OnPartConnected(this.mountPointDefinitionID, partToAttach, remoteMountID);
        partToAttach.OnPartConnected(remoteMountID, this.ParentPartInstance, this.mountPointDefinitionID);

        // Update vehicle hierarchy
        partToAttach.SetOwningVehicle(ParentPartInstance?.OwningVehicle); // Propagate VehicleRoot
        ParentPartInstance?.OwningVehicle?.RegisterPart(partToAttach, partToAttach.ItemInstanceData); // Register with vehicle

        // Debug.Log($"Attached {partToAttach.name} to {ParentPartInstance?.name ?? "Root?"} at {mountPointDefinitionID}");
        return true;
    }

    /// <summary>
    /// Detaches the currently attached part, if any.
    /// Unparents the transform, notifies parts, and unregisters from the vehicle.
    /// </summary>
    /// <returns>The detached PartInstance, or null if nothing was attached.</returns>
    public PartInstance Detach() {
        if (CurrentlyAttachedPart == null) return null;

        PartInstance detachedPart = CurrentlyAttachedPart;
        CurrentlyAttachedPart = null; // Clear reference first

        // Notify parts about disconnection (simplistic remote ID handling)
        string remoteMountID = "unknown_connection";
        ParentPartInstance?.OnPartDisconnected(this.mountPointDefinitionID, detachedPart);
        detachedPart.OnPartDisconnected(remoteMountID, this.ParentPartInstance);

        // Unregister from vehicle and clear owning vehicle reference
        ParentPartInstance?.OwningVehicle?.UnregisterPart(detachedPart);
        detachedPart.SetOwningVehicle(null);

        // Unparent and let physics take over (OnDropped will be called by interaction logic)
        detachedPart.transform.SetParent(null, true); // Keep world position

        // Debug.Log($"Detached {detachedPart.name} from {ParentPartInstance?.name ?? "Root?"} at {mountPointDefinitionID}");
        return detachedPart;
    }

    // --- Gizmo Visualization ---
    void OnDrawGizmosSelected() {
        MountPointDefinition currentDef = this.Definition; // Use property to ensure it's resolved
        if (currentDef != null) {
            Gizmos.color = currentDef.gizmoColor;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireSphere(Vector3.zero, currentDef.gizmoRadius);
            Gizmos.DrawRay(Vector3.zero, Vector3.forward * currentDef.gizmoRadius * 2f); // Show orientation Z+
            Gizmos.color = Color.red; // X axis
            Gizmos.DrawRay(Vector3.zero, Vector3.right * currentDef.gizmoRadius * 1.5f);
             Gizmos.color = Color.green; // Y axis
            Gizmos.DrawRay(Vector3.zero, Vector3.up * currentDef.gizmoRadius * 1.5f);
        } else {
            // Draw small red sphere if definition is missing/invalid
            Gizmos.color = Color.red;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireSphere(Vector3.zero, 0.03f);
        }
    }
}