using UnityEngine;

public enum FireMode { Semi, Auto, Burst }

// Assuming WeaponItemData exists and inherits from ItemData.
// If not, change "WeaponItemData" to "ItemData".
// You might need to add a 'using' directive if your base classes are in a namespace.
// using YourNamespace;

/// <summary>
/// ScriptableObject defining the static properties of a firearm weapon.
/// Contains data for firing mechanics, ammo, recoil, attachments, and presentation.
/// Inherits from WeaponItemData (or ItemData directly).
/// </summary>
[CreateAssetMenu(menuName = "Items/Weapon/Firearm")]
public class FirearmItemData : WeaponItemData // Or inherit from ItemData if WeaponItemData doesn't exist
{
    /* ────────── Static weapon tuning (designer edits) ────────── */

    [Header("Firing Mechanism")]
    [Tooltip("Firing mode (Semi-auto, Full-auto, Burst)")]
    public FireMode fireMode = FireMode.Semi;

    [Tooltip("Shots per second. Used for Auto fire delay and Semi-auto cooldown.")]
    [Min(0.1f)] // Prevent zero or negative fire rate
    public float fireRate = 10f; // rounds-per-second

    [Tooltip("Time in seconds it takes to complete a reload cycle (animation/action duration).")]
    [Min(0.1f)] // Ensure a minimum reload time
    public float reloadTime = 1.5f;

    [Header("Magazine & Ammunition")]
    [Tooltip("Maximum number of rounds the magazine can hold.")]
    [Min(1)] // Magazine must hold at least one round
    public int magazineSize = 12;

    [Tooltip("The ItemData ScriptableObject that represents the required ammunition (e.g., a ProjectileItemData).")]
    public ItemData ammoType; // Reference to the ProjectileItemData or generic Ammo ItemData

    [Header("Recoil")]
    [Tooltip("Defines the base recoil characteristics (kick, recovery) of this weapon before attachment modifiers.")]
    public RecoilPattern baseRecoilPattern = new RecoilPattern(); // Embed the RecoilPattern configuration

    [Header("Attachments")]
    [Tooltip("Number of available slots for attaching modifications (sights, grips, etc.).")]
    [Min(0)]
    public int attachmentSlots = 4; // How many attachment components can this weapon have

    [Header("Presentation")]
    [Tooltip("The prefab representing the weapon's in-world/viewmodel appearance and behavior. Should contain a FirearmBehavior script (or derived type).")]
    public GameObject prefab; // The runtime GameObject prefab (e.g., RifleWeapon prefab)

    [Tooltip("Optional: Sprite used for UI elements like inventory slots or HUD icons.")]
    public Sprite hudSprite; // Visual representation in UI

    /* ────────── Factory: build runtime InventoryItem ────────── */

    /// <summary>
    /// Creates a brand-new InventoryItem instance representing this specific firearm type.
    /// This instance will contain its own runtime state (magazine container, attachments container, durability).
    /// Call this when initially adding a new instance of this firearm to an inventory.
    /// </summary>
    /// <returns>A new InventoryItem configured for this firearm type.</returns>
    public InventoryItem CreateInventoryItem()
    {
        // 1. Create the runtime state payload specific to firearms.
        //    This includes containers for the magazine and attachments, plus initial durability.
        var payload = new FirearmState(attachmentSlots, 100); // Initial durability 100

        // 2. Create the InventoryItem itself.
        //    It links the static definition (this ScriptableObject) with the unique runtime state (payload).
        var item = new InventoryItem(this, payload); // 'this' refers to this FirearmItemData instance

        return item;
    }

    // Optional: Add validation logic that runs in the Editor when values are changed.
    private void OnValidate()
    {
        // Clamp potentially problematic values to safe ranges in the editor
        if (fireRate <= 0)
        {
            fireRate = 0.1f; // Prevent division by zero or nonsensical rates
            Debug.LogWarning($"[{this.name}] Fire rate was non-positive, clamped to 0.1.", this);
        }
        if (magazineSize < 1)
        {
            magazineSize = 1;
            Debug.LogWarning($"[{this.name}] Magazine size was less than 1, clamped to 1.", this);
        }
        if (reloadTime < 0.1f) // Ensure some minimum reload time
        {
            reloadTime = 0.1f;
             Debug.LogWarning($"[{this.name}] Reload time was less than 0.1, clamped to 0.1.", this);
        }

        // Check for common configuration mistakes
        if (ammoType == null)
        {
            Debug.LogWarning($"[{this.name}] Ammo Type is not assigned. Reloading will not work.", this);
        }
        // Optional: Check if ammoType is specifically ProjectileItemData if that's required
        // else if (!(ammoType is ProjectileItemData)) {
        //     Debug.LogWarning($"[{this.name}] Ammo type assigned is not a ProjectileItemData. Ensure this is intended.", this);
        // }

        if (prefab == null)
        {
            //Debug.LogError($"[{this.name}] Weapon Prefab is not assigned!", this);
        }
        // Check if the assigned prefab actually contains the required behavior script
        else if (prefab.GetComponentInChildren<FirearmBehavior>(true) == null) // 'true' checks inactive children too
        {
             Debug.LogError($"The assigned Prefab '{prefab.name}' for [{this.name}] does not contain a FirearmBehavior (or derived class like PistolBehavior) component in its hierarchy! The weapon will not function.", this);
        }
    }
}