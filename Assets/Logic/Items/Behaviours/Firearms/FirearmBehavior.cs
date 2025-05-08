// In Assets/Scripts/Items/Behaviours/Firearms/FirearmBehavior.cs
using UnityEngine;
using System.Collections;

public abstract class FirearmBehavior : EquippableBehavior {
    [Header("Core Component References")]
    [SerializeField] private AttachmentController attachmentController;
    [SerializeField] private FirearmAudioHandler audioHandler;
    [SerializeField] private MuzzleFlashHandler muzzleHandler;
    [SerializeField] private RecoilHandler recoilHandler;
    [SerializeField] private ADSController adsController;
    [SerializeField] private SpreadHandler spreadController;
    [SerializeField] protected Transform firePoint;
    
    private FirearmItemData def;
    private FirearmRuntimeState state;

    private Coroutine reloadRoutine;
    private Coroutine autoFireRoutine;
    private bool isCooldown = false;
    private bool isReloading = false;
    private float lastShotTime = -1f;
    private bool _isInitialized = false;
    private bool _isADS = false;

    private int MaxBullets => def?.magazineSize ?? 1;
    private float AutoDelay => (def != null && def.fireRate > 0) ? 1f / def.fireRate : 0.1f;
    private bool IsAuto => def != null && def.fireMode == FireMode.Auto;
    private int CurrentAmmo => state?.magazine != null && state.magazine.Size > 0 && !state.magazine[0].IsEmpty() ? state.magazine[0].quantity : 0;


    protected virtual void Awake() {
        attachmentController ??= GetComponent<AttachmentController>();
        audioHandler ??= GetComponentInChildren<FirearmAudioHandler>(true);
        muzzleHandler ??= GetComponentInChildren<MuzzleFlashHandler>(true);
        recoilHandler ??= GetComponentInChildren<RecoilHandler>(true);
        adsController ??= GetComponent<ADSController>();
        spreadController ??= GetComponent<SpreadHandler>();

        if (attachmentController == null) Debug.LogError($"[{GetType().Name} on {gameObject.name}] AttachmentController missing!", this);
        // ... (other component null checks from previous version)
    }

    // CORRECTED Initialize signature
    public override void Initialize(InventoryItem itemInstance, IEquipmentHolder holder, IAimProvider aimProvider) {
        base.Initialize(itemInstance, holder, aimProvider); // Sets runtimeItem, ownerEquipmentHolder, ownerAimProvider

        _isInitialized = false;
        StopRunningCoroutines();
        isReloading = false;

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
        
        // Ensure contexts from base are valid before proceeding
        if (this.ownerEquipmentHolder == null) { Debug.LogError($"[{GetType().Name} on {gameObject.name}] IEquipmentHolder context (from base) is null!", this); this.enabled = false; return; }
        if (this.ownerAimProvider == null) { Debug.LogError($"[{GetType().Name} on {gameObject.name}] IAimProvider context (from base) is null!", this); this.enabled = false; return; }

        // Check its own components again (Awake tried, this is a final check post-base-init)
        if (attachmentController == null || recoilHandler == null || adsController == null || spreadController == null || firePoint == null || audioHandler == null) {
             Debug.LogError($"[{GetType().Name} on {gameObject.name}] One or more critical firearm components (AttachmentCtrl, Recoil, ADS, Spread, FirePoint, Audio) are missing on the prefab. Disabling. Check Awake and Inspector.", this);
             this.enabled = false; return;
        }

        attachmentController.Initialize(state, def, recoilHandler, adsController, spreadController);
        SetADSState(false);
        _isInitialized = true;
    }

    // ... (Rest of FirearmBehavior: OnEnable, OnDisable, input handlers, Firing, Reloading, Utility methods - All remain the same) ...
    // Ensure methods use `this.ownerEquipmentHolder` and `this.ownerAimProvider` for context.
    protected override void OnEnable() {
        base.OnEnable();
        isCooldown = false;
        if (adsController != null && _isInitialized) {
             SetADSState(adsController.IsAiming);
        } else if (_isInitialized) {
            SetADSState(false);
        }
    }

    protected override void OnDisable() {
        StopRunningCoroutines();
        CancelInvoke();
        isReloading = false;
        if (adsController != null) adsController.ForceStopAiming();
        SetADSState(false);
        _isInitialized = false;
        base.OnDisable();
    }

    public override void OnFire1Down() { if (!CanPerformAction() || isReloading) return; if (IsAuto) StartAutoFire(); else AttemptSingleFire(); }
    public override void OnFire1Up() { StopAutoFire(); }
    public override void OnFire2Down() { if (!CanPerformAction() || isReloading) return; adsController?.StartAiming(); SetADSState(true); }
    public override void OnFire2Up() { if (adsController != null) { adsController.StopAiming(); SetADSState(false); } }
    public override void OnReloadDown() { if (!CanPerformAction()) return; AttemptReload(); }

    private void AttemptSingleFire() {
        if (!CanPerformAction() || isCooldown || isReloading) return;
        if (Time.time - lastShotTime < AutoDelay && lastShotTime > 0) return;

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
        if (!CanPerformAction() || state?.magazine == null || state.magazine.Size == 0 || firePoint == null || ownerAimProvider == null || recoilHandler == null || spreadController == null) { Debug.LogWarning($"[{GetType().Name} on {gameObject.name}] PerformShot pre-condition failed.", this); return; }
        InventorySlot magSlot = state.magazine[0]; if (magSlot.IsEmpty()) { audioHandler?.PlayDryFire(); return; }
        var ammoInvItem = magSlot.item; var projData = ammoInvItem?.data as ProjectileItemData; if (projData == null || projData.projectilePrefab == null) { audioHandler?.PlayDryFire(); return; }

        var prefab = projData.projectilePrefab;
        Ray ray = ownerAimProvider.GetLookRay();
        Vector3 target = ownerAimProvider.GetAimHitPoint();
        Vector3 dir = (target - firePoint.position).normalized;
        if ((target - firePoint.position).sqrMagnitude < 0.1f || Vector3.Dot(dir, ray.direction) < 0.1f) dir = ray.direction;

        Quaternion rOff = recoilHandler.GetCurrentRecoilOffsetRotation();
        Quaternion sOff = spreadController.GetSpreadOffsetRotation();
        Vector3 finalDir = sOff * rOff * dir; finalDir.Normalize();

        ProjectileBehavior p = Instantiate(prefab, firePoint.position, Quaternion.LookRotation(finalDir)).GetComponent<ProjectileBehavior>();
        if (p != null) {
            p.Launch(finalDir, projData.baseShootForce);
        } else {
             Debug.LogError($"[{GetType().Name} on {gameObject.name}] Instantiated projectile '{prefab.name}' missing ProjectileBehavior script!", prefab);
        }

        audioHandler?.PlayShootSound();
        muzzleHandler?.Muzzle();
        recoilHandler.ApplyRecoil();
        spreadController.AddSpread();
    }

    private void ConsumeRound() {
        if (state?.magazine != null && state.magazine.Size > 0 && !state.magazine[0].IsEmpty()) {
            state.magazine[0].ReduceQuantity(1);
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
    private void StartCooldown() { if (IsAuto) return; isCooldown = true; CancelInvoke(nameof(EndCooldown)); Invoke(nameof(EndCooldown), AutoDelay); }
    private void EndCooldown() => isCooldown = false;

    private void AttemptReload() {
        if (!CanPerformAction() || isReloading) return;
        if (def?.ammoType == null) { Debug.LogWarning($"[{GetType().Name} on {gameObject.name}] Cannot reload: {def?.itemName} missing ammoType.", this); return; }
        if (ownerEquipmentHolder == null) { Debug.LogError($"[{GetType().Name} on {gameObject.name}] Cannot reload: Missing IEquipmentHolder.", this); return; } // Use this.ownerEquipmentHolder
        if (state?.magazine == null || state.magazine.Size == 0) { Debug.LogError($"[{GetType().Name} on {gameObject.name}] Cannot reload: Missing magazine state.", this); return; }
        if (CurrentAmmo >= MaxBullets) return;

        if (!ownerEquipmentHolder.HasItemInInventory(def.ammoType)) { // Use this.ownerEquipmentHolder
            audioHandler?.PlayDryFire(); return;
        }
        StartReloadSequence();
    }

    private void StartReloadSequence() {
        StopRunningCoroutines();
        reloadRoutine = StartCoroutine(ReloadCoroutine());
    }

    private IEnumerator ReloadCoroutine() {
        isReloading = true;
        if (adsController != null) { SetADSState(false); adsController.ForceStopAiming(); }
        audioHandler?.PlayReload();
        yield return new WaitForSeconds(def?.reloadTime ?? 1.0f);

        if (!isReloading || !_isInitialized || state?.magazine == null || def == null || ownerEquipmentHolder == null) { // Use this.ownerEquipmentHolder
            isReloading = false; reloadRoutine = null; yield break;
        }

        int needed = MaxBullets - CurrentAmmo;
        ItemContainer mainInv = ownerEquipmentHolder.GetContainerForInventory(); // Use this.ownerEquipmentHolder
        int available = 0;
        if (mainInv != null) foreach (var s in mainInv.Slots) if (!s.IsEmpty() && s.item.data == def.ammoType) available += s.quantity;
        int transfer = Mathf.Min(needed, available);

        if (transfer > 0) {
            if (ownerEquipmentHolder.RequestConsumeItem(def.ammoType, transfer)) { // Use this.ownerEquipmentHolder
                InventorySlot magSlot = state.magazine[0];
                if (magSlot.IsEmpty()) magSlot.item = new InventoryItem(def.ammoType);
                magSlot.AddQuantity(transfer);
            } else Debug.LogError($"[{GetType().Name} on {gameObject.name}] Failed inventory consumption during reload!", this);
        }
        FinishReload();
    }

    private void FinishReload() {
        isReloading = false;
        reloadRoutine = null;
        audioHandler?.OnReloadComplete();
    }
    private bool CanPerformAction() => _isInitialized && this.enabled;
    private void StopRunningCoroutines() {
        StopAutoFire();
        if (reloadRoutine != null) {
            StopCoroutine(reloadRoutine); reloadRoutine = null;
            if (isReloading) { isReloading = false; }
        }
    }
    private void SetADSState(bool isADS) {
        if (_isADS == isADS && _isInitialized) return;
        _isADS = isADS;
        if (_isInitialized) {
            recoilHandler?.SetADS(isADS);
            spreadController?.SetADS(isADS);
        }
    }
}

// For PistolBehavior, it would just be:
// public class PistolBehavior : FirearmBehavior { /* No specific overrides needed if it uses all FirearmBehavior logic */ }