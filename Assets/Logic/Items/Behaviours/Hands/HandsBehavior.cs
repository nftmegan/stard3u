using UnityEngine;
using System.Collections; // Keep for potential Coroutines if Invoke is replaced

// Removed namespace

// Inherits from EquippableBehavior to receive input calls
public class HandsBehavior : EquippableBehavior
{
    [Header("Hands Settings")]
    [Tooltip("Time in seconds before the player can punch again.")]
    [SerializeField] private float punchCooldown = 0.5f;
    [Tooltip("How far the basic interaction/grab check reaches.")]
    [SerializeField] private float grabReach = 2.0f; // Currently only used for debug log

    // Runtime state
    private bool _canPunch = true;
    private Coroutine _punchCooldownCoroutine; // Store coroutine to prevent issues if disabled

    // Initialize is called by EquipmentController.
    // We don't need to do anything special here for Hands,
    // but the base Initialize will still run (and might log the null inventory error, which is okay here).
    public override void Initialize(InventoryItem itemInstance, ItemContainer inventory)
    {
        base.Initialize(itemInstance, inventory); // Call base implementation
        // We expect itemInstance to be null for Hands
        // We expect inventory to be potentially null or the player's inventory
        // Reset state specific to Hands when equipped
        _canPunch = true;
        StopCooldownCoroutine(); // Ensure no leftover cooldowns
    }

    // --- Input Handling ---

    public override void OnFire1Down() // Primary action (Punch)
    {
        if (_canPunch)
        {
            PerformPunch();
        }
    }

    public override void OnFire2Down() // Secondary action (Could be block, aim grab, etc.)
    {
         // Currently unused for basic hands, add logic if needed
         // Debug.Log("[Hands] Secondary Action");
    }

    public override void OnUtilityDown() // Utility action (Grab/Interact)
    {
        TryGrab();
    }

    // --- Actions ---

    private void PerformPunch()
    {
        Debug.Log("[Hands] Punch!");
        // TODO: Implement actual punch logic here:
        // - Play punch animation
        // - Play punch sound
        // - Perform a physics overlap check or short raycast in front
        // - Apply damage/force to hit objects

        _canPunch = false; // Prevent immediate re-punching
        // Start cooldown using a coroutine for better management
        StopCooldownCoroutine(); // Stop previous if any still running
       _punchCooldownCoroutine = StartCoroutine(PunchCooldownCoroutine());
    }

    private void TryGrab()
    {
        Debug.Log($"[Hands] Trying to grab within {grabReach}m...");
        // TODO: Implement grab logic:
        // - Raycast or OverlapSphere forward from camera/player using grabReach
        // - Check if hit object has an "IGrabbable" component
        // - If yes, call a method on the IGrabbable or attach it via Physics Joint
    }

    // --- Cooldown ---

    private IEnumerator PunchCooldownCoroutine()
    {
        yield return new WaitForSeconds(punchCooldown);
        _canPunch = true;
        _punchCooldownCoroutine = null; // Clear reference when done
    }

    private void StopCooldownCoroutine()
    {
        if (_punchCooldownCoroutine != null)
        {
            StopCoroutine(_punchCooldownCoroutine);
            _punchCooldownCoroutine = null;
        }
    }

    // --- Cleanup ---
    // Stop coroutine if the object is disabled while waiting for cooldown
    protected override void OnDisable() // Changed private/protected to protected override
    {
        base.OnDisable(); // Optional: Call base class logic if it exists/might exist
        StopCooldownCoroutine();
        _canPunch = true;
    }

    // --- Unused Input Overrides (keep empty or remove if base class allows) ---
    // public override void OnFire1Hold() { }
    // public override void OnFire1Up() { }
    // public override void OnFire2Hold() { }
    // public override void OnFire2Up() { }
    // public override void OnUtilityUp() { }
    // public override void OnReloadDown() { }
}