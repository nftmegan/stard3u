// In Assets/Scripts/Items/Behaviours/Bow/BowBehavior.cs (or your path)
using UnityEngine;
using System.Collections;

public class BowBehavior : EquippableBehavior {
    [Header("Component References")]
    [SerializeField] private Transform arrowSpawnPoint;
    [SerializeField] private BowDrawEffect drawEffect;
    [SerializeField] private BowAudioHandler audioHandler;
    [SerializeField] private BowArrowVisualEffect arrowVisualEffect;

    [Header("Bow Settings")]
    [SerializeField] private float maxPullTime = 1.5f;
    [SerializeField] private float minShootThreshold = 0.85f;
    [SerializeField] private float baseShootForce = 40f;
    [SerializeField] private float shotCooldown = 0.5f;
    [SerializeField] private float drawTimeToSlow = 0.1f;

    [Header("Ammo Settings")]
    [Tooltip("Assign the specific Arrow ItemData SO required by this bow.")]
    [SerializeField] private ItemData requiredArrowData;

    // Runtime State
    private bool isDrawing, isCooldown, isHoldingAim;
    private float drawStartTime, currentPowerPercent;

    // Required Player Components
    private MyCharacterController characterController; // Still needed for slow walk

    // CORRECTED Initialize signature
    public override void Initialize(InventoryItem itemInstance, IEquipmentHolder holder, IAimProvider aimProvider) {
        // Call base.Initialize FIRST to set runtimeItem, ownerEquipmentHolder, ownerAimProvider
        base.Initialize(itemInstance, holder, aimProvider);

        // --- Validate contexts critical for BowBehavior specifically ---
        if (this.runtimeItem == null || this.runtimeItem.data == null) {
            Debug.LogError($"[BowBehavior on {gameObject.name}] Initialize ERROR: Null ItemInstance or ItemData from base class! Cannot function.", this);
            this.enabled = false;
            return;
        }

        // --- Find Character Controller (using PlayerManager or fallback if holder is PlayerManager) ---
        // ownerEquipmentHolder is already set by base.Initialize
        if (this.ownerEquipmentHolder is PlayerManager playerManagerHolder) { // If the holder itself is PlayerManager
            characterController = playerManagerHolder.CharacterController;
        } else if (this.ownerEquipmentHolder is PlayerInventory playerInventoryHolder) { // Common case
            PlayerManager pm = playerInventoryHolder.GetComponentInParent<PlayerManager>();
            if (pm != null) characterController = pm.CharacterController;
        }
        
        if (characterController == null) { // Fallback if not found via holder's hierarchy
            characterController = GetComponentInParent<PlayerManager>()?.CharacterController ?? GetComponentInParent<MyCharacterController>();
        }

        if (characterController == null) {
             Debug.LogWarning($"[BowBehavior on {gameObject.name}] CharacterController not found - slow walk effect disabled. Holder: {this.ownerEquipmentHolder?.GetType().Name}", this);
        }
        // --- End Find Character Controller ---

        drawEffect ??= GetComponent<BowDrawEffect>();
        audioHandler ??= GetComponent<BowAudioHandler>();
        arrowVisualEffect ??= GetComponentInChildren<BowArrowVisualEffect>(true);

        if (arrowSpawnPoint == null) Debug.LogError($"[BowBehavior on {gameObject.name}] Arrow Spawn Point missing!", this);
        if (requiredArrowData == null) Debug.LogError($"[BowBehavior on {gameObject.name}] Required Arrow Data missing!", this);

        ResetState();
        arrowVisualEffect?.SetArrowVisibility(false);
    }

    // ... (Rest of BowBehavior: Update, Input Handlers, Core Logic, Cooldown, Helpers, OnDisable - All remain the same) ...
    // Ensure methods like Shoot use `this.ownerEquipmentHolder` and `this.ownerAimProvider`.
    private void Update() { if (!isDrawing) return; float drawDur = Time.time - drawStartTime; currentPowerPercent = Mathf.Clamp01(drawDur / maxPullTime); drawEffect?.UpdateDraw(currentPowerPercent); arrowVisualEffect?.UpdateDraw(currentPowerPercent); if (currentPowerPercent >= 1.0f) audioHandler?.StartLoop(); }
    public override void OnFire2Down() { isHoldingAim = true; if (!isDrawing && !isCooldown) TryStartDrawing(); }
    public override void OnFire2Up() { isHoldingAim = false; if (isDrawing) CancelDrawing(); }
    public override void OnFire1Down() { if (isDrawing && currentPowerPercent >= minShootThreshold) Shoot(currentPowerPercent); }
    private void TryStartDrawing() { if (requiredArrowData == null || ownerEquipmentHolder == null) return; if (!ownerEquipmentHolder.HasItemInInventory(requiredArrowData)) { audioHandler?.PlayDryFire(); return; } StartDrawing(); }
    private void StartDrawing() { isDrawing = true; drawStartTime = Time.time; currentPowerPercent = 0f; CancelInvoke(nameof(ApplySlowWalk)); Invoke(nameof(ApplySlowWalk), drawTimeToSlow); audioHandler?.PlayDrawStart(); arrowVisualEffect?.SetArrowVisibility(true); }
    private void CancelDrawing() { if (!isDrawing) return; CancelInvoke(nameof(ApplySlowWalk)); characterController?.ForceSlowWalk(false); ResetState(); drawEffect?.StopDraw(); audioHandler?.StopLoop(); audioHandler?.PlayCancel(); arrowVisualEffect?.SetArrowVisibility(false); arrowVisualEffect?.UpdateDraw(0f); }
    private void Shoot(float powerPercent) {
        if (ownerEquipmentHolder == null || ownerAimProvider == null || arrowSpawnPoint == null || requiredArrowData == null) {
            Debug.LogError($"[Bow on {gameObject.name}] Cannot shoot: Missing required contexts or data. Holder: {ownerEquipmentHolder != null}, Aimer: {ownerAimProvider != null}", this);
            StartCooldown(); ResetState(); return;
        }
        if (!ownerEquipmentHolder.RequestConsumeItem(requiredArrowData, 1)) {
            Debug.LogWarning($"[Bow on {gameObject.name}] Failed to consume arrow before shooting.", this);
            CancelDrawing(); return;
        }
        CancelInvoke(nameof(ApplySlowWalk));
        characterController?.ForceSlowWalk(false);
        ResetState();
        drawEffect?.StopDraw();
        audioHandler?.PlayRelease();
        audioHandler?.StopLoop();
        arrowVisualEffect?.SetArrowVisibility(false);
        arrowVisualEffect?.UpdateDraw(0f);
        GameObject projectilePrefabToSpawn = null;
        var arrowDataSpecific = requiredArrowData as ArrowItemData;
        if (arrowDataSpecific != null && arrowDataSpecific.projectilePrefab != null) {
            projectilePrefabToSpawn = arrowDataSpecific.projectilePrefab;
        }
        else if (requiredArrowData.worldPrefab != null && requiredArrowData.worldPrefab.GetComponentInChildren<ProjectileBehavior>() != null) {
             projectilePrefabToSpawn = requiredArrowData.worldPrefab;
        }
        if (projectilePrefabToSpawn == null) {
            Debug.LogError($"[Bow on {gameObject.name}] Arrow Data '{requiredArrowData.itemName}' missing a suitable projectile prefab!", this);
            StartCooldown(); return;
        }
        Ray lookRay = ownerAimProvider.GetLookRay();
        Vector3 target = ownerAimProvider.GetAimHitPoint();
        Vector3 dir = (target - arrowSpawnPoint.position).normalized;
        if ((target - arrowSpawnPoint.position).sqrMagnitude < 0.1f || Vector3.Dot(dir, lookRay.direction) < 0.1f) dir = lookRay.direction;
        float force = baseShootForce * powerPercent;
        GameObject projGO = Instantiate(projectilePrefabToSpawn, arrowSpawnPoint.position, Quaternion.LookRotation(dir));
        ProjectileBehavior projBehavior = projGO.GetComponent<ProjectileBehavior>();
        if (projBehavior != null) {
            projBehavior.Launch(dir, force);
        } else {
             Debug.LogError($"[Bow on {gameObject.name}] Instantiated projectile '{projectilePrefabToSpawn.name}' is missing ProjectileBehavior script!", projGO);
        }
        StartCooldown();
    }
    private void StartCooldown() { isCooldown = true; CancelInvoke(nameof(EndCooldown)); Invoke(nameof(EndCooldown), shotCooldown); }
    private void EndCooldown() { isCooldown = false; if (isHoldingAim) TryStartDrawing(); }
    private void ApplySlowWalk() { if (isDrawing && characterController != null) characterController.ForceSlowWalk(true); }
    private void ResetState() { isDrawing = false; drawStartTime = 0f; currentPowerPercent = 0f; }
    protected override void OnDisable() { base.OnDisable(); CancelInvoke(); if (characterController != null) characterController.ForceSlowWalk(false); ResetState(); isHoldingAim = false; isCooldown = false; drawEffect?.StopDraw(); audioHandler?.StopLoop(); arrowVisualEffect?.SetArrowVisibility(false); arrowVisualEffect?.UpdateDraw(0f); }
    public override void OnFire1Hold() { } public override void OnFire1Up() { } public override void OnFire2Hold() { } public override void OnUtilityDown() { } public override void OnUtilityUp() { } public override void OnReloadDown() { }
}