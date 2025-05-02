using UnityEngine;
using Random = UnityEngine.Random; // Explicitly use UnityEngine.Random

/// <summary>
/// Manages weapon spread (random bullet deviation).
/// Calculates current spread angle based on base stats, firing, ADS, and modifiers.
/// Provides random offsets for bullet trajectory.
/// </summary>
public class SpreadHandler : MonoBehaviour
{
    // --- Configuration (Set via Initialize & SetSpreadModifiers) ---
    private SpreadPattern basePattern = new SpreadPattern();
    private float adsSpreadMultiplier = 1.0f;
    // Combined modifiers from attachments
    private float modBaseSpreadMult = 1.0f;
    private float modMaxSpreadMult = 1.0f;
    private float modSpreadIncreaseMult = 1.0f;
    private float modSpreadRecoveryMult = 1.0f;

    // --- Runtime State ---
    private float currentSpreadAngle = 0f; // Current spread radius in degrees
    private float timeSinceLastShot = float.MaxValue; // Time tracking for recovery delay
    private bool isCurrentlyADS = false;
    private bool _isInitialized = false;

    // --- Properties ---
    public float CurrentSpreadDegrees => currentSpreadAngle; // Expose if needed for UI/debug

    // --- Initialization ---
    public void Initialize(SpreadPattern pattern, float adsMult)
    {
        basePattern = (pattern != null) ? new SpreadPattern(pattern) : new SpreadPattern();
        adsSpreadMultiplier = Mathf.Clamp01(adsMult);

        // Reset modifiers and state
        modBaseSpreadMult = 1.0f;
        modMaxSpreadMult = 1.0f;
        modSpreadIncreaseMult = 1.0f;
        modSpreadRecoveryMult = 1.0f;
        currentSpreadAngle = GetEffectiveBaseSpread(); // Start at base spread
        timeSinceLastShot = float.MaxValue;
        isCurrentlyADS = false;
        _isInitialized = true;
    }

    public void SetSpreadModifiers(float baseMult, float maxMult, float increaseMult, float recoveryMult)
    {
        modBaseSpreadMult = Mathf.Max(0f, baseMult);
        modMaxSpreadMult = Mathf.Max(0f, maxMult);
        modSpreadIncreaseMult = Mathf.Max(0f, increaseMult);
        modSpreadRecoveryMult = Mathf.Max(0.1f, recoveryMult); // Prevent zero recovery rate multiplier

        // Clamp current spread immediately if modifiers changed max/min
        currentSpreadAngle = Mathf.Clamp(currentSpreadAngle, GetEffectiveBaseSpread(), GetEffectiveMaxSpread());
    }

    public void SetADS(bool isADS)
    {
        isCurrentlyADS = isADS;
    }

    // --- Update Logic ---
    private void Update()
    {
        if (!_isInitialized) return;

        timeSinceLastShot += Time.deltaTime;

        // Recover spread if delay has passed
        if (timeSinceLastShot >= basePattern.spreadRecoveryDelay)
        {
            RecoverSpread(Time.deltaTime);
        }
    }

    private void RecoverSpread(float deltaTime)
    {
        float targetSpread = GetEffectiveBaseSpread();
        float recoveryRate = basePattern.spreadRecoveryRate * modSpreadRecoveryMult;

        // Move towards target spread, but don't overshoot
        currentSpreadAngle = Mathf.Max(targetSpread, currentSpreadAngle - recoveryRate * deltaTime);
    }

    // --- Actions ---

    /// <summary>
    /// Increases the current spread due to firing a shot.
    /// </summary>
    public void AddSpread()
    {
        if (!_isInitialized) return;

        float increaseAmount = basePattern.spreadIncreasePerShot * modSpreadIncreaseMult;
        float maxSpread = GetEffectiveMaxSpread();

        currentSpreadAngle = Mathf.Min(maxSpread, currentSpreadAngle + increaseAmount);
        timeSinceLastShot = 0f; // Reset recovery timer
    }

    /// <summary>
    /// Calculates a random rotation offset based on the current spread angle and ADS state.
    /// </summary>
    /// <returns>A Quaternion representing the random deviation.</returns>
    public Quaternion GetSpreadOffsetRotation()
    {
        if (!_isInitialized) return Quaternion.identity;

        float effectiveSpread = currentSpreadAngle;
        if (isCurrentlyADS)
        {
            effectiveSpread *= adsSpreadMultiplier;
        }

        // Ensure minimum spread if calculation results in zero or negative
        effectiveSpread = Mathf.Max(0.01f, effectiveSpread); // Avoid issues with zero spread

        // --- Calculate random point within a circle ---
        // Convert degrees to radians for trig functions
        float spreadRad = Mathf.Deg2Rad * effectiveSpread * 0.5f; // Use half angle for tan

        // Get a random angle and radius (use sqrt for uniform distribution)
        float randomAngle = Random.Range(0f, 2f * Mathf.PI);
        float randomRadius = Mathf.Sqrt(Random.Range(0f, 1f)); // Uniform distribution within the cone base circle

        // Calculate offset using tangent
        float tanSpread = Mathf.Tan(spreadRad);
        float offsetX = randomRadius * Mathf.Cos(randomAngle) * tanSpread;
        float offsetY = randomRadius * Mathf.Sin(randomAngle) * tanSpread;

        // Z component is always 1 (forward direction in local space)
        Vector3 offsetTarget = new Vector3(offsetX, offsetY, 1f);

        // Create rotation that looks towards this offset point from origin
        return Quaternion.LookRotation(offsetTarget.normalized, Vector3.up); // Using LookRotation is convenient
    }

    // --- Helper Methods ---
    private float GetEffectiveBaseSpread()
    {
        return basePattern.baseSpreadAngle * modBaseSpreadMult;
    }

    private float GetEffectiveMaxSpread()
    {
        // Ensure max is always >= base
        return Mathf.Max(GetEffectiveBaseSpread(), basePattern.maxSpreadAngle * modMaxSpreadMult);
    }
}