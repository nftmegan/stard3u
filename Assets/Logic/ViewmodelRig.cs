using UnityEngine;

public class ViewmodelSwayAnimation : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerLook playerLook;
    [SerializeField] private Transform weaponCamera;

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
        if (playerLook == null)
        {
            Debug.LogError("[PlayerLook] Player look is not assigned!");
        }

        if (weaponCamera == null)
        {
            Debug.LogError("[ViewmodelRig] Weapon Camera is not assigned!");
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

        // === Calculate Mouse Delta ===
        Vector2 currentLookInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
        Vector2 rawDelta = currentLookInput - previousLookInput;
        previousLookInput = currentLookInput;

        smoothedSwayInput = Vector2.Lerp(smoothedSwayInput, rawDelta, Time.deltaTime * swaySmoothness);

        Vector3 swayOffset = new Vector3(
            Mathf.Clamp(-smoothedSwayInput.x * swayMultiplier, -maxSwayX, maxSwayX), // ✅ Inverted X
            Mathf.Clamp(-smoothedSwayInput.y * swayMultiplier, -maxSwayY, maxSwayY),
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
