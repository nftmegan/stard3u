using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Applies visual recoil motion and exposes the current rotational offset for aim calculation.
/// Considers base pattern, attachment multipliers, and ADS state.
/// </summary>
public class RecoilHandler : MonoBehaviour
{
    // --- Configuration (Set by AttachmentController) ---
    private RecoilPattern basePattern = new RecoilPattern();
    private float currentMagnitudeMultiplier = 1.0f;
    private float currentRecoverySpeedMultiplier = 1.0f;
    private float adsVisualRecoilMultiplier = 1.0f; // Multiplier when ADS

    // --- Runtime State ---
    [Header("Randomness")]
    [Range(0f, 100f)] public float randomSeed = 0f;

    private Vector3 originalEuler;
    private Vector3 rotOffset = Vector3.zero;
    private Vector3 rotVelocity = Vector3.zero;
    private Vector3 originalPos;
    private Vector3 posOffset = Vector3.zero;
    private Vector3 posVelocity = Vector3.zero;

    private bool isCurrentlyADS = false; // Track ADS state

    private void Awake()
    {
        originalEuler = transform.localEulerAngles;
        originalPos = transform.localPosition;
        if (randomSeed <= 0f) randomSeed = Random.Range(1f, 100f);
        basePattern ??= new RecoilPattern();
    }

    /// <summary>
    /// Sets the base, unmodified recoil pattern.
    /// </summary>
    public void SetBaseRecoilPattern(RecoilPattern newBasePattern)
    {
        this.basePattern = (newBasePattern != null) ? new RecoilPattern(newBasePattern) : new RecoilPattern();
    }

    /// <summary>
    /// Sets the combined multipliers from attachments and the ADS multiplier from definition.
    /// </summary>
    public void SetRecoilModifiers(float magnitudeMultiplier, float recoverySpeedMultiplier, float adsMultiplier)
    {
        this.currentMagnitudeMultiplier = Mathf.Max(0f, magnitudeMultiplier);
        this.currentRecoverySpeedMultiplier = Mathf.Max(0.01f, recoverySpeedMultiplier);
        this.adsVisualRecoilMultiplier = Mathf.Clamp01(adsMultiplier); // Store ADS multiplier
    }

    /// <summary>
    /// Updates the ADS state, affecting recoil magnitude. Called by FirearmBehavior.
    /// </summary>
    public void SetADS(bool isADS)
    {
        isCurrentlyADS = isADS;
    }

    /// <summary>
    /// Applies a single visual recoil kick based on the base pattern and current modifiers/ADS state.
    /// </summary>
    public void ApplyRecoil()
    {
        if (basePattern == null) return;

        // Calculate effective magnitude considering attachments AND ADS state
        float effectiveMagnitude = currentMagnitudeMultiplier;
        if (isCurrentlyADS)
        {
            effectiveMagnitude *= adsVisualRecoilMultiplier; // Apply ADS reduction
        }

        // --- Calculate Random Kick Values ---
        // Vertical component (Pitch)
        float vOff = RandomPattern(basePattern.verticalMin * effectiveMagnitude, basePattern.verticalMax * effectiveMagnitude);
        // Horizontal component (Yaw)
        float hOff = RandomPattern(basePattern.horizontalMin * effectiveMagnitude, basePattern.horizontalMax * effectiveMagnitude);
        // Roll component (Tilt)
        float rOff = RandomPattern(basePattern.rollMin * effectiveMagnitude, basePattern.rollMax * effectiveMagnitude);

        // --- Apply Rotation Offset (Corrected for Standard Axes) ---
        // X controls Roll, Y controls Yaw, Z controls Pitch
        // Negative Z rotation pitches UP
        rotOffset += new Vector3(rOff, hOff, -vOff);
        //                      ^Roll ^Yaw  ^Pitch (Up)

        // --- Positional Kick ---
        float zOff = RandomPattern(basePattern.kickbackMin * effectiveMagnitude, basePattern.kickbackMax * effectiveMagnitude);
        // Kickback is usually along the local forward/backward axis (Z)
        posOffset += new Vector3(0f, 0f, -zOff); // Still standard backward kick along local Z
    }

    /// <summary>
    /// Gets the current rotational offset caused by recoil. Used by FirearmBehavior for aim calculation.
    /// </summary>
    /// <returns>Quaternion representing the current visual rotation offset.</returns>
    public Quaternion GetCurrentRecoilOffsetRotation()
    {
        // Convert the Euler offset to a Quaternion
        return Quaternion.Euler(rotOffset);
    }


    private void Update()
    {
        if (basePattern == null) return;

        // Calculate effective recovery duration
        float effectiveRecoveryDuration = basePattern.recoveryDuration * currentRecoverySpeedMultiplier;
        effectiveRecoveryDuration = Mathf.Max(0.01f, effectiveRecoveryDuration);

        // Recover rotation
        rotOffset = Vector3.SmoothDamp(rotOffset, Vector3.zero, ref rotVelocity, effectiveRecoveryDuration);
        transform.localEulerAngles = originalEuler + rotOffset;

        // Recover position
        posOffset = Vector3.SmoothDamp(posOffset, Vector3.zero, ref posVelocity, effectiveRecoveryDuration);
        transform.localPosition = originalPos + posOffset;
    }

    private float RandomPattern(float min, float max)
    {
         return Random.Range(min, max);
    }
}