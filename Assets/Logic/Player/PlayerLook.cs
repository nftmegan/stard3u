using UnityEngine;

public class PlayerLook : MonoBehaviour, IAimProvider
{
    [Header("References")]
    [SerializeField] private Transform cameraPivot;
    private Transform yawRoot;

    [Header("Look Settings")]
    [SerializeField] private float sensitivityX = 2f;
    [SerializeField] private float sensitivityY = 2f;
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;

    [Header("Aim Settings")]
    [SerializeField] private float maxAimDistance = 100f;
    [SerializeField] private LayerMask aimCollisionLayers = ~0;

    // Runtime state
    private float _targetYaw;
    private float _targetPitch;

    // REMOVED: Reference to UIStateController

    // Public properties
    public Quaternion YawOrientation => yawRoot != null ? yawRoot.rotation : Quaternion.identity;
    public Quaternion PitchOrientation => cameraPivot != null ? cameraPivot.localRotation : Quaternion.identity;
    public float CurrentYaw => _targetYaw;
    public float CurrentPitch => _targetPitch;

    private void Awake()
    {
        if (yawRoot == null) yawRoot = transform;
        if (cameraPivot == null) { Debug.LogError("Camera Pivot not assigned!", this); this.enabled = false; return; }

        // REMOVED: Finding UIStateController

        _targetYaw = yawRoot.eulerAngles.y;
        Vector3 currentPivotEuler = cameraPivot.localEulerAngles;
        _targetPitch = (currentPivotEuler.x > 180) ? currentPivotEuler.x - 360 : currentPivotEuler.x;
    }

    public void SetLookInput(Vector2 lookDelta)
    {
        _targetYaw += lookDelta.x * sensitivityX;
        _targetPitch = Mathf.Clamp(_targetPitch - lookDelta.y * sensitivityY, minPitch, maxPitch);
        ApplyRotation();
    }

    private void ApplyRotation()
    {
         if (yawRoot) yawRoot.rotation = Quaternion.Euler(0f, _targetYaw, 0f);
         if (cameraPivot) cameraPivot.localRotation = Quaternion.Euler(_targetPitch, 0f, 0f);
    }

    // REMOVED: Update method that handled cursor locking

    // --- IAimProvider Implementation (Remains the same) ---
    public Vector3 GetAimHitPoint() { if (!cameraPivot) return yawRoot.position + yawRoot.forward * maxAimDistance; Ray ray = GetLookRay(); if (Physics.Raycast(ray, out RaycastHit hit, maxAimDistance, aimCollisionLayers, QueryTriggerInteraction.Ignore)) return hit.point; return ray.origin + ray.direction * maxAimDistance; }
    public Ray GetLookRay() { if (!cameraPivot) return new Ray(yawRoot.position, yawRoot.forward); Vector3 origin = cameraPivot.position; Vector3 direction = cameraPivot.forward; return new Ray(origin, direction); }
    public Transform GetLookTransform() => cameraPivot;
    public Vector3 GetLookDirection() => cameraPivot ? cameraPivot.forward : yawRoot.forward;
    public Vector3 GetLookOrigin() => cameraPivot ? cameraPivot.position : yawRoot.position;
    public Vector3 GetAimDirection(Vector3 fromPosition) => (GetAimHitPoint() - fromPosition).normalized;
}