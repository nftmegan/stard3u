using UnityEngine;

public class ViewmodelMovementAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MyCharacterController characterMotor; // Your real movement controller
    [SerializeField] private Transform weaponCamera; // EquipmentCamera

    [Header("Animation Settings")]
    public float idleBobFrequency = 1.5f;
    public float idleBobAmplitude = 0.005f;

    public float walkBobFrequency = 6f;
    public float walkBobAmplitude = 0.015f;

    public float runBobFrequency = 10f;
    public float runBobAmplitude = 0.03f;

    public float bobSpeedMultiplier = 1f;
    public float bobSmoothness = 8f;

    [Header("Speed Thresholds")]
    public float walkSpeedThreshold = 1f;
    public float runSpeedThreshold = 5f;

    private Vector3 initialLocalPosition;
    private float bobTimer;
    private float currentFrequency;
    private float currentAmplitude;

    private void Awake()
    {
        if (weaponCamera == null)
            Debug.LogError("[ViewmodelMovementAnimator] WeaponCamera reference missing!");
    }

    private void Start()
    {
        if (weaponCamera != null)
            initialLocalPosition = weaponCamera.localPosition;
    }

    private void LateUpdate()
    {
        if (characterMotor == null || weaponCamera == null)
            return;

        // Get player horizontal velocity
        Vector3 flatVelocity = new Vector3(characterMotor.GetVelocity().x, 0f, characterMotor.GetVelocity().z);
        float speed = flatVelocity.magnitude;

        // Select bob settings based on speed
        if (speed < walkSpeedThreshold)
        {
            // Idle
            currentFrequency = idleBobFrequency;
            currentAmplitude = idleBobAmplitude;
        }
        else if (speed < runSpeedThreshold)
        {
            // Walking
            currentFrequency = walkBobFrequency;
            currentAmplitude = walkBobAmplitude;
        }
        else
        {
            // Running
            currentFrequency = runBobFrequency;
            currentAmplitude = runBobAmplitude;
        }

        // Bobbing over time
        bobTimer += Time.deltaTime * currentFrequency * bobSpeedMultiplier;
        Vector3 bobOffset = new Vector3(
            0f,
            Mathf.Sin(bobTimer) * currentAmplitude,
            0f
        );

        // Smoothly apply bobbing
        weaponCamera.localPosition = Vector3.Lerp(
            weaponCamera.localPosition,
            initialLocalPosition + bobOffset,
            Time.deltaTime * bobSmoothness
        );
    }
}