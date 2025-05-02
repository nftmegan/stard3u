using UnityEngine;

/// <summary>
/// Defines the parameters for weapon spread (random inaccuracy).
/// </summary>
[System.Serializable]
public class SpreadPattern
{
    [Header("Angle Settings (Degrees)")]
    [Tooltip("Minimum spread angle when weapon is idle/fully recovered.")]
    [Range(0f, 5f)]
    public float baseSpreadAngle = 0.5f;

    [Tooltip("Maximum spread angle the weapon can reach.")]
    [Range(0.1f, 15f)]
    public float maxSpreadAngle = 5.0f;

    [Header("Bloom & Recovery")]
    [Tooltip("Angle increase added per shot fired.")]
    [Range(0f, 5f)]
    public float spreadIncreasePerShot = 0.75f;

    [Tooltip("Degrees per second the spread angle recovers towards the base spread.")]
    [Range(0.1f, 50f)]
    public float spreadRecoveryRate = 10.0f;

    [Tooltip("Delay (in seconds) after firing before spread starts recovering.")]
    [Range(0f, 1f)]
    public float spreadRecoveryDelay = 0.15f;

    // Constructor for defaults
    public SpreadPattern() { }

    // Copy constructor (important since it's likely used in ItemData)
    public SpreadPattern(SpreadPattern other)
    {
        this.baseSpreadAngle = other.baseSpreadAngle;
        this.maxSpreadAngle = other.maxSpreadAngle;
        this.spreadIncreasePerShot = other.spreadIncreasePerShot;
        this.spreadRecoveryRate = other.spreadRecoveryRate;
        this.spreadRecoveryDelay = other.spreadRecoveryDelay;
    }

    // Optional: Add validation logic here if needed
    public void Validate()
    {
        baseSpreadAngle = Mathf.Max(0f, baseSpreadAngle);
        maxSpreadAngle = Mathf.Max(baseSpreadAngle, maxSpreadAngle); // Max cannot be less than base
        spreadIncreasePerShot = Mathf.Max(0f, spreadIncreasePerShot);
        spreadRecoveryRate = Mathf.Max(0.1f, spreadRecoveryRate);
        spreadRecoveryDelay = Mathf.Max(0f, spreadRecoveryDelay);
    }
}