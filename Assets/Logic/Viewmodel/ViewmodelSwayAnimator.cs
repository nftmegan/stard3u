using UnityEngine;

// Removed namespace

public class ViewmodelSwayAnimator : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Assign the PlayerLook component from your player rig.")]
    [SerializeField] private PlayerLook playerLook;
    [Tooltip("Assign the Transform holding your viewmodel camera (often called EquipmentCamera).")]
    [SerializeField] private Transform weaponCamera;

    [Header("Sway Settings")]
    [SerializeField] private float swayMultiplier = 0.02f;
    [SerializeField] private float maxSwayX = 0.06f;
    [SerializeField] private float maxSwayY = 0.06f;
    [SerializeField] private float swaySmoothness = 8f;

    private Vector3 initialCamLocalPosition;
    private Vector2 smoothedSwayDelta; // Store the smoothed delta, not raw input
    private float previousYaw;
    private float previousPitch;

    private void Awake()
    {
        // Attempt to find PlayerLook if not assigned
        if (playerLook == null) playerLook = GetComponentInParent<PlayerLook>();

        if (weaponCamera == null)
        {
            Debug.LogError("[ViewmodelSwayAnimator] Weapon Camera is not assigned!", this);
            this.enabled = false; // Disable if camera is missing
            return;
        }
        if (playerLook == null)
        {
            Debug.LogError("[ViewmodelSwayAnimator] PlayerLook component not found or assigned!", this);
            this.enabled = false; // Disable if look component is missing
            return;
        }
    }

    private void Start()
    {
        if (weaponCamera != null)
        {
            initialCamLocalPosition = weaponCamera.localPosition;
        }
        // Initialize previous look values
        previousYaw = playerLook.CurrentYaw;
        previousPitch = playerLook.CurrentPitch;
    }

    private void LateUpdate()
    {
        // Guard clause
        if (playerLook == null || weaponCamera == null) return;

        // --- Calculate Look Delta based on PlayerLook's current state ---
        // Use the CurrentYaw and CurrentPitch properties from the refactored PlayerLook
        float currentYaw = playerLook.CurrentYaw;
        float currentPitch = playerLook.CurrentPitch;

        // Calculate the change in look angle since the last frame
        // Mathf.DeltaAngle handles wrapping correctly (e.g., from 359 to 1 degree)
        float deltaYaw = Mathf.DeltaAngle(previousYaw, currentYaw);
        float deltaPitch = Mathf.DeltaAngle(previousPitch, currentPitch);

        // Update previous values for the next frame
        previousYaw = currentYaw;
        previousPitch = currentPitch;

        // Create the raw delta vector
        Vector2 rawDelta = new Vector2(deltaYaw, deltaPitch);

        // --- Smooth the delta ---
        // Lerp towards the new delta for smoother sway transitions
        smoothedSwayDelta = Vector2.Lerp(smoothedSwayDelta, rawDelta, Time.deltaTime * swaySmoothness);

        // --- Calculate Sway Offset ---
        // Apply multiplier and clamp the smoothed delta
        // Note the inversion: Yaw delta moves camera opposite horizontally (X), Pitch delta moves opposite vertically (Y)
        Vector3 swayOffset = new Vector3(
            Mathf.Clamp(-smoothedSwayDelta.x * swayMultiplier, -maxSwayX, maxSwayX),
            Mathf.Clamp(-smoothedSwayDelta.y * swayMultiplier, -maxSwayY, maxSwayY), // Usually negative pitch delta means look up -> camera sways down
            0f // No sway on Z axis
        );

        // --- Apply Sway to Weapon Camera Position ---
        // Lerp the camera's local position towards the target position (initial + sway)
        weaponCamera.localPosition = Vector3.Lerp(
            weaponCamera.localPosition,
            initialCamLocalPosition + swayOffset,
            Time.deltaTime * swaySmoothness // Apply smoothness again for the position follow
        );
    }
}