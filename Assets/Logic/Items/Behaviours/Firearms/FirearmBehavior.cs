using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public abstract class FirearmBehavior : EquippableBehavior
{
    // --- Inspector Refs ---
    [Header("Component References")]
    [SerializeField] protected Transform firePoint;
    [SerializeField] protected FirearmAudioHandler audioHandler;
    [SerializeField] protected RecoilHandler recoilHandler;
    [SerializeField] protected MuzzleFlashHandler muzzleHandler;
    [SerializeField] protected ADSController adsController;

    [Header("ADS Setup")]
    [SerializeField] protected Transform defaultWeaponAimPointTransform;
    [SerializeField] private Vector3 defaultCameraAnchorOffset = new Vector3(0, 0, 0.15f);

    [Header("Attachment Setup")]
    [SerializeField] private Transform attachmentsRoot;

    // --- Cached Data & State ---
    private FirearmItemData def;
    private FirearmState state;
    private IAimProvider aimProvider; // Found in Awake/Start

    // --- Runtime State ---
    private Coroutine reloadRoutine;
    private Coroutine autoFireRoutine;
    private bool isCooldown;
    private bool isReloading;
    private float lastShotTime;

    private Transform _currentWeaponAimPoint;
    private Vector3 _currentCameraAnchorOffset;
    private RecoilPattern _effectiveRecoilPattern = new RecoilPattern(); // Initialize default

    // --- Attachment Management ---
    private readonly Dictionary<int, GameObject> _activeAttachmentInstances = new Dictionary<int, GameObject>();
    private readonly List<AttachmentStatModifier> _activeStatModifiers = new List<AttachmentStatModifier>();
    private readonly Dictionary<string, Transform> _mountPoints = new Dictionary<string, Transform>();
    private bool _isInitialized = false; // Track if Initialize has run

    // --- Quick Access Properties ---
    private int MaxBullets => def?.magazineSize ?? 1;
    private float AutoDelay => (def != null && def.fireRate > 0) ? 1f / def.fireRate : 1f;
    private bool IsAuto => def != null && def.fireMode == FireMode.Auto;
    public int CurrentAmmo => state?.magazine != null && state.magazine.Size > 0 ? state.magazine[0].quantity : 0;

    // --- Unity Lifecycle ---

    protected virtual void Awake()
    {
        // Find essential components that don't depend on ItemData/State yet
        aimProvider = GetComponentInParent<IAimProvider>();
        // Null checks for components assigned in Inspector happen during Initialize/validation steps.
        if (aimProvider == null) Debug.LogError($"[{GetType().Name}] IAimProvider not found in parent hierarchy!", this);

        // Cache mount points ONCE if attachmentsRoot is valid
        // Do this in Awake as the hierarchy should be stable.
        _mountPoints.Clear();
        if (attachmentsRoot != null)
        {
            foreach (Transform t in attachmentsRoot)
            {
                if (t != null && !string.IsNullOrEmpty(t.gameObject.tag))
                {
                    _mountPoints[t.gameObject.tag] = t;
                }
            }
        }
        else
        {
             Debug.LogWarning($"[{GetType().Name}] Attachments Root is not assigned in Awake. Attachments require manual assignment or will fail.", this);
        }
    }

    // Initialize is called EXTERNALLY by EquipmentController when this specific item is equipped
    public override void Initialize(InventoryItem inv, ItemContainer ownerInv)
    {
        base.Initialize(inv, ownerInv); // Sets runtimeItem and ownerInventory in base

        // --- Reset core state flags ---
        _isInitialized = false; // Mark as needing full setup
        isReloading = false;
        isCooldown = false;
        // Stop any lingering coroutines from previous activations (belt-and-braces)
        StopRunningCoroutines();

        // --- Get Data and State from InventoryItem ---
        if (inv != null)
        {
            def = inv.data as FirearmItemData;
            state = inv.runtime as FirearmState;

            // Validate essential data
            if (def == null) { Debug.LogError($"[{GetType().Name}] Initialized with ItemData that is not FirearmItemData!", this); return; }
            if (state == null) { Debug.LogError($"[{GetType().Name}] Initialized with InventoryItem missing FirearmState runtime payload!", this); return; }
        }
        else
        {
             Debug.LogWarning($"[{GetType().Name}] Initialized with null InventoryItem. Behavior may be limited (e.g., unarmed).", this);
             // If truly unarmed, def and state will remain null. Subsequent logic should handle this.
             // Clear any visuals from a previous state if this instance is being reused for unarmed.
             ClearAllAttachmentsVisuals(); // Explicitly clear visuals if becoming unarmed
             def = null;
             state = null;
             // Update handlers with default/null state if needed
             UpdateHandlersWithCurrentState();
             _isInitialized = true; // Mark as initialized (even if unarmed)
             return; // Exit if unarmed
        }

        // --- Validate Component References (Assigned in Inspector) ---
        if (adsController == null) Debug.LogError($"[{GetType().Name} '{def.itemName}'] ADSController reference is missing!", this);
        if (recoilHandler == null) Debug.LogError($"[{GetType().Name} '{def.itemName}'] RecoilHandler reference is missing!", this);
        // AttachmentsRoot warning is in Awake
        if (defaultWeaponAimPointTransform == null) Debug.LogWarning($"[{GetType().Name} '{def.itemName}'] Default Weapon Aim Point Transform is not assigned!", this);


        // --- Setup based on new data/state ---

        // Subscribe to state changes (Unsubscribe first ensures no duplicates if Initialize is called multiple times)
        if (state?.attachments != null)
        {
            state.attachments.OnSlotChanged -= HandleAttachmentSlotChanged;
            state.attachments.OnSlotChanged += HandleAttachmentSlotChanged;
        }

        // Perform the full refresh: Clears old visuals, instantiates new ones based on 'state',
        // calculates stats, updates ADS/Recoil handlers.
        RefreshAllAttachments();

        _isInitialized = true; // Mark initialization complete for this item setup
    }


    // OnEnable is called AFTER Awake, and every time SetActive(true) is used.
    protected override void OnEnable()
    {
        base.OnEnable(); // Call base if it does anything

        // If Initialize has already run at least once (meaning def/state should be set
        // for this specific instance's logical item), we need to ensure the visuals
        // and handlers are synchronized with that state upon re-activation.
        if (_isInitialized)
        {
            // Refresh everything to match the current 'def' and 'state'
            // This handles the case where the cached instance is re-enabled.
            RefreshAllAttachments();

             // Ensure ADS is reset correctly on enable
             if(adsController != null) {
                 adsController.ForceStopAiming();
                 // ADSController's SetWeaponAimPoint/Offset is called inside RefreshAllAttachments
             }
        }
        // If _isInitialized is false, it means Initialize hasn't run yet for this
        // specific equip action. Initialize will be called shortly by EquipmentController
        // and handle the setup, including the first RefreshAllAttachments.
    }

    // OnDisable is called when SetActive(false) is used or the object is destroyed.
    protected override void OnDisable()
    {
        // --- Stop Active Processes ---
        StopRunningCoroutines();
        CancelInvoke(); // Cancel any pending invokes (like EndCooldown)

        // --- Unsubscribe ---
        // Important to check 'state' as it might be null if initialized as unarmed
        if (state?.attachments != null)
        {
            state.attachments.OnSlotChanged -= HandleAttachmentSlotChanged;
        }

        // --- Cleanup Visuals ---
        // Destroy the instantiated attachment GameObjects when disabled.
        // This prevents them lingering when the object is cached but inactive.
        ClearAllAttachmentsVisuals();

        // --- Reset State ---
        adsController?.ForceStopAiming();
        isReloading = false; // Ensure reload is cancelled visually/logically if disabled mid-reload

        // We don't reset _isInitialized here, as the instance might be re-enabled later.
        // We keep def/state associated with this instance until Initialize is called with a new item.

        base.OnDisable(); // Call base if it does anything
    }

    // --- Core Logic Methods (Shoot, Reload, etc.) ---
    // (Keep the implementations for OnFire1Down, OnFire1Up, OnFire2Down, OnFire2Up, OnReloadDown,
    // SingleFire, Shoot, ConsumeRound, AutoFireCoroutine, StartCooldown, EndCooldown, Reload, ReloadCoroutine
    // from the previous complete script - they don't need significant changes for this issue)
    public override void OnFire1Down() { if (isReloading || def == null || !_isInitialized) return; if (IsAuto) { if (autoFireRoutine != null) StopCoroutine(autoFireRoutine); autoFireRoutine = StartCoroutine(AutoFireCoroutine()); } else { if (isCooldown) return; SingleFire(); } }
    public override void OnFire1Up() { if (autoFireRoutine != null) { StopCoroutine(autoFireRoutine); autoFireRoutine = null; } }
    public override void OnFire2Down() { if (isReloading || adsController == null || def == null || !_isInitialized) return; adsController.StartAiming(); }
    public override void OnFire2Up() { adsController?.StopAiming(); }
    public override void OnReloadDown() { if (def == null || state == null || !_isInitialized) return; Reload(); }
    private void SingleFire() { if (Time.time - lastShotTime < AutoDelay) return; if (CurrentAmmo <= 0) { audioHandler?.PlayDryFire(); } else { Shoot(); ConsumeRound(); StartCooldown(); } lastShotTime = Time.time; }
    protected virtual void Shoot() { if (state?.magazine == null || state.magazine.Size == 0 || state.magazine[0].IsEmpty()) return; var projItemData = state.magazine[0].item?.data as ProjectileItemData; if (projItemData == null || projItemData.projectilePrefab == null) { Debug.LogWarning($"[{GetType().Name}] No projectile prefab for loaded ammo: {state.magazine[0].item?.data?.itemName ?? "NULL"}", this); return; } var projPrefab = projItemData.projectilePrefab; if (firePoint == null || aimProvider == null) { Debug.LogError($"[{GetType().Name}] Missing FirePoint or IAimProvider!", this); return; } Vector3 target = aimProvider.GetAimHitPoint(); Vector3 dir = (target - firePoint.position).normalized; var proj = Instantiate(projPrefab, firePoint.position, Quaternion.LookRotation(dir)); proj.Launch(dir, projItemData.baseShootForce); audioHandler?.PlayShootSound(); muzzleHandler?.Muzzle(); recoilHandler?.ApplyRecoil(); }
    private void ConsumeRound() { if (state?.magazine != null && state.magazine.Size > 0) { state.magazine[0].ReduceQuantity(1); } }
    private IEnumerator AutoFireCoroutine() { while (!isReloading && CurrentAmmo > 0) { Shoot(); ConsumeRound(); yield return new WaitForSeconds(AutoDelay); } if (CurrentAmmo <= 0 && !isReloading) { audioHandler?.PlayDryFire(); } autoFireRoutine = null; }
    private void StartCooldown() { isCooldown = true; CancelInvoke(nameof(EndCooldown)); Invoke(nameof(EndCooldown), AutoDelay); }
    private void EndCooldown() => isCooldown = false;
    private void Reload() { if (isReloading) return; if (def?.ammoType == null) { Debug.LogWarning($"[{GetType().Name}] Cannot reload: AmmoType undefined.", this); return; } if (ownerInventory == null) { Debug.LogError($"[{GetType().Name}] Cannot reload: OwnerInventory null.", this); return; } if (state?.magazine == null || state.magazine.Size == 0) { Debug.LogError($"[{GetType().Name}] Cannot reload: No magazine state.", this); return; } int currentInMag = state.magazine[0].quantity; if (currentInMag >= MaxBullets) return; int needed = MaxBullets - currentInMag; if (needed <= 0) return; if (!ownerInventory.HasItem(def.ammoType)) { Debug.Log($"[{GetType().Name}] No {def.ammoType.itemName} in inventory."); return; } if (reloadRoutine != null) StopCoroutine(reloadRoutine); reloadRoutine = StartCoroutine(ReloadCoroutine()); }
    private IEnumerator ReloadCoroutine() { isReloading = true; adsController?.ForceStopAiming(); if (autoFireRoutine != null) { StopCoroutine(autoFireRoutine); autoFireRoutine = null; } audioHandler?.PlayReload(); yield return new WaitForSeconds(def?.reloadTime ?? 1.0f); if (!isReloading) yield break; int needed = MaxBullets - CurrentAmmo; if (needed > 0 && ownerInventory != null && def?.ammoType != null && state?.magazine != null) { ownerInventory.Withdraw(def.ammoType, needed, state.magazine[0]); } isReloading = false; reloadRoutine = null; audioHandler?.OnReloadComplete(); }


    // --- Attachment Logic ---

    private void HandleAttachmentSlotChanged(int slotIndex)
    {
        if (!_isInitialized) return; // Don't react if not fully initialized

        if (slotIndex < 0) // Structural change
        {
            RefreshAllAttachments();
        }
        else // Single slot changed
        {
            RefreshAttachment(slotIndex);
        }
    }

    // Clears existing visuals and rebuilds all based on current 'state'
    private void RefreshAllAttachments()
    {
        // 1. Clear existing visuals and runtime lists
        ClearAllAttachmentsVisuals();

        // 2. Instantiate and Apply based on current state.attachments
        if (state?.attachments != null) // Ensure state and attachments container exist
        {
            for (int i = 0; i < state.attachments.Slots.Length; i++)
            {
                var slot = state.attachments.Slots[i];
                if (slot == null || slot.IsEmpty()) continue;

                AttachmentItemData attachmentData = slot.item.data as AttachmentItemData;
                InstantiateSingleAttachment(attachmentData, i); // Refactored instantiation
            }
        }

        // 3. Calculate combined stats and update handlers (ADS/Recoil)
        UpdateHandlersWithCurrentState();
    }

    // Cleans up and potentially replaces attachment in a single slot
    private void RefreshAttachment(int slotIndex)
    {
        if (state?.attachments == null || slotIndex < 0 || slotIndex >= state.attachments.Slots.Length) return; // Bounds check

        // 1. Teardown Old Visuals for this specific slot
        ClearSingleAttachmentVisuals(slotIndex);

        // 2. Instantiate New Visuals if an item exists in the slot now
        var slot = state.attachments.Slots[slotIndex];
        AttachmentItemData attachmentData = slot?.item?.data as AttachmentItemData;
        if (attachmentData != null) // Only instantiate if there's data
        {
             InstantiateSingleAttachment(attachmentData, slotIndex);
        }

        // 3. Recalculate stats and update handlers
        UpdateHandlersWithCurrentState(); // Always update after any change
    }

    // Helper to instantiate and configure a single attachment
    private GameObject InstantiateSingleAttachment(AttachmentItemData data, int slotIndex)
    {
         if (data == null || data.attachmentPrefab == null) return null;

        if (_mountPoints.TryGetValue(data.mountPointTag, out var mount))
        {
            // Instantiate
            var go = Instantiate(data.attachmentPrefab, mount.position, mount.rotation, mount);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            _activeAttachmentInstances[slotIndex] = go; // Store reference

            // Apply Effects (find and store modifiers)
            var statModifier = go.GetComponentInChildren<AttachmentStatModifier>(true);
            if (statModifier != null)
            {
                _activeStatModifiers.Add(statModifier);
            }
            // Apply other effects (lasers, etc.) if needed

            return go;
        }
        else
        {
            Debug.LogWarning($"[{GetType().Name}] Mount point tag '{data.mountPointTag}' not found for attachment '{data.itemName}' slot {slotIndex}.", this);
            return null;
        }
    }


    // Helper to destroy all attachment GOs and clear tracking lists
    private void ClearAllAttachmentsVisuals()
    {
        // Use ToList() to avoid modifying collection while iterating
        foreach (var kvp in _activeAttachmentInstances.ToList())
        {
            ClearSingleAttachmentVisuals(kvp.Key);
        }
        // Ensure lists are cleared even if dictionary was already empty
         _activeAttachmentInstances.Clear();
         _activeStatModifiers.Clear();
    }

    // Helper to destroy visuals and remove tracking for a single slot
    private void ClearSingleAttachmentVisuals(int slotIndex)
    {
        if (_activeAttachmentInstances.TryGetValue(slotIndex, out var instanceGO))
        {
            if (instanceGO != null)
            {
                // Remove associated effects FIRST
                var statModifier = instanceGO.GetComponentInChildren<AttachmentStatModifier>(true);
                if (statModifier != null)
                {
                    _activeStatModifiers.Remove(statModifier);
                }
                // Remove other effects if tracked

                // Then destroy the GameObject
                Destroy(instanceGO);
            }
            // Remove from dictionary even if GO was already null
             _activeAttachmentInstances.Remove(slotIndex);
        }
    }

    // Central place to calculate stats and update dependent components (ADS, Recoil)
    private void UpdateHandlersWithCurrentState()
    {
         RecalculateEffectiveStats(); // Calculates _effectiveRecoilPattern
         UpdateCurrentAimPointAndOffset(); // Calculates _currentWeaponAimPoint/_currentCameraAnchorOffset

         // Update Recoil Handler
         if (recoilHandler != null)
         {
             recoilHandler.SetRecoilPattern(_effectiveRecoilPattern);
         }

         // Update ADS Controller
         if (adsController != null)
         {
             adsController.SetWeaponAimPoint(_currentWeaponAimPoint);
             adsController.SetCameraAnchorOffset(_currentCameraAnchorOffset);
         }
    }

    // Calculates the effective recoil pattern based on 'def' and active modifiers
    private void RecalculateEffectiveStats()
    {
        // Start with base pattern (handle null def for unarmed state)
        _effectiveRecoilPattern = (def?.baseRecoilPattern != null) ? new RecoilPattern(def.baseRecoilPattern) : new RecoilPattern();

        // Apply modifiers
        foreach (var modifier in _activeStatModifiers)
        {
            if(modifier == null) continue;
            _effectiveRecoilPattern.verticalMin *= modifier.verticalRecoilMultiplier;
            _effectiveRecoilPattern.verticalMax *= modifier.verticalRecoilMultiplier;
            _effectiveRecoilPattern.horizontalMin *= modifier.horizontalRecoilMultiplier;
            _effectiveRecoilPattern.horizontalMax *= modifier.horizontalRecoilMultiplier;
            _effectiveRecoilPattern.rollMin *= modifier.rollRecoilMultiplier;
            _effectiveRecoilPattern.rollMax *= modifier.rollRecoilMultiplier;
            _effectiveRecoilPattern.kickbackMin *= modifier.kickbackRecoilMultiplier;
            _effectiveRecoilPattern.kickbackMax *= modifier.kickbackRecoilMultiplier;
            _effectiveRecoilPattern.recoveryDuration *= modifier.recoveryDurationMultiplier;
            _effectiveRecoilPattern.recoveryDuration = Mathf.Max(0.01f, _effectiveRecoilPattern.recoveryDuration);
        }
        // Apply other stat calculations here (ADS speed multiplier, etc.)
    }

    // Determines the active aim point and offset based on attached sights
    private void UpdateCurrentAimPointAndOffset()
    {
        Transform activeSightAimPoint = null;
        AttachmentItemData sightData = null;

        // Check active instances ONLY if state exists (handles unarmed case)
        if (state?.attachments != null) {
             foreach (var kvp in _activeAttachmentInstances)
            {
                GameObject instance = kvp.Value;
                int slotIdx = kvp.Key;

                // Basic safety checks
                if (instance == null || slotIdx >= state.attachments.Slots.Length) continue;

                var item = state.attachments.Slots[slotIdx].item;
                var data = item?.data as AttachmentItemData;

                if (data?.attachmentType == AttachmentType.Sight)
                {
                    var aimPointComponent = instance.GetComponentInChildren<AttachmentAimPoint>(true);
                    if (aimPointComponent != null)
                    {
                        activeSightAimPoint = aimPointComponent.transform;
                        sightData = data;
                        break; // Found first sight
                    }
                    else { Debug.LogWarning($"Sight '{data.itemName}' missing AttachmentAimPoint component!", instance); }
                }
            }
        }


        // Apply found sight or defaults
        if (activeSightAimPoint != null && sightData != null)
        {
            _currentWeaponAimPoint = activeSightAimPoint;
            _currentCameraAnchorOffset = sightData.overrideCameraAnchorOffset ? sightData.customCameraAnchorOffset : defaultCameraAnchorOffset;
        }
        else
        {
            // Use weapon's defaults (check if default exists)
            _currentWeaponAimPoint = defaultWeaponAimPointTransform; // Can be null if not assigned
            _currentCameraAnchorOffset = defaultCameraAnchorOffset;
        }

        // Final safety check - log error if no aim point could be determined AT ALL
        if (_currentWeaponAimPoint == null) {
              Debug.LogError($"[{GetType().Name} '{def?.itemName ?? "Unknown"}'] CRITICAL: Could not determine weapon aim point (No sight attached AND DefaultWeaponAimPointTransform is null/not assigned!) ADS will likely fail.", this);
         }
    }

    // --- Utility ---
    private void StopRunningCoroutines()
    {
        if (reloadRoutine != null) { StopCoroutine(reloadRoutine); reloadRoutine = null; }
        if (autoFireRoutine != null) { StopCoroutine(autoFireRoutine); autoFireRoutine = null; }
    }
}