// Assets/Logic/Items/Behaviours/Hands/HandsBehavior.cs
using UnityEngine;
using System.Collections;

/// <summary>
/// EquippableBehavior representing the player's empty hands.
/// Handles grabbing, holding, rotating, attaching/detaching IGrabbable items (especially PartInstances),
/// storing held items into inventory, and pulling items from inventory.
/// Acts as the primary physical interaction tool.
/// </summary>
public class HandsBehavior : EquippableBehavior { // Inherits EquippableBehavior

    [Header("Interaction Settings")]
    [Tooltip("Maximum distance the player can reach to grab/interact with objects.")]
    [SerializeField] private float interactionReach = 3.0f;
    [Tooltip("Layer containing MountPoint colliders for attaching parts.")]
    [SerializeField] private LayerMask mountPointLayerMask = 0;
    [Tooltip("Layer containing loose IGrabbable objects (WorldItem, loose PartInstance).")]
    [SerializeField] private LayerMask grabbableLayerMask = 0;
    private LayerMask _combinedInteractableMask; // Used for initial grab/detach raycast

    [Header("Item/Part Manipulation")]
    [Tooltip("Default distance to hold items in front of the camera.")]
    [SerializeField] private float itemHoldDistanceDefault = 1.5f;
    [Tooltip("Minimum distance an item can be held (using scroll wheel).")]
    [SerializeField] private float itemHoldDistanceMin = 0.75f;
    [Tooltip("Maximum distance an item can be held (using scroll wheel).")]
    [SerializeField] private float itemHoldDistanceMax = 4.0f;
    [Tooltip("Speed at which held items rotate with mouse movement (when allowed).")]
    [SerializeField] private float itemRotationSpeed = 180f;
    [Tooltip("Sensitivity of the scroll wheel for changing hold distance.")]
    [SerializeField] private float itemScrollSpeed = 0.3f;
    [Tooltip("Smoothness factor for moving the held item towards its target position.")]
    [SerializeField] private float itemMoveLerpSpeed = 20f;

    // --- Private State ---
    private IGrabbable _heldGrabbable;          // Currently held item
    private Rigidbody _heldRigidbody;           // Rigidbody of the held item
    private Transform _heldTransform;           // Transform of the held item
    private float _currentItemHoldDistance;     // Current distance item is held at
    private bool _allowHeldItemRotation = false; // Can the player rotate the item?

    // --- Dependencies ---
    private InteractionController _interactionController; // Detects look targets, handles highlighting
    private PlayerInventory _playerInventoryInternal; // Specific inventory reference for storing/pulling

    // --- Initialization ---
    /// <summary>
    /// Initializes HandsBehavior, getting necessary references and setting up layers.
    /// </summary>
    public override void Initialize(InventoryItem itemInstance, IEquipmentHolder holder, IAimProvider aimProvider) {
        // Call base Initialize first (sets ownerEquipmentHolder, ownerAimProvider)
        base.Initialize(itemInstance, holder, aimProvider);

        // Attempt to get specific PlayerInventory reference
        _playerInventoryInternal = this.ownerEquipmentHolder as PlayerInventory;
        if (_playerInventoryInternal == null && this.ownerEquipmentHolder != null) {
             Debug.LogWarning($"[HandsBehavior Initialize] Holder is not PlayerInventory ({this.ownerEquipmentHolder.GetType().Name}). Storing/Pulling might not work.", this.ownerEquipmentHolder as MonoBehaviour);
        } else if (this.ownerEquipmentHolder == null) {
             // Hands are the fallback, but *player* hands should always have a holder context
             Debug.LogError($"[HandsBehavior Initialize] Initialized with a NULL IEquipmentHolder. Critical error for player hands.", this);
        }

        // Find InteractionController
        _interactionController = GetComponentInParent<InteractionController>(); // Search hierarchy
        if (_interactionController == null) {
             Debug.LogError($"[HandsBehavior Initialize] InteractionController component not found in parent hierarchy! Highlighting/Mounting will fail.", this);
             // Consider disabling if InteractionController is critical: this.enabled = false; return;
        }

        // Combine masks for initial detection raycast
        _combinedInteractableMask = mountPointLayerMask.value | grabbableLayerMask.value;
        if (_combinedInteractableMask.value == 0) {
             Debug.LogWarning($"[HandsBehavior Initialize] No interaction layer masks (MountPoint or Grabbable) assigned in Inspector!", this);
        }

        ResetHandState(true); // Ensure clean start
    }

    // --- State Management ---
    /// <summary>
    /// Resets the held item state, optionally forcing a drop.
    /// </summary>
    private void ResetHandState(bool forcedDrop) {
        if (_heldGrabbable != null) {
             DropHeldItemInternal(forcedDrop, Vector3.zero); // Drop with no velocity if forced reset
        }
        _allowHeldItemRotation = false;
    }

    protected override void OnEnable() {
        base.OnEnable();
        // Reset state when enabled, ensuring contexts are likely set
        if (this.ownerEquipmentHolder != null && this.ownerAimProvider != null) {
            ResetHandState(true); // Force reset on enable
        }
        // else: Initialize will handle the first ResetHandState
    }

    protected override void OnDisable() {
        base.OnDisable();
        // Force drop anything held when hands are disabled/unequipped
        if (_heldGrabbable != null) {
             DropHeldItemInternal(true, Vector3.zero);
        }
        _interactionController?.ClearMountHighlight(); // Attempt to clear highlight
    }

    // --- Updates ---
    /// <summary>
    /// Handles physics-based movement of the held item in FixedUpdate.
    /// </summary>
    void FixedUpdate() {
        // Smoothly move the held object towards the target hold position
        if (_heldGrabbable != null && _heldRigidbody != null && _heldRigidbody.isKinematic) {
            MoveHeldItemPhysics();
        }
    }

    /// <summary>
    /// Handles input for item manipulation (rotation, distance) and mount point highlighting in Update.
    /// </summary>
    void Update() {
        if (_heldGrabbable != null) {
            // Handle scroll wheel distance and rotation input
            HandleHeldItemManipulationInput();

            // Update mount point highlighting if holding a PartInstance
            if (_interactionController != null) {
                if (_heldGrabbable is PartInstance heldPart) {
                    _interactionController.UpdateMountPointHighlight(heldPart);
                } else {
                    // Clear highlight if holding something generic (not a PartInstance)
                    _interactionController.ClearMountHighlight();
                }
            }
        } else {
            // Ensure highlight is cleared if nothing is held
             _interactionController?.ClearMountHighlight();
        }
    }

    // --- Input Handlers ---
    /// <summary>
    /// Handles Fire1 press: Grab/Detach world object or Attach held part.
    /// </summary>
    public override void OnFire1Down() {
        if (ownerAimProvider == null) { Debug.LogError("[HandsBehavior] Missing Aim Provider!", this); return; }

        if (_heldGrabbable != null) {
            // If holding a part, try to attach it
            if (_heldGrabbable is PartInstance heldPart) {
                 TryAttachHeldPart(heldPart);
            }
            // If holding something else (WorldItem), LMB might do nothing or trigger 'use'? (Not implemented)
        } else {
            // If hands empty, try to grab or detach something from the world
            TryGrabOrDetachWorldObject();
        }
     }

    /// <summary>
    /// Handles Fire2 press: Drop the currently held item.
    /// </summary>
    public override void OnFire2Down() {
         if (_heldGrabbable != null) {
             Vector3 dropVelocity = ownerAimProvider != null ? ownerAimProvider.GetLookRay().direction * 2f : Vector3.zero; // Add small forward velocity
             DropHeldItemInternal(false, dropVelocity); // Normal drop
         }
     }

    /// <summary>
    /// Handles Utility press: Toggle rotation lock for held item.
    /// </summary>
    public override void OnUtilityDown() {
         if (_heldGrabbable != null) {
              _allowHeldItemRotation = !_allowHeldItemRotation;
              // Optional: Provide feedback (sound, UI message) about rotation lock state
              // Debug.Log($"Held item rotation allowed: {_allowHeldItemRotation}");
         }
     }

    /// <summary>
    /// Handles Store press: Store held item or Pull item from inventory.
    /// </summary>
    public override void OnStoreDown() {
       if (_heldGrabbable != null) {
           TryStoreHeldItem(); // Store the held item
       } else {
           TryPullItem(); // Pull item from selected slot
       }
    }

    // --- Core Actions: Grab / Attach / Store / Pull ---

    /// <summary>
    /// Raycasts to find a grabbable item or an installed part to detach.
    /// </summary>
    private void TryGrabOrDetachWorldObject() {
        if (ownerAimProvider == null || _combinedInteractableMask.value == 0) return;

        Ray lookRay = ownerAimProvider.GetLookRay();
        if (Physics.Raycast(lookRay, out RaycastHit hit, interactionReach, _combinedInteractableMask, QueryTriggerInteraction.Collide)) {

            // Priority 1: Check if hitting a MountPoint with an installed part (for detaching)
            MountPoint mountPoint = hit.collider.GetComponentInParent<MountPoint>();
            if (mountPoint != null && mountPoint.CurrentlyAttachedPart != null && (mountPointLayerMask.value & (1 << hit.collider.gameObject.layer)) > 0) {
                PartInstance partToDetach = mountPoint.Detach();
                if (partToDetach != null) {
                     GrabGrabbable(partToDetach, hit.distance); // Grab the detached part
                     return;
                }
            }

            // Priority 2: Check if hitting a loose IGrabbable
            IGrabbable grabbable = hit.collider.GetComponentInParent<IGrabbable>();
            if (grabbable != null && (grabbableLayerMask.value & (1 << hit.collider.gameObject.layer)) > 0 ) {
                if (grabbable.CanGrab()) { // Check if it *can* be grabbed (e.g., not attached)
                     GrabGrabbable(grabbable, hit.distance);
                     return;
                }
            }
        }
    }

    /// <summary>
    /// Initiates grabbing of a specific IGrabbable item.
    /// </summary>
    private void GrabGrabbable(IGrabbable grabbable, float hitDistance) {
        if (grabbable == null || grabbable.GetTransform() == null) { Debug.LogError("[HandsBehavior Grab] Invalid grabbable or transform!", grabbable as MonoBehaviour); return; }
        // Ensure InventoryItem data is valid *before* grabbing
        if (grabbable.GetInventoryItemData() == null || grabbable.GetInventoryItemData().data == null) { Debug.LogError($"[HandsBehavior Grab] Grabbable '{grabbable.GetTransform().name}' has missing ItemInstanceData or ItemData!", grabbable as MonoBehaviour); return; }

        if (_heldGrabbable != null) DropHeldItemInternal(true, Vector3.zero); // Force drop previous

        _heldGrabbable = grabbable;
        _heldTransform = _heldGrabbable.GetTransform();
        _heldRigidbody = _heldTransform.GetComponent<Rigidbody>(); // Can be null

        // Notify the item it's being grabbed (disables physics, etc.)
        _heldGrabbable.OnGrabbed(null); // Pass null for physics gun style grabber transform

        // Set initial hold distance, clamped
        _currentItemHoldDistance = Mathf.Clamp(hitDistance, itemHoldDistanceMin, itemHoldDistanceMax);
        _allowHeldItemRotation = false; // Start with rotation locked
         // Debug.Log($"[HandsBehavior] Grabbed: {_heldTransform.name}", _heldTransform);
    }

    /// <summary>
    /// Attempts to attach the currently held PartInstance to the MountPoint being looked at.
    /// </summary>
    private void TryAttachHeldPart(PartInstance heldPart) {
        if (heldPart == null || ownerAimProvider == null || _interactionController == null) return;

        InteractionController.InteractableInfo lookTarget = _interactionController.CurrentLookTargetInfo;

        // Use the properties from InteractableInfo
        if (lookTarget.HasTarget && lookTarget.Mount != null) {
            MountPoint targetMountPoint = lookTarget.Mount;

            if (targetMountPoint.IsCompatible(heldPart)) {
                Vector3 cachedVelocity = _heldRigidbody ? _heldRigidbody.linearVelocity : Vector3.zero; // Cache pre-drop velocity
                // Logically drop the item *before* attempting the attach.
                // OnDropped resets physics, which might be needed by TryAttach depending on its implementation.
                _heldGrabbable.OnDropped(Vector3.zero);

                // Attempt the attach via the MountPoint
                if (targetMountPoint.TryAttach(heldPart)) {
                    // Success! Clear the reference in HandsBehavior.
                    ClearHeldItemInternal();
                } else {
                    // Attach failed at the MountPoint level. Re-grab the item.
                    Debug.LogWarning($"[HandsBehavior Attach] MountPoint.TryAttach failed. Re-grabbing {heldPart.name}.", this);
                    GrabGrabbable(heldPart, _currentItemHoldDistance);
                    // Optionally restore velocity if Rigidbody exists
                    if (_heldRigidbody) _heldRigidbody.linearVelocity = cachedVelocity;
                }
            } // else: Not compatible, do nothing (optional feedback?)
        } // else: Not looking at a valid mount point
    }

    /// <summary>
    /// Attempts to store the currently held item into the player's inventory.
    /// </summary>
     private void TryStoreHeldItem() {
        IGrabbable grabbableToStore = _heldGrabbable; // Cache reference
        PlayerInventory inventoryRef = _playerInventoryInternal;

        if (grabbableToStore == null || inventoryRef == null) { Debug.LogError($"[HandsBehavior Store] Cannot store: Held item or PlayerInventory ref is null.", this); return; }

        InventoryItem itemToStore = grabbableToStore.GetInventoryItemData(); // Calls UpdateInventoryItemRuntimeState inside
        if (itemToStore == null || itemToStore.data == null) { Debug.LogError("[HandsBehavior Store] Item data is null.", this); return; }
        if (itemToStore.data.isBulky) { /* Inform player? */ return; }

        GameObject gameObjectToDestroy = grabbableToStore.GetTransform()?.gameObject; // Cache GameObject reference
        if (gameObjectToDestroy == null) { Debug.LogError($"[HandsBehavior Store] Cannot store item '{itemToStore.data.itemName}': Its GameObject is null!", grabbableToStore as MonoBehaviour); return; }
        if (inventoryRef.Container == null) { Debug.LogError("[HandsBehavior Store] PlayerInventory.Container is NULL.", inventoryRef); return; }

        // --- Attempt Storage ---
        int currentToolbarIndex = inventoryRef.GetSelectedToolbarIndex();
        bool storedSuccessfully = false;
        if (currentToolbarIndex != -1 && currentToolbarIndex < inventoryRef.Container.Size) {
             InventorySlot targetToolbarSlot = inventoryRef.GetSlotAt(currentToolbarIndex);
             if (targetToolbarSlot != null && targetToolbarSlot.IsEmpty()) { if (inventoryRef.TryStoreItemInSpecificSlot(itemToStore, currentToolbarIndex)) { storedSuccessfully = true; } }
        }
        if (!storedSuccessfully) { if (inventoryRef.RequestAddItemToInventory(itemToStore)) { storedSuccessfully = true; } }
        // --- End Storage Attempt ---

        if (storedSuccessfully) {
            try { grabbableToStore.OnStored(); } // Call BEFORE clearing refs/destroying
            catch (System.Exception e) { Debug.LogError($"Error in {grabbableToStore.GetType().Name}.OnStored(): {e.Message}", grabbableToStore as MonoBehaviour); }

            // Clear held item reference *only if* it hasn't been changed by an interrupt
            if (this._heldGrabbable == grabbableToStore) { ClearHeldItemInternal(); }

            Destroy(gameObjectToDestroy); // Destroy the world object
        } else {
             Debug.LogWarning($"[HandsBehavior Store] Failed to store '{itemToStore.data.itemName}' (Inventory full?). Item remains held.", this);
        }
     }

    /// <summary>
    /// Attempts to pull the currently selected toolbar item out into the world and grab it.
    /// </summary>
    private void TryPullItem() {
        if (_playerInventoryInternal == null || ownerAimProvider == null) { Debug.LogError("[HandsBehavior Pull] Missing PlayerInventory or AimProvider.", this); return; }

        int selectedIndex = _playerInventoryInternal.GetSelectedToolbarIndex();
        if (selectedIndex < 0) return; // No slot selected

        // Attempt to remove item from inventory data structure FIRST
        if (_playerInventoryInternal.TryPullItemFromSlot(selectedIndex, out InventoryItem pulledItem)) {
            // Inventory removal success, now spawn and grab
            if (pulledItem == null || pulledItem.data == null || pulledItem.data.worldPrefab == null) { Debug.LogError($"[HandsBehavior Pull] Pulled item invalid!", this); return; } // Error check

            // Instantiate prefab
            Ray lookRay = ownerAimProvider.GetLookRay();
            Vector3 spawnPos = lookRay.GetPoint(itemHoldDistanceDefault * 0.7f); // Spawn closer
            Quaternion spawnRot = Quaternion.LookRotation(lookRay.direction);
            GameObject spawnedGO = Instantiate(pulledItem.data.worldPrefab, spawnPos, spawnRot);
            if (spawnedGO == null) { Debug.LogError($"[HandsBehavior Pull] Failed Instantiate prefab!", this); return; } // Error check

            // Get Grabbable component
            IGrabbable grabbable = spawnedGO.GetComponentInChildren<IGrabbable>();
            if (grabbable == null) { Debug.LogError($"[HandsBehavior Pull] Prefab missing IGrabbable!", spawnedGO); Destroy(spawnedGO); return; }

            // Initialize the NEW instance with the data/state from the InventoryItem
            if (grabbable is ItemInstance itemInst) { // Check if it inherits ItemInstance (preferred)
                 itemInst.Initialize(pulledItem);
            } else {
                 Debug.LogWarning($"[HandsBehavior Pull] Spawned IGrabbable '{spawnedGO.name}' does not inherit ItemInstance. Standard initialization might be needed.", spawnedGO);
                 // Add specific initialization logic for non-ItemInstance grabbables if any
            }

            // Immediately grab the spawned item
            GrabGrabbable(grabbable, itemHoldDistanceDefault * 0.7f); // Grab slightly closer

        } // else: Failed to pull from inventory (e.g., slot was empty), do nothing.
    }

    // --- Internal State Cleanup & Physics ---
    /// <summary>
    /// Clears held item references and tells the item it was dropped.
    /// </summary>
    private void DropHeldItemInternal(bool wasForced, Vector3 dropVelocity) {
        if (_heldGrabbable == null) return;
        IGrabbable itemToDrop = _heldGrabbable; // Cache before clearing
        ClearHeldItemInternal(); // Clear references now
        itemToDrop.OnDropped(dropVelocity); // Notify item AFTER clearing internal state
    }

    /// <summary>
    /// Resets internal variables related to holding an item.
    /// </summary>
    private void ClearHeldItemInternal() {
        _interactionController?.ClearMountHighlight(); // Clear highlight safely
        _heldGrabbable = null;
        _heldTransform = null;
        _heldRigidbody = null;
        // Keep _currentItemHoldDistance as is? Or reset to default? Resetting seems safer.
        _currentItemHoldDistance = itemHoldDistanceDefault;
        _allowHeldItemRotation = false; // Reset rotation lock
    }

    // --- Input & Physics Helpers ---
    /// <summary>
    /// Handles mouse scroll for distance and Shift+Mouse for rotation.
    /// </summary>
    private void HandleHeldItemManipulationInput() {
        // Scroll wheel for distance
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.01f) {
            _currentItemHoldDistance = Mathf.Clamp(_currentItemHoldDistance - scroll * itemScrollSpeed, itemHoldDistanceMin, itemHoldDistanceMax);
        }

        // Rotation (example: Left Shift + Mouse)
        if (_allowHeldItemRotation && Input.GetKey(KeyCode.LeftShift)) {
            if (_heldRigidbody != null && _heldRigidbody.isKinematic && _heldTransform != null && ownerAimProvider != null) {
                float mouseX = Input.GetAxis("Mouse X") * itemRotationSpeed * Time.deltaTime;
                float mouseY = Input.GetAxis("Mouse Y") * itemRotationSpeed * Time.deltaTime;
                Transform cameraTransform = ownerAimProvider.GetLookTransform();
                if (cameraTransform != null) {
                    // Rotate around world axes for intuitive control
                    _heldTransform.Rotate(Vector3.up, -mouseX, Space.World); // Yaw
                    _heldTransform.Rotate(cameraTransform.right, mouseY, Space.World); // Pitch relative to camera right
                }
            }
        }
    }

    /// <summary>
    /// Smoothly moves the held item's Rigidbody to the target position.
    /// </summary>
    private void MoveHeldItemPhysics() {
        if (_heldGrabbable == null || _heldRigidbody == null || !_heldRigidbody.isKinematic || ownerAimProvider == null) return;
        Ray lookRay = ownerAimProvider.GetLookRay();
        Vector3 targetPosition = lookRay.GetPoint(_currentItemHoldDistance);
        // Use MovePosition for smoother physics interaction
        _heldRigidbody.MovePosition(Vector3.Lerp(_heldRigidbody.position, targetPosition, Time.fixedDeltaTime * itemMoveLerpSpeed));
        // Optionally use MoveRotation to align object? For now, only position.
        // _heldRigidbody.MoveRotation(Quaternion.Slerp(_heldRigidbody.rotation, targetRotation, Time.fixedDeltaTime * itemMoveLerpSpeed));
    }

    // --- Unused Base Input Methods ---
    public override void OnFire1Hold() {}
    public override void OnFire1Up() {}
    public override void OnFire2Hold() {}
    public override void OnFire2Up() {}
    public override void OnUtilityUp() {} // UtilityDown toggles rotation lock
    public override void OnReloadDown() {} // Reload does nothing for hands

}
