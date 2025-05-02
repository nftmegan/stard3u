using UnityEngine;

/// <summary>
/// Attach this component to attachment prefabs to modify weapon stats.
/// </summary>
public class AttachmentStatModifier : MonoBehaviour
{
    [Header("ADS")]
    [Tooltip("Multiplier for ADS transition speed. > 1 speeds up, < 1 slows down.")]
    [Range(0.5f, 2f)]
    public float adsSpeedMultiplier = 1.0f;

    [Header("Recoil Modifiers")]
    [Tooltip("Overall multiplier for recoil magnitude (Vertical, Horizontal, Roll, Kickback). < 1 Reduces Recoil magnitude.")]
    [Range(0f, 2f)]
    public float recoilMagnitudeMultiplier = 1.0f;
    [Tooltip("Multiplier for how quickly the weapon recovers from recoil. < 1 Faster Recovery, > 1 Slower Recovery.")]
    [Range(0.5f, 2f)]
    public float recoverySpeedMultiplier = 1.0f;

    // --- ADDED SPREAD MODIFIERS ---
    [Header("Spread Modifiers")]
    [Tooltip("Multiplier for the minimum base spread angle. < 1 Tighter base spread.")]
    [Range(0f, 2f)]
    public float baseSpreadMultiplier = 1.0f;

    [Tooltip("Multiplier for the maximum possible spread angle. < 1 Less max spread.")]
    [Range(0f, 2f)]
    public float maxSpreadMultiplier = 1.0f;

    [Tooltip("Multiplier for how much spread increases per shot. < 1 Less bloom.")]
    [Range(0f, 2f)]
    public float spreadIncreaseMultiplier = 1.0f;

    [Tooltip("Multiplier for how fast spread recovers. < 1 Faster recovery, > 1 Slower recovery.")]
    [Range(0.5f, 2f)]
    public float spreadRecoveryMultiplier = 1.0f;
    // --- END ADDED ---
}