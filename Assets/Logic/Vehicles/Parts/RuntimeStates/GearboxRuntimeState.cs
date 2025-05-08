using UnityEngine;

/// <summary>
/// Runtime state for a Gearbox part. Implements cloning for use with spawners.
/// </summary>
[System.Serializable]
public class GearboxRuntimeState : IPartRuntimeState, ICloneableRuntimeState { // Added ICloneableRuntimeState

    // --- IPartRuntimeState Implementation ---
    [field: SerializeField] [field: Range(0f, 100f)]
    public float CurrentDurability { get; set; } = 100f;

    [field: SerializeField] [field: Range(0f, 1f)]
    public float CurrentWear { get; set; } = 0f;

    // --- Gearbox Specific State ---
    [SerializeField] public int currentGear = 0; // 0 for Neutral
    // Shift state is transient, typically not saved/cloned
    // [SerializeField] public bool isShifting = false;
    // [SerializeField] public float shiftTimer = 0f;

    // Default constructor
    public GearboxRuntimeState() { }

    // --- ICloneableRuntimeState Implementation ---
    /// <summary>
    /// Creates a shallow clone using MemberwiseClone. Safe here because all persistent fields are value types.
    /// </summary>
    /// <returns>A new IRuntimeState object containing a copy of the data.</returns>
    public IRuntimeState Clone() {
        // MemberwiseClone creates a new object and copies value-type fields.
        // If you later add reference types (like ItemContainer), you MUST implement deep cloning here.
        return this.MemberwiseClone() as IRuntimeState;
    }
    // --- End ICloneableRuntimeState ---
}