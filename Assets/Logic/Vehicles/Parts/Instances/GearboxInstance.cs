using UnityEngine;

public class GearboxInstance : PartInstance, ITorqueReceiver, ITorqueProvider {

    // --- Internal TRANSIENT Simulation Vars ---
    private float _inputRPM_sim = 0f;
    private float _torqueAppliedBySource_sim = 0f;
    private float _outputRPM_sim = 0f;
    private float _torqueToProvide_sim = 0f;
    private float _currentTotalRatio = 0f;
    private bool _isCurrentlyShifting_internal = false;
    private float _shiftTimer_internal = 0f;

    protected override void Awake() {
        base.Awake();
        // Initialize transient vars
        _inputRPM_sim = 0f; _torqueAppliedBySource_sim = 0f; _outputRPM_sim = 0f;
        _torqueToProvide_sim = 0f; _currentTotalRatio = 0f;
        _isCurrentlyShifting_internal = false; _shiftTimer_internal = 0f;
    }

    /// <summary>
    /// Initialize: Sets up gearbox after base setup.
    /// </summary>
    public override void Initialize(InventoryItem itemInstance, VehicleRoot vehicleRoot) {
        base.Initialize(itemInstance, vehicleRoot);
        if (!this.enabled) return;

        // --- Gearbox Specific Transient Setup ---
        _isCurrentlyShifting_internal = false; // Reset transient shift state
        _shiftTimer_internal = 0f;
        // Reset transient calculation vars explicitly? (Awake already does this)
        // _inputRPM_sim = 0f; _torqueAppliedBySource_sim = 0f; etc.
        CalculateCurrentTotalRatio(); // Calculate initial ratio based on loaded persistent gear state
        // --- End Gearbox Specific Setup ---
    }

    // InitializeTransientState REMOVED

    protected override void UpdateInventoryItemRuntimeState() {
        // base.UpdateInventoryItemRuntimeState(); // If base manages state
        if (GetRuntimeState<GearboxRuntimeState>() == null) { /* Log Error */ }
    }

    public override void PrePhysicsSimulateTick(float deltaTime) {
        GearboxData gearboxData = GetItemData<GearboxData>(); // Corrected
        GearboxRuntimeState gearboxState = GetRuntimeState<GearboxRuntimeState>();
        if (gearboxData == null || gearboxState == null || OwningVehicle == null) { return; }
        // ... (Shift logic reading/writing gearboxState.currentGear, using internal shift vars) ...
        // ... (Torque calculation reads gearboxState.currentGear via CalculateCurrentTotalRatio) ...
        // ... (Wear update writing gearboxState.CurrentWear) ...
    }

    private void CalculateCurrentTotalRatio() {
        GearboxData gearboxData = GetItemData<GearboxData>(); // Corrected
        GearboxRuntimeState gearboxState = GetRuntimeState<GearboxRuntimeState>();
        if (gearboxData == null || gearboxState == null) { _currentTotalRatio = 0; return; }
        int gear = gearboxState.currentGear; float selectedGearRatio = 0f;
        if (gear == 0) { selectedGearRatio = 0; } else if (gear < 0) { selectedGearRatio = gearboxData.reverseGearRatio; } else if (gear > 0 && gear <= gearboxData.forwardGearRatios.Count) { selectedGearRatio = gearboxData.forwardGearRatios[gear - 1]; } else { gearboxState.currentGear = 0; selectedGearRatio = 0; }
        _currentTotalRatio = selectedGearRatio * gearboxData.finalDriveRatio;
    }

    #region ITorqueReceiver Implementation (Corrected Return Paths)
    public void ApplyReceivedTorque(float torque, float sourceOutputRpm) { _torqueAppliedBySource_sim = torque; _inputRPM_sim = sourceOutputRpm; }
    public float GetImposedLoadTorque() {
        GearboxData gearboxData = GetItemData<GearboxData>(); // Corrected
        GearboxRuntimeState gearboxState = GetRuntimeState<GearboxRuntimeState>();
        if (gearboxData == null || gearboxState == null || _isCurrentlyShifting_internal || gearboxState.currentGear == 0 || Mathf.Abs(_currentTotalRatio) < 0.001f) { return 0f; } // RETURN
        ITorqueReceiver downstream = GetConnectedPartViaInterface<ITorqueReceiver>(gearboxData.outputShaftMountID); float loadDown = (downstream != null) ? downstream.GetImposedLoadTorque() : 0f;
        float reflectedLoad = (loadDown / _currentTotalRatio) / gearboxData.efficiency; float internalLoad = 5.0f;
        return reflectedLoad + internalLoad; // RETURN
    }
    public float GetCurrentInputRPM() { return _inputRPM_sim; } // RETURN
    #endregion

    #region ITorqueProvider Implementation (Corrected Return Paths/Out Param)
    public float GetAvailableTorque(out float outputRpmAtSource) {
        outputRpmAtSource = _outputRPM_sim; // Assign unconditionally
        GearboxRuntimeState gearboxState = GetRuntimeState<GearboxRuntimeState>();
        if (_isCurrentlyShifting_internal || gearboxState == null || gearboxState.currentGear == 0) {
            outputRpmAtSource = 0f; // Ensure RPM is 0
            return 0f; // RETURN
        }
        return _torqueToProvide_sim; // RETURN
    }
    #endregion
}