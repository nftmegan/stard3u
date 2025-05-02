using UnityEngine;

[System.Serializable]
public class RecoilPattern // Keeping it as a class as requested
{
    [Header("Rotation (°)")]
    public float verticalMin    = 1f;
    public float verticalMax    = 2f;
    public float horizontalMin  = -1f;
    public float horizontalMax  =  1f;
    public float rollMin        = -0.5f;
    public float rollMax        =  0.5f;

    [Header("Translation (units)")]
    [Tooltip("Minimum backward kick (local Z)")]
    public float kickbackMin    = 0.02f;
    [Tooltip("Maximum backward kick (local Z)")]
    public float kickbackMax    = 0.05f;

    [Header("Timing (s)")]
    public float recoilDuration   = 0.1f;
    public float recoveryDuration = 0.2f;

    [Tooltip("Easing curve for both recoil & recovery")]
    public AnimationCurve recoilCurve = AnimationCurve.EaseInOut(0,0,1,1);

    // --- ADD THIS PARAMETERLESS CONSTRUCTOR ---
    // This allows 'new RecoilPattern()' to work again.
    // It initializes fields to their default values (like the field initializers above).
    public RecoilPattern() { }
    // --- END ADDED CONSTRUCTOR ---

    // --- COPY CONSTRUCTOR (Keep this) ---
    // Handles new RecoilPattern(otherPattern)
    public RecoilPattern(RecoilPattern other)
    {
        // Null check for safety, although unlikely if used correctly
        if (other == null) return;

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
        // AnimationCurve is a class, needs deep copy
        this.recoilCurve = (other.recoilCurve != null) ? new AnimationCurve(other.recoilCurve.keys) : new AnimationCurve();
    }
}