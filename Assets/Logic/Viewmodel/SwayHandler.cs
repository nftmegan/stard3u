using UnityEngine;

public class SwayHandler : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Assign the PlayerLook component from your player rig.")]
    [SerializeField] private PlayerLook playerLook;
    [Tooltip("Assign the Transform holding your viewmodel camera (often called EquipmentCamera).")]
    [SerializeField] private Transform weaponCamera;

    [Header("Sway Settings")]
    [Tooltip("Base sway strength applied at multiplier = 1.")]
    [SerializeField] private float defaultSwayMultiplier = 0.02f;
    [Tooltip("Current overall sway strength (default * multiplier).")]
    [SerializeField] private float currentSwayMultiplier;
    [SerializeField] private float maxSwayX = 0.06f;
    [SerializeField] private float maxSwayY = 0.06f;
    [SerializeField] private float swaySmoothness = 8f;

    private Vector3 initialCamLocalPosition;
    private Vector2 smoothedSwayDelta;
    private float previousYaw;
    private float previousPitch;

    private void Awake()
    {
        if (playerLook == null)
            playerLook = GetComponentInParent<PlayerLook>();

        if (weaponCamera == null)
        {
            Debug.LogError("[SwayHandler] Weapon Camera is not assigned!", this);
            enabled = false;
            return;
        }

        if (playerLook == null)
        {
            Debug.LogError("[SwayHandler] PlayerLook component not found or assigned!", this);
            enabled = false;
            return;
        }
    }

    private void Start()
    {
        initialCamLocalPosition = weaponCamera.localPosition;
        previousYaw = playerLook.CurrentYaw;
        previousPitch = playerLook.CurrentPitch;

        // Ensure we start with normal hip-fire sway
        currentSwayMultiplier = defaultSwayMultiplier;
    }

    private void LateUpdate()
    {
        if (playerLook == null || weaponCamera == null) return;

        // 1) Compute raw look delta
        float currentYaw = playerLook.CurrentYaw;
        float currentPitch = playerLook.CurrentPitch;
        float deltaYaw = Mathf.DeltaAngle(previousYaw, currentYaw);
        float deltaPitch = Mathf.DeltaAngle(previousPitch, currentPitch);
        previousYaw = currentYaw;
        previousPitch = currentPitch;
        Vector2 rawDelta = new Vector2(deltaYaw, deltaPitch);

        // 2) Smooth it
        smoothedSwayDelta = Vector2.Lerp(smoothedSwayDelta, rawDelta, Time.deltaTime * swaySmoothness);

        // 3) Compute offset (invert axes as needed)
        Vector3 swayOffset = new Vector3(
            Mathf.Clamp(-smoothedSwayDelta.x * currentSwayMultiplier, -maxSwayX, maxSwayX),
            Mathf.Clamp(smoothedSwayDelta.y  * currentSwayMultiplier, -maxSwayY, maxSwayY),
            0f
        );

        // 4) Debug visualization
        Debug.DrawLine(
            weaponCamera.parent.TransformPoint(initialCamLocalPosition),
            weaponCamera.parent.TransformPoint(initialCamLocalPosition + swayOffset),
            Color.cyan
        );

        // 5) Apply sway
        weaponCamera.localPosition = Vector3.Lerp(
            weaponCamera.localPosition,
            initialCamLocalPosition + swayOffset,
            Time.deltaTime * swaySmoothness
        );
    }
}