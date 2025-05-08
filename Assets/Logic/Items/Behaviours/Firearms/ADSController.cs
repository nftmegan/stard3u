// In Assets/Scripts/Player/Equipment/ADSController.cs (or your path)
using UnityEngine;

/// <summary>
/// Manages the Aim Down Sights (ADS) transition for an equippable item's viewmodel.
/// Lerps the position and rotation of the 'adsPivot' transform to align a specific
/// 'weaponAimPoint' (e.g., the sight) with a calculated camera anchor point.
/// Relies on an IAimProvider for camera information.
/// </summary>
public class ADSController : MonoBehaviour {
    #region Inspector Fields
    [Header("Core References")]
    [Tooltip("The root Transform of the viewmodel (e.g., holding Pistol visuals) that gets moved/rotated for ADS.")]
    [SerializeField] private Transform adsPivot;
    // Note: weaponAimPoint is set dynamically via SetWeaponAimPoint

    [Header("ADS Configuration")]
    [Tooltip("How quickly the viewmodel transitions between hip and ADS poses.")]
    [SerializeField] private float adsSpeed = 15f; // Increased default speed slightly
    // Note: The proximity threshold for alignment isn't explicitly used in the current lerp logic,
    // but could be added for snapping or different interpolation methods.
    #endregion

    #region Private Fields
    // Dynamic reference to the specific point on the weapon model that should align with the camera anchor
    private Transform weaponAimPoint;

    // Pose captured when not aiming
    private Vector3 _hipFireLocalPosition = Vector3.zero;
    private Quaternion _hipFireLocalRotation = Quaternion.identity;

    // Runtime state
    private bool _isAiming = false; // Internal flag controlled by Start/StopAiming
    private bool _isInitialized = false; // Tracks if essential references are set

    // Dependencies
    private IAimProvider _aimProvider; // Provides camera transform and look direction
    private Vector3 _cameraAnchorOffset = Vector3.zero; // Offset from camera where the weaponAimPoint should align
    #endregion

    #region Public Properties
    /// <summary>
    /// Gets whether the controller is currently in the Aiming Down Sights state.
    /// </summary>
    public bool IsAiming => _isAiming;
    #endregion

    #region Unity Lifecycle
    void Awake() {
        _isAiming = false; // Ensure starting state
        _isInitialized = false;

        // Find required IAimProvider dependency
        _aimProvider = GetComponentInParent<IAimProvider>(); // Search hierarchy
        if (_aimProvider == null) Debug.LogError($"[{GetType().Name} on {gameObject.name}] IAimProvider component not found in parents!", this);

        // Validate essential Inspector reference
        if (adsPivot == null) Debug.LogError($"[{GetType().Name} on {gameObject.name}] ADS Pivot transform not assigned!", this);
        else {
            // Capture initial hip pose in Awake *after* validating adsPivot exists
            CaptureHipPose();
        }

        // Initial check, requires weaponAimPoint to be set later via SetWeaponAimPoint
        CheckInitialization();
    }

    void OnEnable() {
        // It's generally safer to capture/reset pose OnEnable in case the object was
        // disabled and re-enabled, potentially with transform changes.
        if (adsPivot != null) {
             CaptureHipPose(); // Recapture in case something moved it while disabled
             // Reset visual state immediately
             adsPivot.localPosition = _hipFireLocalPosition;
             adsPivot.localRotation = _hipFireLocalRotation;
        }
        _isAiming = false; // Ensure ADS is off when enabled
        CheckInitialization(); // Re-check if weaponAimPoint was set while disabled
    }

    void OnDisable() {
        // Reset state when disabled
        _isAiming = false;
        _isInitialized = false; // Mark as uninitialized as dependencies might change/become invalid
    }

    void Update() {
        // Only update if initialized (requires adsPivot, aimProvider, and weaponAimPoint)
        if (!_isInitialized || adsPivot == null || weaponAimPoint == null || _aimProvider == null) {
            // If aiming was requested but we aren't initialized, try to return to hip pose visually
            if (_isAiming && adsPivot != null) {
                 LerpToHipPose();
            }
            return; // Do nothing further if not ready
        }

        // Perform transitions based on the aiming state
        if (_isAiming) {
            AlignToAnchor();
        } else {
            LerpToHipPose();
        }

        // Optional: Debug draw line for the anchor point
        // Transform currentCameraTransform = _aimProvider.GetLookTransform();
        // if (currentCameraTransform != null) {
        //     Vector3 dynamicAnchorPosition = currentCameraTransform.position + (currentCameraTransform.rotation * _cameraAnchorOffset);
        //     Debug.DrawLine(dynamicAnchorPosition, dynamicAnchorPosition + currentCameraTransform.forward * 0.1f, Color.red); // Short line from anchor
        // }
    }
    #endregion

    #region ADS Alignment Logic
    /// <summary>
    /// Smoothly moves the adsPivot to align the weaponAimPoint with the calculated camera anchor.
    /// </summary>
    private void AlignToAnchor() {
        Transform cam = _aimProvider.GetLookTransform();
        Transform holder = adsPivot; // Use local variable for clarity

        // Need all references to proceed
        if (cam == null || holder == null || weaponAimPoint == null) {
             if (Time.frameCount % 120 == 0) // Log warning occasionally
                Debug.LogWarning("[ADSController] Cannot AlignToAnchor: Missing Camera, Holder, or WeaponAimPoint.", this);
            LerpToHipPose(); // Attempt to return to safety (hip)
            return;
        }

        // 1. Calculate World-Space Anchor Point (camera position + offset relative to camera rotation)
        Vector3 anchorPos = cam.position + (cam.rotation * _cameraAnchorOffset);

        // 2. Calculate Ideal World Position for the Pivot
        // Find the weapon aim point's position relative to the pivot (in pivot's local space)
        Vector3 localOffsetFromPivotToAimPoint = holder.InverseTransformPoint(weaponAimPoint.position);
        // Determine where the pivot *should* be in world space so that its local offset matches the anchor
        Vector3 idealWorldPosForPivot = anchorPos - holder.TransformDirection(localOffsetFromPivotToAimPoint); // Use TransformDirection for offset vector

        // 3. Convert Ideal World Position to Pivot's Parent's Local Space
        Vector3 idealLocalPosForPivot = holder.parent != null
            ? holder.parent.InverseTransformPoint(idealWorldPosForPivot)
            : idealWorldPosForPivot; // Use world space if no parent

        // 4. Lerp Pivot Position
        holder.localPosition = Vector3.Lerp(holder.localPosition, idealLocalPosForPivot, Time.deltaTime * adsSpeed);

        // 5. Lerp Pivot Rotation
        // Option A: Lerp towards camera's rotation (simple alignment)
        // Quaternion targetRotation = Quaternion.LookRotation(cam.forward, cam.up); // Basic forward align
        // if (holder.parent != null) targetRotation = Quaternion.Inverse(holder.parent.rotation) * targetRotation; // Convert to local if parented
        // holder.localRotation = Quaternion.Slerp(holder.localRotation, targetRotation, Time.deltaTime * adsSpeed);

        // Option B: Lerp towards a predefined ADS local rotation (often simpler and more stable visually)
        // You might need another field like 'Quaternion adsLocalRotation' set based on weapon type.
        // For now, let's lerp back towards the initial hip rotation for simplicity.
        // Replace _hipFireLocalRotation with 'adsTargetLocalRotation' if you define one.
        holder.localRotation = Quaternion.Slerp(holder.localRotation, _hipFireLocalRotation, Time.deltaTime * adsSpeed);
    }

    /// <summary>
    /// Smoothly moves the adsPivot back to its original captured hip-fire position and rotation.
    /// </summary>
    private void LerpToHipPose() {
        if (adsPivot != null) {
            adsPivot.localPosition = Vector3.Lerp(adsPivot.localPosition, _hipFireLocalPosition, Time.deltaTime * adsSpeed);
            adsPivot.localRotation = Quaternion.Slerp(adsPivot.localRotation, _hipFireLocalRotation, Time.deltaTime * adsSpeed);
        }
    }

    /// <summary>
    /// Captures the current local position and rotation of the adsPivot to use as the hip-fire target pose.
    /// Call this when the weapon is first equipped or when attachments might change the hip pose.
    /// </summary>
    public void CaptureHipPose() {
       if (adsPivot != null) {
            _hipFireLocalPosition = adsPivot.localPosition;
            _hipFireLocalRotation = adsPivot.localRotation;
        } else {
             Debug.LogWarning("[ADSController] Cannot capture hip pose - adsPivot is null.", this);
        }
    }
    #endregion

    #region State Control & Configuration
    /// <summary>Starts the ADS transition.</summary>
    public void StartAiming() { if(_isInitialized) _isAiming = true; else Debug.LogWarning("[ADSController] Cannot StartAiming - Not Initialized.", this); }

    /// <summary>Stops the ADS transition.</summary>
    public void StopAiming() { _isAiming = false; }

    /// <summary>Immediately stops aiming, regardless of current transition.</summary>
    public void ForceStopAiming() { _isAiming = false; if(adsPivot != null) { adsPivot.localPosition = _hipFireLocalPosition; adsPivot.localRotation = _hipFireLocalRotation; } } // Snap back instantly

    // IsAiming is now a public property

    /// <summary>
    /// Sets the specific Transform on the weapon model that should align with the camera anchor during ADS.
    /// Typically called by AttachmentController when sights change.
    /// </summary>
    public void SetWeaponAimPoint(Transform newAimPoint) {
        // Only update and re-check initialization if the point actually changes
        if (weaponAimPoint != newAimPoint) {
             weaponAimPoint = newAimPoint; // Can be null if no sight is attached
             CheckInitialization(); // Re-evaluate if we are ready now
             // Debug.Log($"[ADSController] Weapon Aim Point set to: {weaponAimPoint?.name ?? "NULL"}");
        }
    }

    /// <summary>
    /// Sets the offset from the camera's origin where the WeaponAimPoint should align.
    /// Allows different weapons/sights to sit differently relative to the camera.
    /// Typically called by AttachmentController.
    /// </summary>
    public void SetCameraAnchorOffset(Vector3 offset) {
        _cameraAnchorOffset = offset;
    }
    #endregion

    #region Internal Methods
    /// <summary>
    /// Checks if all required dependencies (adsPivot, _aimProvider, weaponAimPoint) are set.
    /// Updates the _isInitialized flag.
    /// </summary>
    private void CheckInitialization() {
        _isInitialized = adsPivot != null && _aimProvider != null && weaponAimPoint != null;
        // if (!_isInitialized && Time.time > 0.5f) { // Optional: Log warning if not initialized after a short time
             // Debug.LogWarning($"[ADSController] Not Initialized. Pivot: {adsPivot != null}, AimProvider: {_aimProvider != null}, WeaponAimPoint: {weaponAimPoint != null}");
        // }
    }
    #endregion
}