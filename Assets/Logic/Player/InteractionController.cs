using UnityEngine;
using UnityEngine.UI; // Uncomment if you add the Text prompt back

/// <summary>
/// Detects interactable objects (Parts, MountPoints, etc.) the player is looking at
/// and provides information about them. Also handles highlighting potential MountPoints
/// when requested by other systems (like HandsBehavior).
/// </summary>
public class InteractionController : MonoBehaviour {

    [Header("Detection Settings")]
    [Tooltip("Max distance to detect interactable objects.")]
    [SerializeField] private float detectionRange = 3.0f;
    [Tooltip("Layers containing physically interactable objects like PartInstances and MountPoints.")]
    [SerializeField] private LayerMask detectionLayerMask = 0; // Renamed for clarity
    [Tooltip("Color used to highlight compatible MountPoints.")]
    [SerializeField] private Color highlightColor = Color.green;

    [Header("References")]
    [Tooltip("The transform of the camera used for raycasting.")]
    [SerializeField] private Transform playerCameraTransform;
    // [Tooltip("Optional UI Text element to display interaction prompts.")]
    // [SerializeField] private Text interactionPromptText;

    // --- Runtime State ---
    private InteractableInfo _currentLookInfo; // Internal state
    private MountPoint _highlightedMountPoint;
    private Material _originalMountMaterial; // For restoring color
    private Renderer _highlightedRenderer;   // Cache renderer for efficiency

    /// <summary>
    /// Information about the object currently being looked at by the player.
    /// </summary>
    public InteractableInfo CurrentLookTargetInfo => _currentLookInfo; // Public read-only property

    /// <summary>
    /// Struct holding details about the detected interactable object.
    /// </summary>
    public struct InteractableInfo {
        public GameObject TargetObject { get; private set; }
        public PartInstance Part { get; private set; } // Renamed for clarity
        public MountPoint Mount { get; private set; } // Renamed for clarity
        public bool IsLoosePart { get; private set; }
        public bool IsInstalledPart { get; private set; }
        public bool HasTarget => TargetObject != null; // Helper property

        // Constructor to set values safely
        public InteractableInfo(GameObject target, PartInstance part, MountPoint mount) {
            TargetObject = target;
            Part = part;
            Mount = mount;
            IsLoosePart = part != null && part.OwningVehicle == null;
            // Installed if part exists, attached to vehicle, and is the part currently on the detected mount (or no mount detected)
            IsInstalledPart = part != null && part.OwningVehicle != null && (mount == null || mount.CurrentlyAttachedPart == part);
        }
    }

    private void Awake() {
        // Attempt to find camera if not assigned
        if (playerCameraTransform == null) {
            var mainCamera = Camera.main;
            if (mainCamera != null) {
                playerCameraTransform = mainCamera.transform;
            } else {
                Debug.LogError($"[{GetType().Name}] Player Camera Transform not found or assigned, and Camera.main is null!", this);
                enabled = false; // Cannot function without a camera
                return;
            }
        }
        // Validate detection layer mask
        if (detectionLayerMask.value == 0) {
             Debug.LogWarning($"[{GetType().Name} on {gameObject.name}] Detection Layer Mask is not set. Will likely not detect any parts/mounts.", this);
        }
        // if (interactionPromptText == null) Debug.LogWarning($"[{GetType().Name}] Interaction Prompt Text not assigned.", this);
        // else interactionPromptText.text = "";
    }

    private void Update() {
        if (playerCameraTransform == null) return; // Exit if camera is missing

        // Store previous target for comparison
        GameObject previousTarget = _currentLookInfo.TargetObject;

        // Detect what's currently being looked at
        DetectLookTarget();

        // If target changed, clear any existing highlight
        if (previousTarget != _currentLookInfo.TargetObject && _highlightedMountPoint != null) {
            ClearMountHighlight();
        }

        // Update UI prompt based on the new look target
        UpdateInteractionPrompt();
    }

    /// <summary>
    /// Performs a raycast and populates the _currentLookInfo struct.
    /// </summary>
    private void DetectLookTarget() {
        Ray lookRay = new Ray(playerCameraTransform.position, playerCameraTransform.forward);
        _currentLookInfo = new InteractableInfo(); // Reset info

        if (Physics.Raycast(lookRay, out RaycastHit hit, detectionRange, detectionLayerMask)) {
             GameObject hitObject = hit.collider.gameObject;
             // Prioritize getting components from the collider first, then check parent
             PartInstance part = hit.collider.GetComponent<PartInstance>() ?? hit.collider.GetComponentInParent<PartInstance>();
             MountPoint mount = hit.collider.GetComponent<MountPoint>() ?? hit.collider.GetComponentInParent<MountPoint>();

             _currentLookInfo = new InteractableInfo(hitObject, part, mount);
        }
        // If raycast hits nothing relevant, _currentLookInfo remains empty (default values).
    }

    /// <summary>
    /// Updates the interaction prompt (currently Debug.Log) based on the looked-at object.
    /// </summary>
    private void UpdateInteractionPrompt() {
        // if (interactionPromptText == null) return; // Early exit if no UI text

        string prompt = "";
        if (_currentLookInfo.HasTarget) {
            PartInstance targetPart = _currentLookInfo.Part; // Use property for clarity
            MountPoint targetMount = _currentLookInfo.Mount; // Use property

            if (targetPart != null) { // Check if we are looking at a PartInstance
                // Use the correct GetItemData method
                ItemData partData = targetPart.GetItemData<ItemData>();
                string itemName = partData?.itemName ?? "Part"; // Use null-conditional access

                if (_currentLookInfo.IsLoosePart) {
                    // Prompt assumes HandsBehavior uses LMB ('Fire1') to grab/detach
                    prompt = $"[LMB] Grab {itemName}";
                } else if (_currentLookInfo.IsInstalledPart) {
                     prompt = $"[LMB] Detach {itemName}";
                }
                // Note: If looking at an installed part that *is not* the one on the mount point
                // (e.g., looking at engine block behind a radiator mounted to it),
                // IsInstalledPart might be true but IsLoosePart false. Add specific prompts if needed.

            } else if (targetMount != null && targetMount.CurrentlyAttachedPart == null) {
                // Looking at an empty mount point - prompt depends on HandsBehavior state (if holding compatible part)
                // Example (HandsBehavior would need to signal this state):
                // if (IsPlayerHoldingCompatiblePart(targetMount)) {
                //    prompt = $"[LMB] Attach Held Part to {targetMount.mountPointDefinitionID}";
                // }
                 prompt = $"Look at empty Mount Point: {targetMount.mountPointDefinitionID}"; // Generic placeholder
            }
            // Add more conditions for other interactable types if InteractionController handled them
        }

        // Update UI Text or Debug Log
        // interactionPromptText.text = prompt;
        if (!string.IsNullOrEmpty(prompt)) {
            Debug.Log($"[InteractionPrompt] {prompt}"); // Placeholder UI
        }
    }


    // --- Highlighting Logic ---

    /// <summary>
    /// Called by HandsBehavior to potentially highlight a mount point the player is looking at,
    /// checking for compatibility with the held part.
    /// </summary>
    public void UpdateMountPointHighlight(PartInstance heldPart) {
        // Check if currently looking at a suitable mount point
        MountPoint potentialMount = _currentLookInfo.Mount; // Get from current look info

        if (potentialMount != null && potentialMount.CurrentlyAttachedPart == null && heldPart != null && potentialMount.IsCompatible(heldPart)) {
             // Compatible empty mount point is being looked at
             if (_highlightedMountPoint != potentialMount) {
                 ClearMountHighlight(); // Clear previous if different
                 HighlightMount(potentialMount);
             }
        } else {
             // Not looking at a compatible empty mount point, clear highlight
             if (_highlightedMountPoint != null) {
                 ClearMountHighlight();
             }
        }
    }

    /// <summary>
    /// Applies the highlight effect to the specified MountPoint.
    /// </summary>
    private void HighlightMount(MountPoint mp) {
        if (mp == null) return;

        _highlightedMountPoint = mp;
        _highlightedRenderer = mp.GetComponent<Renderer>(); // Attempt to get renderer

        if (_highlightedRenderer != null) {
            _originalMountMaterial = _highlightedRenderer.material; // Store original
            // Create a temporary instance to avoid modifying shared material asset
            Material tempMat = new Material(_originalMountMaterial);
            tempMat.color = highlightColor;
             // Assign the temporary material
            _highlightedRenderer.material = tempMat;
            // Debug.Log($"[InteractionController] Highlighting Mount: {mp.name}", mp);
        } else {
             Debug.LogWarning($"[InteractionController] Cannot highlight MountPoint '{mp.name}': No Renderer found.", mp);
        }
    }

    /// <summary>
    /// Removes any active highlight effect.
    /// </summary>
    public void ClearMountHighlight() {
        if (_highlightedMountPoint != null && _highlightedRenderer != null) {
            // Check if we have a stored original material to restore
            if (_originalMountMaterial != null) {
                // If the current material is the temp instance we created, destroy it
                if (_highlightedRenderer.material != _originalMountMaterial && _highlightedRenderer.material.name.Contains("(Instance)")) {
                     Destroy(_highlightedRenderer.material);
                }
                // Restore the original material
                _highlightedRenderer.material = _originalMountMaterial;
            }
            // else: No original material stored or renderer lost? Just clear refs.
            // Debug.Log($"[InteractionController] Clearing Highlight from Mount: {_highlightedMountPoint.name}", _highlightedMountPoint);
        }
        // Clear references regardless
        _highlightedMountPoint = null;
        _highlightedRenderer = null;
        _originalMountMaterial = null;
    }
}