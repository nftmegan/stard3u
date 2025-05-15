using UnityEngine;
using System.Collections;
using System;

public abstract class FirearmBehavior : EquippableBehavior {
    [Header("Core Component References")]
    [SerializeField] private AttachmentController attachmentController;
    [SerializeField] private FirearmAudioHandler audioHandler;
    [SerializeField] private MuzzleFlashHandler muzzleHandler;
    [SerializeField] private RecoilHandler recoilHandler;
    [SerializeField] private ADSController adsController;
    [SerializeField] private SpreadHandler spreadController;
    [SerializeField] protected Transform firePoint;

    // Runtime Data References (set in Initialize)
    private FirearmItemData def;
    private FirearmRuntimeState state;

    // Internal State
    private Coroutine reloadRoutine;
    private Coroutine autoFireRoutine;
    private bool isCooldown = false;
    private bool isReloading = false;
    private float lastShotTime = -1f;
    private bool _isInitialized = false;
    private bool _isADS = false;

    // Removed: _playerGrabController reference is no longer needed here for Store action


    private int MaxBullets => def?.magazineSize ?? 1;
    private float AutoDelay => (def != null && def.fireRate > 0) ? 1f / def.fireRate : 0.1f;
    private bool IsAuto => def != null && def.fireMode == FireMode.Auto;
    private int CurrentAmmo => state?.magazine != null && state.magazine.Size > 0 && !state.magazine[0].IsEmpty() ? state.magazine[0].quantity : 0;


    protected virtual void Awake() {
        // Find components (ensure robust finding)
        attachmentController ??= GetComponent<AttachmentController>();
        audioHandler ??= GetComponentInChildren<FirearmAudioHandler>(true);
        muzzleHandler ??= GetComponentInChildren<MuzzleFlashHandler>(true);
        recoilHandler ??= GetComponentInChildren<RecoilHandler>(true);
        adsController ??= GetComponent<ADSController>();
        spreadController ??= GetComponent<SpreadHandler>();

        // Validate essential components found in Awake
        if (attachmentController == null) Debug.LogError($"[{GetType().Name} on {gameObject.name}] AttachmentController missing!", this);
        if (audioHandler == null) Debug.LogError($"[{GetType().Name} on {gameObject.name}] FirearmAudioHandler missing!", this);
        if (muzzleHandler == null) Debug.LogError($"[{GetType().Name} on {gameObject.name}] MuzzleFlashHandler missing!", this);
        if (recoilHandler == null) Debug.LogError($"[{GetType().Name} on {gameObject.name}] RecoilHandler missing!", this);
        if (adsController == null) Debug.LogError($"[{GetType().Name} on {gameObject.name}] ADSController missing!", this);
        if (spreadController == null) Debug.LogError($"[{GetType().Name} on {gameObject.name}] SpreadController missing!", this);
        if (firePoint == null) Debug.LogError($"[{GetType().Name} on {gameObject.name}] FirePoint missing!", this);
    }

    public override void Initialize(InventoryItem itemInstance, IEquipmentHolder holder, IAimProvider aimProvider) {
        base.Initialize(itemInstance, holder, aimProvider);

        _isInitialized = false; // Reset initialization flag
        StopRunningCoroutines(); // Ensure no old routines are running
        isReloading = false; // Reset reload state

        // Attempt to cast and validate the provided item instance data
        if (this.runtimeItem != null) {
            def = this.runtimeItem.data as FirearmItemData;
            state = this.runtimeItem.runtime as FirearmRuntimeState;

            if (def == null) { Debug.LogError($"[{GetType().Name} on {gameObject.name}] Init ERROR: ItemData is not FirearmItemData!", this); this.enabled = false; return; }
            if (state == null) { Debug.LogError($"[{GetType().Name} on {gameObject.name}] Init ERROR: InventoryItem missing FirearmState!", this); this.enabled = false; return; }
            if (state.magazine == null) { Debug.LogError($"[{GetType().Name} on {gameObject.name}] Init ERROR: FirearmState missing magazine container!", this); this.enabled = false; return; }
            if (state.attachments == null) { Debug.LogError($"[{GetType().Name} on {gameObject.name}] Init ERROR: FirearmState missing attachments container!", this); this.enabled = false; return; }
        } else {
            Debug.LogError($"[{GetType().Name} on {gameObject.name}] Initializing with null InventoryItem from base class. Cannot function as firearm.", this);
            this.enabled = false; return;
        }

        // Validate essential contexts inherited from base class
        if (this.ownerEquipmentHolder == null) { Debug.LogError($"[{GetType().Name} on {gameObject.name}] IEquipmentHolder context (from base) is null!", this); this.enabled = false; return; }
        if (this.ownerAimProvider == null) { Debug.LogError($"[{GetType().Name} on {gameObject.name}] IAimProvider context (from base) is null!", this); this.enabled = false; return; }

        // Re-validate core components (found in Awake) after initialization context is set
        if (attachmentController == null || recoilHandler == null || adsController == null || spreadController == null || firePoint == null || audioHandler == null) {
             Debug.LogError($"[{GetType().Name} on {gameObject.name}] One or more critical firearm components are missing after Initialize. Disabling.", this);
             this.enabled = false; return;
        }

        // Initialize controllers
        attachmentController.Initialize(state, def, recoilHandler, adsController, spreadController);
        SetADSState(false); // Start not aiming

        _isInitialized = true; // Mark as initialized
    }

    protected override void OnEnable() {
        base.OnEnable();
        isCooldown = false;
        // Restore ADS state if the controller still exists and we were initialized
        if (adsController != null && _isInitialized) {
             SetADSState(adsController.IsAiming); // Sync with ADS controller's state
        } else if (_isInitialized) {
            SetADSState(false); // Default to not aiming if controller missing
        }
    }

    protected override void OnDisable() {
        StopRunningCoroutines();
        CancelInvoke();
        isReloading = false;
        if (adsController != null) adsController.ForceStopAiming(); // Ensure ADS stops visually
        SetADSState(false); // Ensure internal state is non-ADS
        _isInitialized = false; // Mark as uninitialized when disabled
        base.OnDisable();
    }

    // --- Input Handlers ---
    public override void OnFire1Down() { if (!CanPerformAction() || isReloading) return; if (IsAuto) StartAutoFire(); else AttemptSingleFire(); }
    public override void OnFire1Up() { StopAutoFire(); }
    public override void OnFire2Down() { if (!CanPerformAction() || isReloading) return; adsController?.StartAiming(); SetADSState(true); }
    public override void OnFire2Up() { if (adsController != null) { adsController.StopAiming(); SetADSState(false); } }
    public override void OnReloadDown() { if (!CanPerformAction()) return; AttemptReload(); }
    // OnStoreDown() is removed from IItemInputReceiver and base EquippableBehavior

    // --- Firing Logic ---
    private void AttemptSingleFire() {
        if (!CanPerformAction() || isCooldown || isReloading) return;
        if (Time.time - lastShotTime < AutoDelay && lastShotTime > 0 && CurrentAmmo > 0) { return; } // Fire rate limit

        if (CurrentAmmo <= 0) {
            audioHandler?.PlayDryFire();
            lastShotTime = Time.time;
        } else {
            PerformShot();
            ConsumeRound();
            StartCooldown();
            lastShotTime = Time.time;
        }
    }

    protected virtual void PerformShot() {
        // Pre-condition checks
        if (!CanPerformAction() || state?.magazine == null || state.magazine.Size == 0 || firePoint == null || ownerAimProvider == null || recoilHandler == null || spreadController == null) {
            Debug.LogWarning($"[{GetType().Name}] PerformShot pre-condition failed.", this); return;
        }
        InventorySlot magSlot = state.magazine[0];
        if (magSlot.IsEmpty()) { audioHandler?.PlayDryFire(); return; } // Check ammo *again* just before firing

        var ammoInvItem = magSlot.item;
        var projData = ammoInvItem?.data as ProjectileItemData;
        if (projData == null || projData.projectilePrefab == null) {
             Debug.LogWarning($"[{GetType().Name}] No projectile data/prefab for ammo '{ammoInvItem?.data?.itemName}'. Dry fire.", this);
             audioHandler?.PlayDryFire(); return;
        }
        if (projData.projectilePrefab.GetComponent<ProjectileBehavior>() == null) {
             Debug.LogError($"[{GetType().Name}] Projectile prefab '{projData.projectilePrefab.name}' missing ProjectileBehavior!", projData.projectilePrefab);
             return; // Don't fire if prefab is invalid
        }

        // Calculate direction
        Ray ray = ownerAimProvider.GetLookRay();
        Vector3 target = ownerAimProvider.GetAimHitPoint();
        Vector3 baseDir = (target - firePoint.position).normalized;
        // Use look direction if target is too close or behind
        if ((target - firePoint.position).sqrMagnitude < 0.1f || Vector3.Dot(baseDir, ray.direction) < 0.1f) {
            baseDir = ray.direction;
        }

        // Apply recoil and spread offsets
        Quaternion recoilOffset = recoilHandler.GetCurrentRecoilOffsetRotation();
        Quaternion spreadOffset = spreadController.GetSpreadOffsetRotation();
        Vector3 finalDir = spreadOffset * recoilOffset * baseDir; // Apply spread first, then recoil visual offset? Or vice versa? Needs testing. Let's try Spread -> Recoil -> BaseDir
        finalDir.Normalize(); // Ensure unit vector

        // Instantiate and Launch
        try {
            ProjectileBehavior p = Instantiate(projData.projectilePrefab, firePoint.position, Quaternion.LookRotation(finalDir)).GetComponent<ProjectileBehavior>();
            if (p != null) {
                p.Launch(finalDir, projData.baseShootForce);
            } else { // Should have been caught earlier, but belt-and-braces
                 Debug.LogError($"[{GetType().Name}] Failed GetComponent<ProjectileBehavior> on instantiated '{projData.projectilePrefab.name}'!", this);
            }
        } catch (Exception e) {
             Debug.LogError($"[{GetType().Name}] Error instantiating projectile '{projData.projectilePrefab.name}': {e.Message}\n{e.StackTrace}", this);
             return; // Stop if instantiation fails
        }


        // Effects
        audioHandler?.PlayShootSound();
        muzzleHandler?.Muzzle();
        recoilHandler.ApplyRecoil();
        spreadController.AddSpread();
    }

    private void ConsumeRound() {
        if (state?.magazine != null && state.magazine.Size > 0 && !state.magazine[0].IsEmpty()) {
            state.magazine[0].ReduceQuantity(1);
            // Optional: Fire an event or update UI directly if needed here
        }
    }
    private void StartAutoFire() { StopAutoFire(); autoFireRoutine = StartCoroutine(AutoFireCoroutine()); }
    private void StopAutoFire() { if (autoFireRoutine != null) { StopCoroutine(autoFireRoutine); autoFireRoutine = null; } }
    private IEnumerator AutoFireCoroutine() {
        while (true) {
            if (!CanPerformAction() || isReloading) yield break;
            if (CurrentAmmo > 0) { PerformShot(); ConsumeRound(); yield return new WaitForSeconds(AutoDelay); }
            else { audioHandler?.PlayDryFire(); yield break; }
        }
    }
    private void StartCooldown() {
        if (IsAuto && def.fireMode == FireMode.Auto) return; // Auto handles its own delay
        isCooldown = true;
        CancelInvoke(nameof(EndCooldown)); // Prevent stacking invokes
        Invoke(nameof(EndCooldown), AutoDelay);
    }
    private void EndCooldown() => isCooldown = false;

    // --- Reloading Logic ---
    private void AttemptReload() {
        if (!CanPerformAction() || isReloading) return; // Already reloading or cannot act
        if (def?.ammoType == null) { Debug.LogWarning($"[{GetType().Name}] Cannot reload: {def?.itemName} missing ammoType.", this); return; }
        if (ownerEquipmentHolder == null) { Debug.LogError($"[{GetType().Name}] Cannot reload: Missing IEquipmentHolder context.", this); return; }
        if (state?.magazine == null || state.magazine.Size == 0) { Debug.LogError($"[{GetType().Name}] Cannot reload: FirearmState magazine container missing.", this); return; }
        if (CurrentAmmo >= MaxBullets) return; // Magazine already full

        // Check if holder actually has the required ammo type
        if (!ownerEquipmentHolder.HasItemInInventory(def.ammoType)) {
            // Debug.Log($"[{GetType().Name}] No '{def.ammoType.itemName}' in inventory.");
            audioHandler?.PlayDryFire(); // Play sound indicating no ammo available
            return;
        }
        // All checks pass, start the reload sequence
        StartReloadSequence();
    }

    private void StartReloadSequence() {
        StopRunningCoroutines(); // Stop firing, etc.
        reloadRoutine = StartCoroutine(ReloadCoroutine());
    }

    private IEnumerator ReloadCoroutine() {
        isReloading = true;
        if (adsController != null) { SetADSState(false); adsController.ForceStopAiming(); } // Force exit ADS
        audioHandler?.PlayReload();
        yield return new WaitForSeconds(def?.reloadTime ?? 1.0f); // Wait for reload duration

        // Re-validate state after yield, as things might have changed (e.g., player died, item unequipped)
        if (!isReloading || !_isInitialized || state?.magazine == null || def == null || ownerEquipmentHolder == null) {
            isReloading = false; // Ensure flag is reset
            reloadRoutine = null;
            yield break; // Abort if state is no longer valid
        }

        // Calculate ammo needed and available
        int needed = MaxBullets - CurrentAmmo;
        ItemContainer mainInv = ownerEquipmentHolder.GetContainerForInventory();
        int available = 0;
        if (mainInv != null) {
            foreach (var s in mainInv.Slots) {
                if (s != null && !s.IsEmpty() && s.item.data == def.ammoType) {
                    available += s.quantity;
                }
            }
        }

        int transfer = Mathf.Min(needed, available); // Amount to actually transfer

        // Perform the transfer
        if (transfer > 0) {
            // Consume from inventory first
            if (ownerEquipmentHolder.RequestConsumeItem(def.ammoType, transfer)) {
                // Add to magazine slot
                InventorySlot magSlot = state.magazine[0];
                if (magSlot.IsEmpty() || magSlot.item?.data != def.ammoType) {
                    // If empty or wrong type, create new item entry
                    magSlot.item = new InventoryItem(def.ammoType);
                    magSlot.quantity = 0; // Ensure quantity starts at 0 before adding
                }
                magSlot.AddQuantity(transfer); // Add the consumed amount
            } else {
                // This should ideally not happen if HasItemInInventory passed and available > 0
                Debug.LogError($"[{GetType().Name}] Failed inventory consumption for {transfer}x'{def.ammoType.itemName}' during reload despite checks!", this);
            }
        }
        // else: No ammo available or needed, just finish the animation/sound timing

        FinishReload(); // Mark reload as complete
    }

    private void FinishReload() {
        isReloading = false;
        reloadRoutine = null;
        audioHandler?.OnReloadComplete();
        // Optional: Auto-enter ADS if player was holding ADS button during reload?
    }

    // --- Utility Methods ---
    private bool CanPerformAction() => _isInitialized && this.enabled && Time.timeScale > 0.01f;
    private void StopRunningCoroutines() {
        StopAutoFire();
        if (reloadRoutine != null) {
            StopCoroutine(reloadRoutine);
            reloadRoutine = null;
            if (isReloading) { isReloading = false; /* Maybe cancel sound? */ }
        }
    }
    private void SetADSState(bool isADS) {
        if (_isADS == isADS && _isInitialized) return;
        _isADS = isADS;
        if (_isInitialized) { // Only update handlers if firearm is ready
            recoilHandler?.SetADS(isADS);
            spreadController?.SetADS(isADS);
        }
    }
}