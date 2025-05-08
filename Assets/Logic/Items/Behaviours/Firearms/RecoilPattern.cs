// In Assets/Scripts/Items/Data/Weapons/Shared/RecoilPattern.cs (Example Path)
using UnityEngine;

/// <summary>
/// Defines the characteristics of a single recoil impulse applied after firing.
/// Used by RecoilHandler to generate visual weapon kick.
/// </summary>
[System.Serializable]
public class RecoilPattern {

    #region Inspector Fields
    [Header("Rotation Kick (Degrees)")]
    [Tooltip("Minimum upward rotation.")]
    public float verticalMin = 1.0f;
    [Tooltip("Maximum upward rotation.")]
    public float verticalMax = 2.0f;
    [Tooltip("Minimum horizontal rotation (negative = left).")]
    public float horizontalMin = -1.0f;
    [Tooltip("Maximum horizontal rotation (positive = right).")]
    public float horizontalMax = 1.0f;
    [Tooltip("Minimum roll rotation (negative = left barrel up).")]
    public float rollMin = -0.5f;
    [Tooltip("Maximum roll rotation (positive = right barrel up).")]
    public float rollMax = 0.5f;

    [Header("Positional Kick (Local Space Units)")]
    [Tooltip("Minimum backward kick distance along the local Z axis.")]
    public float kickbackMin = 0.02f;
    [Tooltip("Maximum backward kick distance along the local Z axis.")]
    public float kickbackMax = 0.05f;
    // Add lateral/vertical kick if needed:
    // public float lateralKickRange = 0.01f; // Max left/right positional kick
    // public float verticalKickRange = 0.005f; // Max up/down positional kick

    [Header("Timing (Seconds)")]
    [Tooltip("How long the initial kick-back phase lasts.")]
    [Min(0.01f)] public float recoilDuration = 0.1f;
    [Tooltip("How long it takes for the weapon to return to its resting position after the kick.")]
    [Min(0.01f)] public float recoveryDuration = 0.2f;

    [Header("Easing")]
    [Tooltip("Curve defining the interpolation for both the kick-back and recovery phases (Time 0->1 maps to movement 0->1). Should ideally start at 0,0 and end at 1,1.")]
    public AnimationCurve recoilCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    #endregion

    #region Constructors
    /// <summary>
    /// Default constructor. Initializes with default values specified in field initializers.
    /// Required for direct instantiation like 'new RecoilPattern()'.
    /// </summary>
    public RecoilPattern() { }

    /// <summary>
    /// Copy constructor. Creates a deep copy of another RecoilPattern instance.
    /// </summary>
    public RecoilPattern(RecoilPattern other) {
        if (other == null) return; // Safety check

        // Copy value types
        this.verticalMin = other.verticalMin;
        this.verticalMax = other.verticalMax;
        this.horizontalMin = other.horizontalMin;
        this.horizontalMax = other.horizontalMax;
        this.rollMin = other.rollMin;
        this.rollMax = other.rollMax;
        this.kickbackMin = other.kickbackMin;
        this.kickbackMax = other.kickbackMax;
        this.recoilDuration = other.recoilDuration;
        this.recoveryDuration = other.recoveryDuration;

        // Deep copy the AnimationCurve (it's a reference type)
        this.recoilCurve = (other.recoilCurve != null)
            ? new AnimationCurve(other.recoilCurve.keys) // Create new curve with copied keys
            : new AnimationCurve(); // Create a default empty curve if source is null
    }
    #endregion

    #region Validation
    /// <summary>
    /// Clamps values to reasonable ranges. Called by FirearmItemData.OnValidate.
    /// </summary>
    public void Validate() {
        // Ensure Min <= Max for ranges
        verticalMax = Mathf.Max(verticalMin, verticalMax);
        horizontalMax = Mathf.Max(horizontalMin, horizontalMax);
        rollMax = Mathf.Max(rollMin, rollMax);
        kickbackMax = Mathf.Max(kickbackMin, kickbackMax);

        // Ensure positive durations
        recoilDuration = Mathf.Max(0.01f, recoilDuration); // Prevent zero or negative duration
        recoveryDuration = Mathf.Max(0.01f, recoveryDuration);

        // Optional: Add warnings for extreme values if desired
        // if (verticalMax > 15f) Debug.LogWarning("RecoilPattern: High verticalMax value detected.");
    }
    #endregion
}