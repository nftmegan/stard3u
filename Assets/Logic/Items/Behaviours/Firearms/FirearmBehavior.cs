using UnityEngine;
using System.Collections;

/// <summary>
/// Base behavior for firearms, handling core logic like shooting and reloading.
/// Coordinates with AttachmentController, RecoilHandler, SpreadController, etc.
/// </summary>
public abstract class FirearmBehavior : EquippableBehavior
{
    #region Inspector Fields

    [Header("Core Component References")]
    [Tooltip("Manages attachment visuals, stats, and updates ADS/Recoil/Spread handlers.")]
    [SerializeField] private AttachmentController attachmentController;
    [Tooltip("Handles audio playback for firearm actions.")]
    [SerializeField] private FirearmAudioHandler audioHandler;
    [Tooltip("Handles muzzle flash visual effects.")]
    [SerializeField] private MuzzleFlashHandler muzzleHandler;
    [Tooltip("Handles visual recoil motion (configured by AttachmentController).")]
    [SerializeField] private RecoilHandler recoilHandler;
    [Tooltip("Handles Aim Down Sights (configured by AttachmentController).")]
    [SerializeField] private ADSController adsController;
    [Tooltip("Handles random bullet spread (configured by AttachmentController).")]
    [SerializeField] private SpreadHandler spreadController;
    [Tooltip("The point from which projectiles are spawned.")]
    [SerializeField] protected Transform firePoint;

    [Header("Required Dependencies")]
    [Tooltip("Provides aiming direction and target point.")]
    [SerializeField]
    private IAimProvider aimProvider;

    #endregion

    #region Private Fields

    // --- References to Data/State ---
    private FirearmItemData def;
    private FirearmState state;

    // --- Runtime State ---
    private Coroutine reloadRoutine;
    private Coroutine autoFireRoutine;
    private bool isCooldown;
    private bool isReloading;
    private float lastShotTime;
    private bool _isInitialized = false;
    private bool _isADS = false; // Track ADS state locally

    #endregion

    #region Properties

    private int MaxBullets => def?.magazineSize ?? 1;
    private float AutoDelay => (def != null && def.fireRate > 0) ? 1f / def.fireRate : 1f;
    private bool IsAuto => def != null && def.fireMode == FireMode.Auto;
    private int CurrentAmmo => state?.magazine != null && state.magazine.Size > 0 ? state.magazine[0].quantity : 0;

    #endregion

    #region Unity Lifecycle

    protected virtual void Awake()
    {
        // Find essential dependencies if not assigned via Inspector
        aimProvider ??= GetComponentInParent<IAimProvider>();

        // Validate core components assigned in Inspector (or find as fallback)
        attachmentController ??= GetComponent<AttachmentController>();
        audioHandler ??= GetComponentInChildren<FirearmAudioHandler>(true);
        muzzleHandler ??= GetComponentInChildren<MuzzleFlashHandler>(true);
        recoilHandler ??= GetComponentInChildren<RecoilHandler>(true);
        adsController ??= GetComponent<ADSController>();
        spreadController ??= GetComponent<SpreadHandler>();

        // Log errors for missing *required* components after attempting to find them
        if (aimProvider == null) Debug.LogError($"[{GetType().Name} on {gameObject.name}] IAimProvider not found!", this);
        if (attachmentController == null) Debug.LogError($"[{GetType().Name} on {gameObject.name}] AttachmentController missing!", this);
        if (audioHandler == null) Debug.LogError($"[{GetType().Name} on {gameObject.name}] FirearmAudioHandler missing!", this);
        if (recoilHandler == null) Debug.LogError($"[{GetType().Name} on {gameObject.name}] RecoilHandler missing!", this);
        if (adsController == null) Debug.LogError($"[{GetType().Name} on {gameObject.name}] ADSController missing!", this);
        if (spreadController == null) Debug.LogError($"[{GetType().Name} on {gameObject.name}] SpreadController missing!", this);
        if (firePoint == null) Debug.LogError($"[{GetType().Name} on {gameObject.name}] Fire Point missing!", this);
    }

    public override void Initialize(InventoryItem inv, ItemContainer ownerInv)
    {
        base.Initialize(inv, ownerInv); // Sets runtimeItem and ownerInventory

        _isInitialized = false; // Reset flag

        StopRunningCoroutines();
        isReloading = false; // Reset reload state explicitly

        // --- Get Data and State ---
        if (inv != null)
        {
            def = inv.data as FirearmItemData;
            state = inv.runtime as FirearmState;

            if (def == null) { Debug.LogError($"[{GetType().Name} on {gameObject.name}] Init failed: ItemData is not FirearmItemData!", this); return; }
            if (state == null) { Debug.LogError($"[{GetType().Name} on {gameObject.name}] Init failed: InventoryItem missing FirearmState!", this); return; }
        }
        else // Handle unarmed case
        {
            Debug.LogWarning($"[{GetType().Name} on {gameObject.name}] Initializing as unarmed (null InventoryItem).", this);
            def = null;
            state = null;
        }

        // --- Initialize Attachment Controller ---
        // This is now the primary setup point for attachments and dependent handlers
        if (attachmentController != null)
        {
             attachmentController.Initialize(state, def, recoilHandler, adsController, spreadController);
        }
        else
        {
             Debug.LogError($"[{GetType().Name} on {gameObject.name}] Cannot Initialize: AttachmentController is missing!", this);
             return; // Stop initialization if critical component is missing
        }

        // --- Final State Setup ---
        // Ensure handlers know initial ADS state AFTER AttachmentController has configured them
        SetADSState(false);

        _isInitialized = true; // Mark as initialized
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        // Reset transient states when re-enabled
        isCooldown = false;
        // SetADSState is called in Initialize which is called after OnEnable by EquipmentController
        // If EquipmentController pattern changes, might need SetADSState(false) here too.
    }

    protected override void OnDisable()
    {
        // Stop active processes
        StopRunningCoroutines();
        CancelInvoke();
        isReloading = false;
        SetADSState(false); // Ensure handlers know ADS is off when disabled
        _isInitialized = false; // Reset flag

        base.OnDisable();
        // AttachmentController handles its own OnDisable
    }

    #endregion

    #region Input Handling

    public override void OnFire1Down() { if (!CanPerformAction() || isReloading) return; if (IsAuto) StartAutoFire(); else AttemptSingleFire(); }
    public override void OnFire1Up() { StopAutoFire(); }
    public override void OnFire1Hold() { /* Auto fire handled by coroutine */ }

    public override void OnFire2Down() { if (!CanPerformAction() || isReloading) return; adsController?.StartAiming(); SetADSState(true); }
    public override void OnFire2Up() { adsController?.StopAiming(); SetADSState(false); } // Safe to call even if not initialized
    public override void OnFire2Hold() { /* ADSController manages hold state */ }

    public override void OnReloadDown() { if (!CanPerformAction()) return; AttemptReload(); }

    public override void OnUtilityDown() { /* Implement if needed */ }
    public override void OnUtilityUp() { /* Implement if needed */ }

    #endregion

    #region Core Firing Logic

    private void AttemptSingleFire() { if (isCooldown || isReloading) return; if (Time.time - lastShotTime < AutoDelay) return; if (CurrentAmmo <= 0) { audioHandler?.PlayDryFire(); lastShotTime = Time.time; } else { PerformShot(); ConsumeRound(); StartCooldown(); lastShotTime = Time.time; } }
    protected virtual void PerformShot() { if (state?.magazine == null || state.magazine.Size == 0 || state.magazine[0].IsEmpty()) return; if (firePoint == null || aimProvider == null || recoilHandler == null || spreadController == null) { Debug.LogError($"[{GetType().Name} on {gameObject.name}] PerformShot failed - Missing critical reference.", this); return; } var projItemData = state.magazine[0].item?.data as ProjectileItemData; if (projItemData == null || projItemData.projectilePrefab == null) { Debug.LogWarning($"[{GetType().Name}] No projectile prefab for ammo: {state.magazine[0].item?.data?.itemName ?? "NULL"}", this); return; } var projPrefab = projItemData.projectilePrefab; Vector3 targetPoint = aimProvider.GetAimHitPoint(); Vector3 intendedDirection = (targetPoint - firePoint.position).normalized; Quaternion recoilOffset = recoilHandler.GetCurrentRecoilOffsetRotation(); Quaternion spreadOffset = spreadController.GetSpreadOffsetRotation(); Vector3 finalDirection = spreadOffset * recoilOffset * intendedDirection; finalDirection.Normalize(); ProjectileBehavior proj = Instantiate(projPrefab, firePoint.position, Quaternion.LookRotation(finalDirection)); proj.Launch(finalDirection, projItemData.baseShootForce); audioHandler?.PlayShootSound(); muzzleHandler?.Muzzle(); recoilHandler.ApplyRecoil(); spreadController.AddSpread(); }
    private void ConsumeRound() { if (state?.magazine != null && state.magazine.Size > 0) { state.magazine[0].ReduceQuantity(1); } }

    #endregion

    #region Auto Fire
    private void StartAutoFire() { StopAutoFire(); autoFireRoutine = StartCoroutine(AutoFireCoroutine()); }
    private void StopAutoFire() { if (autoFireRoutine != null) { StopCoroutine(autoFireRoutine); autoFireRoutine = null; } }
    private IEnumerator AutoFireCoroutine() { while (true) { if (isReloading) yield break; if (CurrentAmmo > 0) { PerformShot(); ConsumeRound(); yield return new WaitForSeconds(AutoDelay); } else { audioHandler?.PlayDryFire(); yield break; } } }
    #endregion

    #region Cooldown
    private void StartCooldown() { isCooldown = true; CancelInvoke(nameof(EndCooldown)); Invoke(nameof(EndCooldown), AutoDelay); }
    private void EndCooldown() => isCooldown = false;
    #endregion

    #region Reloading Logic
    private void AttemptReload() { if (isReloading) return; if (def?.ammoType == null) { Debug.LogWarning($"[{GetType().Name}] No AmmoType.", this); return; } if (ownerInventory == null) { Debug.LogError($"[{GetType().Name}] No OwnerInventory.", this); return; } if (state?.magazine == null || state.magazine.Size == 0) { Debug.LogError($"[{GetType().Name}] No magazine state.", this); return; } if (CurrentAmmo >= MaxBullets) return; if (!ownerInventory.HasItem(def.ammoType)) { Debug.Log($"[{GetType().Name}] No {def.ammoType.itemName} in inventory."); return; } StartReloadSequence(); }
    private void StartReloadSequence() { StopRunningCoroutines(); reloadRoutine = StartCoroutine(ReloadCoroutine()); }
    private IEnumerator ReloadCoroutine() { isReloading = true; SetADSState(false); adsController?.ForceStopAiming(); audioHandler?.PlayReload(); yield return new WaitForSeconds(def?.reloadTime ?? 1.0f); if (!isReloading) yield break; if (!_isInitialized || state == null || def == null || ownerInventory == null) { Debug.LogError($"[{GetType().Name}] Reload Aborted Post-Wait.", this); FinishReload(); yield break; } int needed = MaxBullets - CurrentAmmo; if (needed > 0) { ownerInventory.Withdraw(def.ammoType, needed, state.magazine[0]); } FinishReload(); }
    private void FinishReload() { isReloading = false; reloadRoutine = null; audioHandler?.OnReloadComplete(); }
    #endregion

    #region Utility Methods
    private bool CanPerformAction() { return _isInitialized && this.enabled; }
    private void StopRunningCoroutines() { StopAutoFire(); if (reloadRoutine != null) { StopCoroutine(reloadRoutine); reloadRoutine = null; } }
    private void SetADSState(bool isADS) { if (_isADS == isADS && _isInitialized) return; _isADS = isADS; recoilHandler?.SetADS(isADS); spreadController?.SetADS(isADS); }
    #endregion
}