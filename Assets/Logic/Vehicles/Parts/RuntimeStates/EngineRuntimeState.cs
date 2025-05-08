using UnityEngine;

[System.Serializable]
public class EngineRuntimeState : IPartRuntimeState { // Implements IPartRuntimeState (which includes ICloneableRuntimeState)

    [field: SerializeField] [field: Range(0f, 100f)]
    public float CurrentDurability { get; set; } = 100f;

    [field: SerializeField] [field: Range(0f, 1f)]
    public float CurrentWear { get; set; } = 0f;

    [SerializeField] public float currentEngineOilLiters = 0f;
    [SerializeField] public float currentCoolantLiters = 0f;
    [SerializeField] public float totalOperatingHours = 0f;
    [SerializeField] public bool isSeized = false;
    [SerializeField] public float currentTemperatureCelsius = 15f;

    public EngineRuntimeState() { } // Needed for serialization

    public IRuntimeState Clone() {
        // Manual field-by-field clone
        return new EngineRuntimeState {
            CurrentDurability = this.CurrentDurability, CurrentWear = this.CurrentWear,
            currentEngineOilLiters = this.currentEngineOilLiters, currentCoolantLiters = this.currentCoolantLiters,
            totalOperatingHours = this.totalOperatingHours, isSeized = this.isSeized,
            currentTemperatureCelsius = this.currentTemperatureCelsius
        };
    }
}