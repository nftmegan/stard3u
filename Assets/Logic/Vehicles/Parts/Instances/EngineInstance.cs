using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EngineInstance : PartInstance, ITorqueProvider {

    // --- Internal TRANSIENT Simulation Vars ---
    private float _currentRPM_internal_sim = 0f;
    private float _torqueToProvideThisTick = 0f;

    protected override void Awake() {
        base.Awake();
        _currentRPM_internal_sim = 0f;
        _torqueToProvideThisTick = 0f;
    }

    /// <summary>
    /// Initialize: Sets up engine after base setup.
    /// </summary>
    public override void Initialize(InventoryItem itemInstance, VehicleRoot vehicleRoot) {
        base.Initialize(itemInstance, vehicleRoot); // Handles ItemInstanceData assignment, validation, base setup
        if (!this.enabled) return; // Check if base init failed

        // --- Engine Specific Transient Setup ---
        EngineRuntimeState engineState = GetRuntimeState<EngineRuntimeState>();
        EngineData engineData = GetItemData<EngineData>(); // Correct method name

        if (engineState != null && engineData != null) {
            _currentRPM_internal_sim = engineState.isSeized ? 0f : Mathf.Max(0, engineData.idleRPM * 0.8f);
        } else {
             // Base Initialize should have already disabled if state/data was critically missing
             Debug.LogError($"[{gameObject.name}] EngineInstance Initialize: Missing required state/data after base init!", this);
             this.enabled = false; // Ensure disable
        }
        _torqueToProvideThisTick = 0f;
        // --- End Engine Specific Setup ---
    }

    // InitializeTransientState REMOVED

    protected override void UpdateInventoryItemRuntimeState() {
        // base.UpdateInventoryItemRuntimeState(); // If base managed durability in state obj
        if (GetRuntimeState<EngineRuntimeState>() == null) { /* Log Error */ }
        // Sync back any locally cached values if they existed
    }

    public override void PrePhysicsSimulateTick(float deltaTime) {
        EngineData engineData = GetItemData<EngineData>(); // Correct method name
        EngineRuntimeState engineState = GetRuntimeState<EngineRuntimeState>();
        if (engineData == null || engineState == null) { return; }
        // ... (Simulation logic reading/writing 'engineState' fields) ...
    }

    public float GetAvailableTorque(out float outputRpm) {
        outputRpm = _currentRPM_internal_sim;
        EngineRuntimeState engineState = GetRuntimeState<EngineRuntimeState>();
        if (engineState == null || engineState.isSeized) return 0f;
        EngineData engineData = GetItemData<EngineData>(); // Correct method name
        if (engineData == null) { outputRpm = 0f; return 0f; }
        bool isEffectivelyRunning = _currentRPM_internal_sim >= engineData.idleRPM * 0.85f;
        return isEffectivelyRunning ? _torqueToProvideThisTick : 0f;
    }
}