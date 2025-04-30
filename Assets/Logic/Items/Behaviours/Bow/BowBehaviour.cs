using UnityEngine;
using System.Collections;

// Ensure this script inherits from your base EquippableBehavior
// which provides Initialize(InventoryItem inv, ItemContainer ownerInv)
public class BowBehavior : EquippableBehavior
{
    // Component References (Set in Inspector or found in Awake/Initialize)
    [Header("Component References")]
    [SerializeField] private Transform arrowSpawnPoint;
    [SerializeField] private BowDrawEffect drawEffect; // Optional visual/audio components
    [SerializeField] private BowAudioHandler audioHandler;
    [SerializeField] private BowArrowVisualEffect arrowVisualEffect;

    [Header("Bow Settings")]
    [SerializeField] private float maxPullTime = 1.5f;
    [SerializeField] private float minShootThreshold = 0.85f; // Min power % to fire
    [SerializeField] private float baseShootForce = 40f;
    [SerializeField] private float shotCooldown = 0.5f;
    [SerializeField] private float drawTimeToSlow = 0.1f; // Delay before slowing starts

    [Header("Ammo Settings")]
    [SerializeField] private ArrowItemData requiredArrowData; // Assign the specific Arrow SO

    // Runtime State
    private bool isDrawing = false;
    private bool isCooldown = false;
    private bool isHoldingAim = false; // Renamed from isHoldingRightClick for clarity
    private float drawStartTime = 0f;
    private float currentPowerPercent = 0f; // Renamed from currentPullForce for clarity

    // Required Parent Components
    private IAimProvider aimProvider;
    private MyCharacterController characterController;
    // 'ownerInventory' (ItemContainer) is provided by the base Initialize method

    // We call the base Initialize, which stores 'itemInstance' and 'ownerInventory'
    public override void Initialize(InventoryItem inv, ItemContainer ownerInv)
    {
        base.Initialize(inv, ownerInv); // CRITICAL: Call the base method

        // Find required components via parent PlayerManager
        PlayerManager playerManager = GetComponentInParent<PlayerManager>();
        if (playerManager != null)
        {
            aimProvider = playerManager.Look; // PlayerLook implements IAimProvider
            characterController = playerManager.CharacterController;
        }
        else
        {
            Debug.LogError("BowBehavior could not find PlayerManager!", this);
        }

        // Validate references
        if (aimProvider == null) Debug.LogError("BowBehavior could not find IAimProvider!", this);
        if (characterController == null) Debug.LogError("BowBehavior could not find MyCharacterController!", this);
        if (arrowSpawnPoint == null) Debug.LogError("BowBehavior needs Arrow Spawn Point assigned!", this);
        if (requiredArrowData == null) Debug.LogError("BowBehavior needs Required Arrow Data assigned!", this);

        // Find optional components on this GameObject
        drawEffect ??= GetComponent<BowDrawEffect>();
        audioHandler ??= GetComponent<BowAudioHandler>();
        arrowVisualEffect ??= GetComponent<BowArrowVisualEffect>();

        // Reset state on initialize/equip
        ResetState();
        arrowVisualEffect?.SetArrowVisibility(false);
    }

    private void Update()
    {
        if (!isDrawing) return;

        float drawDuration = Time.time - drawStartTime;
        currentPowerPercent = Mathf.Clamp01(drawDuration / maxPullTime);

        // Update visuals/audio based on power
        drawEffect?.UpdateDraw(currentPowerPercent);
        arrowVisualEffect?.UpdateDraw(currentPowerPercent);

        if (currentPowerPercent >= minShootThreshold)
        {
            audioHandler?.StartLoop(); // Play 'fully drawn' sound loop
        }
    }

    // --- Input Handling Methods (Called by EquipmentController -> PlayerManager) ---

    public override void OnFire2Down() // Aim button down
    {
        isHoldingAim = true;
        // Start drawing only if aiming and not on cooldown/already drawing
        if (!isDrawing && !isCooldown)
        {
            TryStartDrawing();
        }
    }

    public override void OnFire2Up() // Aim button up
    {
        isHoldingAim = false;
        // Cancel draw if the aim button is released
        if (isDrawing)
        {
            CancelDrawing();
        }
    }

    public override void OnFire1Down() // Fire button down
    {
        // Shoot only if drawing and minimum power threshold is met
        if (isDrawing && currentPowerPercent >= minShootThreshold)
        {
            Shoot(currentPowerPercent);
        }
        // Don't shoot if not drawing or not drawn enough
    }

    // --- Core Bow Logic ---

    private void TryStartDrawing()
    {
        // Check dependencies
        if (requiredArrowData == null || ownerInventory == null)
        {
            Debug.LogWarning("[Bow] Missing required data or inventory reference.");
            return;
        }

        // Check for ammo using the ownerInventory passed via Initialize
        if (!ownerInventory.HasItem(requiredArrowData))
        {
            Debug.Log("[Bow] No arrows available.");
            audioHandler?.PlayDryFire(); // Play no ammo sound
            return;
        }

        // If all checks pass, start drawing
        StartDrawing();
    }

    private void StartDrawing()
    {
        isDrawing = true;
        drawStartTime = Time.time;
        currentPowerPercent = 0f;

        // Start slowing down shortly after beginning draw
        Invoke(nameof(ApplySlowWalk), drawTimeToSlow);

        audioHandler?.PlayDrawStart();
        arrowVisualEffect?.SetArrowVisibility(true); // Show the arrow nocked
    }

    private void CancelDrawing()
    {
        if (!isDrawing) return; // Already cancelled

        CancelInvoke(nameof(ApplySlowWalk)); // Prevent delayed slow walk if cancelled early
        characterController?.ForceSlowWalk(false); // Ensure slow walk stops

        ResetState(); // Reset drawing variables

        // Reset visuals/audio
        drawEffect?.StopDraw();
        audioHandler?.StopLoop(); // Stop 'fully drawn' sound
        audioHandler?.PlayCancel(); // Optional cancel sound
        arrowVisualEffect?.SetArrowVisibility(false);
        arrowVisualEffect?.UpdateDraw(0f); // Reset visual draw state
    }

    private void Shoot(float powerPercent)
    {
        // --- Consume Arrow ---
        // Use ownerInventory and TryConsumeItem
        if (ownerInventory == null || !ownerInventory.TryConsumeItem(requiredArrowData, 1))
        {
            Debug.LogWarning("[Bow] Failed to consume arrow before shooting.");
            CancelDrawing(); // Cancel if we suddenly can't consume the arrow
            return;
        }

        // --- State Reset & Visuals/Audio ---
        CancelInvoke(nameof(ApplySlowWalk)); // Stop delayed slow walk if firing
        characterController?.ForceSlowWalk(false); // Stop slow walk on fire
        ResetState(); // Reset drawing variables

        drawEffect?.StopDraw();
        audioHandler?.PlayRelease();
        audioHandler?.StopLoop();
        arrowVisualEffect?.SetArrowVisibility(false);
        arrowVisualEffect?.UpdateDraw(0f);

        // --- Projectile Logic ---
        if (aimProvider == null || arrowSpawnPoint == null || requiredArrowData.projectilePrefab == null)
        {
            Debug.LogError("[Bow] Missing references needed to shoot!");
            StartCooldown(); // Still trigger cooldown even if spawning fails
            return;
        }

        Vector3 targetPoint = aimProvider.GetAimHitPoint();
        Vector3 direction = (targetPoint - arrowSpawnPoint.position).normalized;
        float launchForce = baseShootForce * powerPercent; // Scale force by draw power

        // Instantiate and launch
        // Assuming projectilePrefab is a component like 'Projectile' with a Launch method
        var projectileInstance = Instantiate(requiredArrowData.projectilePrefab,
                                             arrowSpawnPoint.position,
                                             Quaternion.LookRotation(direction));

        // Assuming projectileInstance has a Launch method
        projectileInstance.Launch(direction, launchForce); // Adjust Launch method signature as needed

        StartCooldown();
    }

    // --- Cooldown Logic ---

    private void StartCooldown()
    {
        isCooldown = true;
        // Use coroutine for better control than Invoke if needed later
        Invoke(nameof(EndCooldown), shotCooldown);
    }

    private void EndCooldown()
    {
        isCooldown = false;
        // If player is still holding aim after cooldown, automatically start drawing again
        if (isHoldingAim)
        {
            TryStartDrawing();
        }
    }

    // --- Helpers & State Management ---

    private void ApplySlowWalk()
    {
        // Only apply slow walk if currently drawing
        if (isDrawing && characterController != null)
        {
            characterController.ForceSlowWalk(true);
        }
    }

    private void ResetState()
    {
        isDrawing = false;
        drawStartTime = 0f;
        currentPowerPercent = 0f;
        // Do NOT reset isHoldingAim or isCooldown here
    }

    // Called when the weapon is unequipped (e.g., via base class OnDisable or similar)
    protected override void OnDisable() // Or OnUnequipped if your base class has that
    {
        base.OnDisable(); // Call base OnDisable if it exists

        // Ensure state is fully reset when unequipped
        CancelInvoke(); // Cancel any pending Invoke calls (like ApplySlowWalk, EndCooldown)
        if (characterController != null) characterController.ForceSlowWalk(false);
        ResetState();
        isHoldingAim = false;
        isCooldown = false;

        // Reset visuals/audio
        drawEffect?.StopDraw();
        audioHandler?.StopLoop();
        arrowVisualEffect?.SetArrowVisibility(false);
        arrowVisualEffect?.UpdateDraw(0f);
    }


    // Implement unused required methods from base/interface if needed
    public override void OnFire1Hold() { }
    public override void OnFire1Up() { }
    public override void OnUtilityDown() { }
    public override void OnUtilityUp() { }
    public override void OnReloadDown() { }
}