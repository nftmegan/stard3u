using UnityEngine;

// Ensure this class actually implements IAimProvider
public class PlayerLook : MonoBehaviour, IAimProvider
{
    [Header("References")]
    [SerializeField] private Transform cameraPivot; // The object that handles Pitch rotation (usually the camera itself)
    [SerializeField] private Transform yawRoot;     // The object that handles Yaw rotation (usually the player's root body)

    [Header("Look Settings")]
    [SerializeField] private float sensitivityX = 2f;
    [SerializeField] private float sensitivityY = 2f;
    [SerializeField] private float minPitch = -85f; // Slightly increased range
    [SerializeField] private float maxPitch = 85f;

    [Header("Aim Settings (for IAimProvider)")]
    [SerializeField] private float maxAimDistance = 100f;
    [SerializeField] private LayerMask aimCollisionLayers = ~0; // Hit everything by default

    // Runtime state
    private float _targetYaw;
    private float _targetPitch;

    // Public properties
    public Quaternion YawOrientation => yawRoot != null ? yawRoot.rotation : Quaternion.identity;
    public Quaternion PitchOrientation => cameraPivot != null ? cameraPivot.localRotation : Quaternion.identity;
    public float CurrentYaw => _targetYaw;
    public float CurrentPitch => _targetPitch;

    private void Awake()
    {
        // Auto-assign yawRoot if not set
        if (yawRoot == null) yawRoot = transform;
        if (cameraPivot == null) {
             // Try finding camera tagged "MainCamera" as a child if pivot isn't set
             Camera mainCam = GetComponentInChildren<Camera>();
             if (mainCam != null && mainCam.CompareTag("MainCamera")) {
                 cameraPivot = mainCam.transform;
                 Debug.LogWarning($"[PlayerLook] Camera Pivot not assigned on {gameObject.name}, automatically assigned to child Camera '{cameraPivot.name}'.", this);
             } else {
                 Debug.LogError("[PlayerLook] Camera Pivot not assigned and could not be found automatically!", this);
                 this.enabled = false;
                 return;
             }
        }

        // Initialize rotation from current transform state
        _targetYaw = yawRoot.eulerAngles.y;
        // Correctly handle initial pitch if camera is rotated down
        Vector3 currentPivotEuler = cameraPivot.localEulerAngles;
        _targetPitch = (currentPivotEuler.x > 180) ? currentPivotEuler.x - 360 : currentPivotEuler.x;

        // Initial application ensures visual matches internal state
        ApplyRotation();
    }

    // Called by PlayerManager when look input is received
    public void SetLookInput(Vector2 lookDelta)
    {
        if (Mathf.Approximately(Time.timeScale, 0f)) return; // Don't process look input if paused

        // Accumulate rotation changes
        _targetYaw += lookDelta.x * sensitivityX;
        _targetPitch -= lookDelta.y * sensitivityY; // Subtract Y delta for standard FPS controls

        // Clamp pitch
        _targetPitch = Mathf.Clamp(_targetPitch, minPitch, maxPitch);

        ApplyRotation();
    }

    private void ApplyRotation()
    {
         if (yawRoot) yawRoot.localRotation = Quaternion.Euler(0f, _targetYaw, 0f); // Apply Yaw to root
         if (cameraPivot) cameraPivot.localRotation = Quaternion.Euler(_targetPitch, 0f, 0f); // Apply Pitch to pivot
    }

    // --- IAimProvider Implementation ---
    public Vector3 GetAimHitPoint() {
         if (!cameraPivot) {
             Debug.LogError("[PlayerLook GetAimHitPoint] Camera Pivot is null!", this);
             return (yawRoot ?? transform).position + (yawRoot ?? transform).forward * maxAimDistance; // Fallback
         }
         Ray ray = GetLookRay();
         if (Physics.Raycast(ray, out RaycastHit hit, maxAimDistance, aimCollisionLayers, QueryTriggerInteraction.Ignore)) {
             return hit.point;
         }
         return ray.origin + ray.direction * maxAimDistance; // Return point far along ray if no hit
    }

    public Ray GetLookRay() {
         if (!cameraPivot) {
             Debug.LogError("[PlayerLook GetLookRay] Camera Pivot is null!", this);
             Transform root = yawRoot ?? transform;
             return new Ray(root.position, root.forward); // Fallback ray from root
         }
         // Ray originates from camera pivot and goes forward
         return new Ray(cameraPivot.position, cameraPivot.forward);
    }

    public Transform GetLookTransform() {
        // Return the transform used for raycasting (the camera pivot)
        if (!cameraPivot) Debug.LogError("[PlayerLook GetLookTransform] Camera Pivot is null!", this);
        return cameraPivot;
     }

     // Added methods that might be useful from IAimProvider interface if defined elsewhere
     public Vector3 GetLookDirection() => cameraPivot ? cameraPivot.forward : (yawRoot ?? transform).forward;
     public Vector3 GetLookOrigin() => cameraPivot ? cameraPivot.position : (yawRoot ?? transform).position;
     public Vector3 GetAimDirection(Vector3 fromPosition) => (GetAimHitPoint() - fromPosition).normalized;
}