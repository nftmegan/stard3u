using UnityEngine;

/// <summary>
/// Attach this component to attachment prefabs (like grips, stocks, barrels)
/// that modify the base weapon's statistics.
/// </summary>
public class AttachmentStatModifier : MonoBehaviour
{
    [Header("ADS")]
    [Tooltip("Multiplier for ADS transition speed. > 1 speeds up, < 1 slows down.")]
    public float adsSpeedMultiplier = 1.0f;

    [Header("Recoil Multipliers (< 1 Reduces Recoil)")]
    [Tooltip("Multiplier for vertical recoil magnitude.")]
    public float verticalRecoilMultiplier = 1.0f;

    [Tooltip("Multiplier for horizontal recoil magnitude (both min/max).")]
    public float horizontalRecoilMultiplier = 1.0f;

    // --- ADD THESE MISSING PUBLIC VARIABLES ---
    [Tooltip("Multiplier for rotational roll magnitude.")]
    public float rollRecoilMultiplier = 1.0f; // Default to 1 (no change)

    [Tooltip("Multiplier for positional kickback magnitude.")]
    public float kickbackRecoilMultiplier = 1.0f; // Default to 1

    [Tooltip("Multiplier for how quickly the weapon recovers from recoil (affects recovery duration).")]
    public float recoveryDurationMultiplier = 1.0f; // Default to 1
    // --- END ADDED VARIABLES ---

    // Add other potential modifiers:
    // public float hipFireAccuracyMultiplier = 1.0f;
    // public float weaponSwayMultiplier = 1.0f;
    // public float reloadSpeedMultiplier = 1.0f;
    // public float effectiveRangeModifier = 0f; // Additive change?
}