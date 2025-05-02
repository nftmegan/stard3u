// --- ADSController.cs ---

using UnityEngine;

public class ADSController : MonoBehaviour
{
    [Header("Core References")]
    [Tooltip("The root Transform of the viewmodel (holding Hands, Pistol etc.) that gets moved.")]
    [SerializeField] private Transform adsPivot;

    // Weapon Aim Point - Set externally via SetWeaponAimPoint
    private Transform weaponAimPoint;

    [Header("ADS Configuration")]
    [Tooltip("How quickly the viewmodel transitions towards the aligned pose.")]
    [SerializeField] private float adsSpeed = 10f;
    [Tooltip("How close the aim point needs to be to the anchor to be considered 'aligned' (helps prevent jitter). Smaller values are more precise but might overshoot.")]

    // --- Hip Pose Data ---
    private Vector3 _hipFireLocalPosition;
    private Quaternion _hipFireLocalRotation;

    // --- Runtime State ---
    [SerializeField] private bool _isAiming = false;
    private bool _isInitialized = false; // Tracks if essential refs are set

    // --- Aim Provider & Offset ---
    private IAimProvider _aimProvider;
    private Vector3 _cameraAnchorOffset = Vector3.zero;


    void Awake()
    {
        _isAiming = false;
        _isInitialized = false;

        _aimProvider = GetComponentInParent<IAimProvider>();
        if (_aimProvider == null) Debug.LogError("[ADSController] IAimProvider not found!", this);
        if (adsPivot == null) Debug.LogError("[ADSController] Weapon View Model Holder not assigned!", this);

        CheckInitialization();
        // Debug.Log($"[ADSController Awake] Initialized: {_isInitialized}"); // <<< DEBUG
    }

    void OnEnable()
    {
        CaptureHipPose();
        _isAiming = false;
        CheckInitialization(); // Re-check on enable
        // Debug.Log($"[ADSController OnEnable] Initialized: {_isInitialized}, HipPos: {_hipFireLocalPosition}"); // <<< DEBUG

        if (adsPivot != null)
        {
            adsPivot.localPosition = _hipFireLocalPosition;
            adsPivot.localRotation = _hipFireLocalRotation;
        }
    }

    void OnDisable()
    {
        _isAiming = false;
        _isInitialized = false;
    }

    void Update()
    {
        if (!_isInitialized || adsPivot == null || weaponAimPoint == null)
        {
            if (_isAiming && adsPivot != null) LerpToHipPose();
            return;
        }

        if (_isAiming)
        {
            AlignToAnchor();
        }
        else
        {
            LerpToHipPose();
        }

        Transform currentCameraTransform = _aimProvider?.GetLookTransform();
        Vector3 dynamicAnchorPosition = currentCameraTransform.position + (currentCameraTransform.rotation * _cameraAnchorOffset);
        Debug.DrawLine(dynamicAnchorPosition, dynamicAnchorPosition + currentCameraTransform.forward * 5, Color.red);
    }

    private void AlignToAnchor()
    {
        // --- Get Camera and Calculate Anchor ---
        Transform cam = _aimProvider?.GetLookTransform();
        if (cam == null)
        {
            if (Time.frameCount % 60 == 0)
                Debug.LogWarning("[ADSController] Cannot Align: IAimProvider returned null Look Transform.", this);
            LerpToHipPose();
            return;
        }

        // world‐space anchor (with your offset)
        Vector3 anchorPos = cam.position + (cam.rotation * _cameraAnchorOffset);

        // ─── DEBUG: raw forward of the anchor ────────────────────
        Debug.DrawRay(anchorPos, cam.rotation * Vector3.forward * 0.3f, Color.blue);

        // ─── NEW: compute relative offset from holder → aimPoint ──
        Transform holder = adsPivot;
        // get aimPoint position in holder’s local space
        Vector3 localOffset = holder.InverseTransformPoint(weaponAimPoint.position);
        // reconstruct that offset in world under anchor
        Vector3 idealWorldPos = anchorPos - holder.TransformVector(localOffset);

        // convert into holder‐parent local space
        Vector3 idealLocalPos = holder.parent != null
            ? holder.parent.InverseTransformPoint(idealWorldPos)
            : idealWorldPos;

        // ─── LERP into place ──────────────────────────────────────
        holder.localPosition = Vector3.Lerp(holder.localPosition, idealLocalPos, Time.deltaTime * adsSpeed);

        // (Optional) keep your existing rotation logic, or just keep the hip-rotation:
        holder.localRotation = Quaternion.Slerp(
            holder.localRotation,
            _hipFireLocalRotation,    // or compute a new one if you need it
            Time.deltaTime * adsSpeed
        );
    }


    private void LerpToHipPose()
    {
        if (adsPivot != null)
        {
            adsPivot.localPosition = Vector3.Lerp(adsPivot.localPosition, _hipFireLocalPosition, Time.deltaTime * adsSpeed);
            adsPivot.localRotation = Quaternion.Slerp(adsPivot.localRotation, _hipFireLocalRotation, Time.deltaTime * adsSpeed);
        }
    }

    public void CaptureHipPose()
    {
       if (adsPivot != null)
        {
            _hipFireLocalPosition = adsPivot.localPosition;
            _hipFireLocalRotation = adsPivot.localRotation;
        }
    }

    // --- State Control Methods ---
    public void StartAiming() { _isAiming = true; }
    public void StopAiming() { _isAiming = false; }
    public void ForceStopAiming() { _isAiming = false; }
    public bool IsAiming() { return _isAiming; }

    // --- Configuration Methods ---
    public void SetWeaponAimPoint(Transform newAimPoint)
    {
        // Debug.Log($"[ADSController] SetWeaponAimPoint: {newAimPoint?.name ?? "NULL"}"); // <<< DEBUG
        if (newAimPoint != null) {
            if (weaponAimPoint != newAimPoint) {
                weaponAimPoint = newAimPoint;
                CheckInitialization();
            }
        } else {
             Debug.LogError("[ADSController] Attempted to set a null Weapon Aim Point!", this);
             weaponAimPoint = null;
             CheckInitialization();
        }
    }

    public void SetCameraAnchorOffset(Vector3 offset)
    {
        // Debug.Log($"[ADSController] SetCameraAnchorOffset: {offset}"); // <<< DEBUG
        _cameraAnchorOffset = offset;
        // In this version, recalculation happens anyway in Update, no need to invalidate pose.
    }

    // --- Initialization Check ---
    private void CheckInitialization()
    {
        _isInitialized = adsPivot != null && _aimProvider != null && weaponAimPoint != null;
    }
}