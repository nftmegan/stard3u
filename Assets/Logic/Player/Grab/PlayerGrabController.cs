// --- Start of script: Assets/Logic/Player/Grab/PlayerGrabController.cs ---
// --- Start of script: Assets/Logic/Player/PlayerGrabController.cs ---
using UnityEngine;
using System.Collections;

/// <summary>
/// Manages the player's ability to grab, hold, manipulate, store, and pull IGrabbable items
/// using direct kinematic control.
/// </summary>
public class PlayerGrabController : MonoBehaviour
{
    #region Inspector Fields

    [Header("Interaction Settings")]
    [SerializeField] private float interactionReach = 3.0f;
    [SerializeField] private LayerMask mountPointLayerMask = 0;
    [SerializeField] private LayerMask grabbableLayerMask = 0;
    [Tooltip("Layers the held object's target position check should collide with (Environment, etc.). Used for collision pullback.")]
    [SerializeField] private LayerMask grabTargetCollisionLayers = ~0;
    private LayerMask _combinedInteractableMask;

    [Header("Grab Manipulation Settings")]
    [Tooltip("How smoothly the held object follows the target position.")]
    [SerializeField] private float itemFollowSpeed = 20f;
    [Tooltip("How quickly the object rotates when actively controlled by the player with middle mouse.")]
    [SerializeField] private float itemManualRotationSpeed = 200f; // Degrees per second scale

    [Header("Item Distance Control")]
    [SerializeField] private float itemHoldDistanceDefault = 1.5f;
    [SerializeField] private float itemHoldDistanceMin = 0.75f;
    [SerializeField] private float itemHoldDistanceMax = 4.0f;
    [SerializeField] private float itemScrollSpeed = 0.3f;
    [Tooltip("Offset from the hit point when an object is pulled back due to collision with environment.")]
    [SerializeField] private float collisionPullbackOffset = 0.1f;

    #endregion

    #region Private State
    private IGrabbable _grabbedItem;
    private Rigidbody _grabbedRigidbody;
    private Transform _grabbedTransform;
    private Collider[] _grabbedColliders;
    private bool _originalRigidbodyIsKinematic;
    private bool _originalRigidbodyUseGravity;
    private float _currentTargetHoldDistance; 
    private bool _isActivelyRotating = false;

    // Dependencies
    private PlayerManager _playerManager;
    private PlayerInventory _playerInventory;
    private IAimProvider _aimProvider;
    private InteractionController _interactionController;
    private Collider _playerCollider;
    #endregion

    #region Public Properties
    public bool IsGrabbing => _grabbedItem != null;
    public IGrabbable CurrentGrabbedItem => _grabbedItem;
    #endregion

    #region Events
    public event System.Action<bool, IGrabbable> OnGrabStateChanged;
    #endregion

    #region Initialization
    void Awake() { /* No specific logic here */ }

    public void InitializeController(PlayerManager manager)
    {
        _playerManager = manager;
        if (_playerManager == null) { Debug.LogError("[PGC Init] PlayerManager null!", this); enabled = false; return; }
        _playerInventory = _playerManager.Inventory;
        _aimProvider = _playerManager.Look;
        _interactionController = FindFirstObjectByType<InteractionController>();

        if (_playerManager.CharacterController != null)
            _playerCollider = _playerManager.CharacterController.GetComponent<Collider>();
        else Debug.LogError("[PGC Init] CharacterController missing on PM!", this);

        bool depsMissing = _playerInventory == null || _aimProvider == null;
        if (depsMissing) { Debug.LogError("[PGC Init] Critical dependencies missing! Disabling.", this); enabled = false; return; }
        
        if (_interactionController == null) Debug.LogWarning("[PGC Init] InteractionController not found, mount point highlighting might not work.", this);
        if (_playerCollider == null) Debug.LogWarning("[PGC Init] PlayerCollider not found, ignoring collisions with player might not work.", this);

        _combinedInteractableMask = mountPointLayerMask.value | grabbableLayerMask.value;
        if (_combinedInteractableMask.value == 0) Debug.LogWarning("[PGC Init] Interaction masks not set.", this);
        _currentTargetHoldDistance = itemHoldDistanceDefault;
        ResetGrabState(true); // True to force drop if anything was somehow grabbed pre-init
    }

    private void ResetGrabState(bool forcedDrop)
    {
        if (IsGrabbing) DropGrabbedItemInternal(forcedDrop, Vector3.zero);
        _isActivelyRotating = false;
        _currentTargetHoldDistance = itemHoldDistanceDefault;
    }
    #endregion

    #region Unity Updates
    void FixedUpdate()
    {
        if (IsGrabbing && _grabbedRigidbody != null && _grabbedRigidbody.isKinematic)
        {
            MoveGrabbedItemKinematic();
        }
    }
    void Update()
    {
        if (IsGrabbing) UpdateHighlighting();
        else _interactionController?.ClearMountHighlight();
    }
    #endregion

    #region Public Action Methods
    public void HandleStoreAction() { if (IsGrabbing) TryStoreGrabbedItem(); else TryPullItemFromInventoryAndGrab(); }

    public bool TryGrabOrDetachWorldObject()
    {
        if (_aimProvider == null || _combinedInteractableMask.value == 0) return false;
        Ray lookRay = _aimProvider.GetLookRay();
        if (Physics.Raycast(lookRay, out RaycastHit hit, interactionReach, _combinedInteractableMask, QueryTriggerInteraction.Collide))
        {
            MountPoint mountPoint = hit.collider.GetComponentInParent<MountPoint>();
            if (mountPoint?.CurrentlyAttachedPart != null && (mountPointLayerMask.value & (1 << hit.collider.gameObject.layer)) > 0)
            {
                PartInstance partToDetach = mountPoint.Detach();
                if (partToDetach != null) { StartGrabbing(partToDetach, hit.distance); return true; }
            }
            IGrabbable grabbable = hit.collider.GetComponentInParent<IGrabbable>();
            if (grabbable != null && (grabbableLayerMask.value & (1 << hit.collider.gameObject.layer)) > 0 && grabbable.CanGrab())
            { StartGrabbing(grabbable, hit.distance); return true; }
        }
        return false;
    }

    public bool TryAttachGrabbedPart()
    {
        if (!IsGrabbing || !(_grabbedItem is PartInstance heldPart) || _aimProvider == null || _interactionController == null) return false;
        InteractionController.InteractableInfo lookTarget = _interactionController.CurrentLookTargetInfo;
        if (lookTarget.HasTarget && lookTarget.Mount != null)
        {
            MountPoint targetMountPoint = lookTarget.Mount;
            if (targetMountPoint.IsCompatible(heldPart))
            {
                DropGrabbedItemInternal(true, Vector3.zero); // Forced drop for attachment
                if (targetMountPoint.TryAttach(heldPart)) return true;
                else
                {
                    Debug.LogWarning($"[PGC Attach] MountPoint.TryAttach failed. Re-grabbing {heldPart.name}.", this);
                    StartGrabbing(heldPart, _currentTargetHoldDistance); 
                    return false;
                }
            }
        }
        return false;
    }

    public void DropGrabbedItemWithLMB()
    {
        if (!IsGrabbing) return;
        Vector3 dropVelocity = _aimProvider != null ? _aimProvider.GetLookRay().direction * 3f : Vector3.zero;
        DropGrabbedItemInternal(false, dropVelocity); // wasForced = false, so it becomes non-kinematic
    }

    public void AdjustGrabbedItemDistance(float scrollDelta)
    {
        if (IsGrabbing && Mathf.Abs(scrollDelta) > 0.01f)
            _currentTargetHoldDistance = Mathf.Clamp(_currentTargetHoldDistance + scrollDelta * itemScrollSpeed, itemHoldDistanceMin, itemHoldDistanceMax);
    }

    public void StartGrabRotation() { if (IsGrabbing) _isActivelyRotating = true; }
    public void EndGrabRotation() { _isActivelyRotating = false; }

    public void ApplyGrabbedItemRotationInput(Vector2 rotateDelta)
    {
        if (!IsGrabbing || !_isActivelyRotating || _grabbedTransform == null || _aimProvider == null) return;
        Transform cameraTransform = _aimProvider.GetLookTransform();
        if (cameraTransform != null)
        {
            float mouseX = rotateDelta.x * itemManualRotationSpeed * Time.deltaTime;
            float mouseY = rotateDelta.y * itemManualRotationSpeed * Time.deltaTime;
            _grabbedTransform.Rotate(Vector3.up, -mouseX, Space.World);
            _grabbedTransform.Rotate(cameraTransform.right, mouseY, Space.World);
        }
    }
    #endregion

    #region Internal Grab/Drop/Store/Pull Logic
    private void StartGrabbing(IGrabbable grabbable, float hitDistance)
    {
        if (grabbable?.GetTransform() == null || grabbable.GetInventoryItemData()?.data == null) { Debug.LogError("[PGC StartGrab] Invalid input for new grabbable item.", grabbable as MonoBehaviour); return; }
        
        if (IsGrabbing) {
            Vector3 previousItemDropVelocity = Vector3.zero;
            if (_aimProvider != null) {
                previousItemDropVelocity = _aimProvider.GetLookRay().direction * 2f;
            }
            DropGrabbedItemInternal(false, previousItemDropVelocity); 
        }

        _grabbedItem = grabbable;
        _grabbedTransform = _grabbedItem.GetTransform();
        _grabbedRigidbody = _grabbedTransform.GetComponent<Rigidbody>();
        _grabbedColliders = _grabbedTransform.GetComponentsInChildren<Collider>(true);

        if (_grabbedRigidbody == null) {
            Debug.LogWarning($"[PGC StartGrab] Grabbable '{_grabbedTransform.name}' is missing a Rigidbody. Adding one.", _grabbedTransform);
            _grabbedRigidbody = _grabbedTransform.gameObject.AddComponent<Rigidbody>();
            _grabbedRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            _grabbedRigidbody.mass = 1f; 
            _originalRigidbodyIsKinematic = false; 
            _originalRigidbodyUseGravity = true;
        } else {
            _originalRigidbodyIsKinematic = _grabbedRigidbody.isKinematic;
            _originalRigidbodyUseGravity = _grabbedRigidbody.useGravity;
        }

        _grabbedRigidbody.isKinematic = true; 
        _grabbedRigidbody.useGravity = false;

        if (_playerCollider != null && _grabbedColliders != null)
            foreach (var grabCol in _grabbedColliders) if (grabCol != null) Physics.IgnoreCollision(_playerCollider, grabCol, true);

        _isActivelyRotating = false;
        _currentTargetHoldDistance = Mathf.Clamp(hitDistance, itemHoldDistanceMin, itemHoldDistanceMax);

        _grabbedItem.OnGrabbed(_aimProvider?.GetLookTransform());
        OnGrabStateChanged?.Invoke(true, _grabbedItem);
    }

    private void DropGrabbedItemInternal(bool wasForced, Vector3 dropVelocity)
    {
        if (!IsGrabbing) return;
        IGrabbable itemToDrop = _grabbedItem; 

        if (_grabbedRigidbody != null)
        {
            bool targetIsKinematic;
            bool targetUseGravity;

            if (!wasForced) 
            {
                targetIsKinematic = false;
                targetUseGravity = true;
            }
            else 
            {
                targetIsKinematic = _originalRigidbodyIsKinematic;
                targetUseGravity = _originalRigidbodyUseGravity;
            }

            _grabbedRigidbody.isKinematic = targetIsKinematic;
            _grabbedRigidbody.useGravity = targetUseGravity;

            // Apply velocities ONLY if the INTENDED state is non-kinematic
            if (!targetIsKinematic) // <<< MODIFIED THIS CONDITION
            {
                _grabbedRigidbody.linearVelocity = dropVelocity;
                _grabbedRigidbody.angularVelocity = Random.insideUnitSphere * 1.5f;
            }
        }

        if (_playerCollider != null && _grabbedColliders != null)
        {
            foreach (var grabCol in _grabbedColliders)
            {
                if (grabCol != null) Physics.IgnoreCollision(_playerCollider, grabCol, false);
            }
        }
        
        ClearControllerGrabState(); 
        itemToDrop?.OnDropped(dropVelocity);
    }

    private void ClearControllerGrabState()
    {
        if (!IsGrabbing) return; 
        IGrabbable previouslyGrabbed = _grabbedItem;
        _interactionController?.ClearMountHighlight();
        _grabbedItem = null; 
        _grabbedTransform = null; 
        _grabbedRigidbody = null; 
        _grabbedColliders = null;
        _currentTargetHoldDistance = itemHoldDistanceDefault;
        _isActivelyRotating = false;
        OnGrabStateChanged?.Invoke(false, previouslyGrabbed);
    }
    private void ClearGrabbedItemInternal() { ClearControllerGrabState(); }

    private bool TryStoreGrabbedItem()
    {
        if (!IsGrabbing || _playerInventory == null) return false;
        IGrabbable grabbableToStore = _grabbedItem;
        InventoryItem itemToStore = grabbableToStore.GetInventoryItemData();

        if (itemToStore?.data == null) { Debug.LogWarning("[PGC Store] Grabbed item has null ItemData.", this); return false; }
        if (itemToStore.data.isBulky) { Debug.Log("[PGC Store] Cannot store bulky item in inventory this way.", this); return false; }

        GameObject gameObjectToDestroy = grabbableToStore.GetTransform()?.gameObject;
        if (gameObjectToDestroy == null) { Debug.LogError("[PGC Store] Grabbed item's transform/GameObject is null!", this); return false; }

        bool storedSuccessfully = false;
        int currentToolbarIndex = _playerInventory.GetSelectedToolbarIndex();

        if (currentToolbarIndex != -1 && currentToolbarIndex < _playerInventory.Container.Size) {
            InventorySlot targetToolbarSlot = _playerInventory.GetSlotAt(currentToolbarIndex);
            if (targetToolbarSlot?.IsEmpty() == true && _playerInventory.TryStoreItemInSpecificSlot(itemToStore, currentToolbarIndex)) {
                storedSuccessfully = true;
            }
        }
        if (!storedSuccessfully) {
            if (_playerInventory.RequestAddItemToInventory(itemToStore)) {
                storedSuccessfully = true;
            }
        }

        if (storedSuccessfully) {
            IGrabbable itemBeingStored = _grabbedItem; 
            ClearGrabbedItemInternal(); 

            try { itemBeingStored?.OnStored(); } 
            catch (System.Exception e) { Debug.LogError($"Error in OnStored for {itemBeingStored?.GetTransform()?.name}: {e.Message}", itemBeingStored as MonoBehaviour); }
            
            if (gameObjectToDestroy != null) Destroy(gameObjectToDestroy); 
            return true;
        }
        Debug.Log($"[PGC Store] Failed to store '{itemToStore.data.itemName}'. Inventory might be full.", this);
        return false;
    }

    private bool TryPullItemFromInventoryAndGrab()
    {
        if (_playerInventory == null || _aimProvider == null || IsGrabbing) return false;
        int selectedIndex = _playerInventory.GetSelectedToolbarIndex();
        if (selectedIndex < 0) return false; 

        if (_playerInventory.TryPullItemFromSlot(selectedIndex, out InventoryItem pulledItem))
        {
            if (pulledItem?.data?.worldPrefab == null) { Debug.LogError("[PGC Pull] Pulled item has invalid data or no worldPrefab.", this); return false; }
            
            Ray lookRay = _aimProvider.GetLookRay();
            Vector3 spawnPos = lookRay.GetPoint(itemHoldDistanceDefault * 0.7f); 
            Quaternion spawnRot = Quaternion.LookRotation(lookRay.direction); 

            GameObject spawnedGO = Instantiate(pulledItem.data.worldPrefab, spawnPos, spawnRot);
            if (spawnedGO == null) { Debug.LogError("[PGC Pull] Failed to instantiate worldPrefab.", this); return false; }

            IGrabbable grabbable = spawnedGO.GetComponentInChildren<IGrabbable>(); 
            if (grabbable == null) { 
                Debug.LogError($"[PGC Pull] Spawned worldPrefab '{pulledItem.data.worldPrefab.name}' is missing an IGrabbable component.", spawnedGO);
                Destroy(spawnedGO); return false; 
            }
            
            if (grabbable is ItemInstance itemInst) { itemInst.Initialize(pulledItem); }
            else if (grabbable is WorldItem worldItem) { worldItem.Initialize(pulledItem); }
            else { Debug.LogWarning($"[PGC Pull] Unknown IGrabbable type: {grabbable.GetType()} on {spawnedGO.name}. State might not be fully initialized.", spawnedGO); }
            
            StartGrabbing(grabbable, itemHoldDistanceDefault * 0.7f); 
            return true;
        }
        return false;
    }
    #endregion

    #region Kinematic Movement & Rotation
    private void MoveGrabbedItemKinematic()
    {
        if (_grabbedRigidbody == null || _aimProvider == null || !_grabbedRigidbody.isKinematic) return;

        Transform camTransform = _aimProvider.GetLookTransform();
        Ray lookRay = _aimProvider.GetLookRay();

        Vector3 idealTargetPoint = lookRay.GetPoint(_currentTargetHoldDistance);
        Vector3 finalTargetPoint = idealTargetPoint;

        float objectRadius = 0.1f; 
        if (_grabbedColliders != null && _grabbedColliders.Length > 0 && _grabbedColliders[0] != null) {
            Bounds combinedBounds = _grabbedColliders[0].bounds; 
            for(int i = 1; i < _grabbedColliders.Length; i++) {
                if(_grabbedColliders[i] != null) combinedBounds.Encapsulate(_grabbedColliders[i].bounds);
            }
            objectRadius = combinedBounds.extents.magnitude * 0.5f; 
        }
        objectRadius = Mathf.Max(0.05f, objectRadius); 

        if (Physics.SphereCast(lookRay.origin, objectRadius, lookRay.direction, out RaycastHit hit, _currentTargetHoldDistance, grabTargetCollisionLayers, QueryTriggerInteraction.Ignore))
        {
            finalTargetPoint = hit.point + hit.normal * (objectRadius + collisionPullbackOffset);
            float distanceToCam = Vector3.Distance(lookRay.origin, finalTargetPoint);
            finalTargetPoint = lookRay.GetPoint(Mathf.Clamp(distanceToCam, itemHoldDistanceMin, _currentTargetHoldDistance));
        }

        Vector3 newPosition = Vector3.Lerp(_grabbedRigidbody.position, finalTargetPoint, Time.fixedDeltaTime * itemFollowSpeed);
        _grabbedRigidbody.MovePosition(newPosition);
    }
    #endregion

    #region Utility
     private void UpdateHighlighting() {
         if (_interactionController != null) {
             if (_grabbedItem is PartInstance heldPart) _interactionController.UpdateMountPointHighlight(heldPart);
             else _interactionController.ClearMountHighlight();
         }
     }
     #endregion
}
// --- End of script: Assets/Logic/Player/PlayerGrabController.cs ---
// --- End of script: Assets/Logic/Player/Grab/PlayerGrabController.cs ---