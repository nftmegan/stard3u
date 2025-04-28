using UnityEngine;

public class ViewmodelSwayAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerLook playerLook; // PlayerLook reference
    [SerializeField] private Transform weaponCamera; // EquipmentCamera

    [Header("Sway Settings")]
    public float swayMultiplier = 0.02f;
    public float maxSwayX = 0.06f;
    public float maxSwayY = 0.06f;
    public float swaySmoothness = 8f;

    private Vector3 initialCamLocalPosition;
    private Vector2 smoothedSwayInput;
    private Vector2 previousLookInput;

    private void Awake()
    {
        if (weaponCamera == null)
        {
            Debug.LogError("[ViewmodelSwayAnimator] Weapon Camera is not assigned!");
        }
    }

    private void Start()
    {
        if (weaponCamera != null)
        {
            initialCamLocalPosition = weaponCamera.localPosition;
        }
    }

    private void LateUpdate()
    {
        if (playerLook == null || weaponCamera == null)
            return;

        // === Use PlayerLook's yaw and pitch for sway instead of raw mouse input ===
        Vector2 currentLookInput = new Vector2(playerLook.Yaw, playerLook.Pitch); // Get values from PlayerLook
        Vector2 rawDelta = currentLookInput - previousLookInput; // Calculate delta based on PlayerLook
        previousLookInput = currentLookInput;

        smoothedSwayInput = Vector2.Lerp(smoothedSwayInput, rawDelta, Time.deltaTime * swaySmoothness);

        // === Invert X axis sway ===
        Vector3 swayOffset = new Vector3(
            Mathf.Clamp(smoothedSwayInput.x * swayMultiplier, -maxSwayX, maxSwayX), // Inverted X sway
            Mathf.Clamp(-smoothedSwayInput.y * swayMultiplier, -maxSwayY, maxSwayY), // Normal Y sway
            0f
        );

        // === Apply position sway only ===
        weaponCamera.localPosition = Vector3.Lerp(
            weaponCamera.localPosition,
            initialCamLocalPosition + swayOffset,
            Time.deltaTime * swaySmoothness
        );
    }
}
