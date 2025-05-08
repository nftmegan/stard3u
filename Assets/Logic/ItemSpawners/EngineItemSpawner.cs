using UnityEngine;

/// <summary>
/// Spawns an Engine, using the 'initialStateTemplate' configured directly
/// in the Inspector as the source for the spawned engine's runtime state.
/// </summary>
public class EngineItemSpawner : ItemSpawner { // Inherit from base

    [Header("--- Engine Initial State Template ---")]
    [Tooltip("Configure the desired starting state for the spawned engine here. This exact state will be cloned.")]
    [SerializeField] private EngineRuntimeState initialStateTemplate = new EngineRuntimeState(); // The template you edit!

    /// <summary>
    /// OVERRIDE: Creates the InventoryItem using the base itemToSpawn data
    /// and a CLONE of the initialStateTemplate configured in the Inspector.
    /// </summary>
    protected override InventoryItem GetInitialInventoryItem() {
        if (itemToSpawn == null) {
             Debug.LogError($"[{gameObject.name} EngineItemSpawner] 'Item To Spawn' (EngineData) is not assigned!", this);
             return null;
        }
        if (!(itemToSpawn is EngineData)) {
             Debug.LogError($"[{gameObject.name} EngineItemSpawner] Assigned 'Item To Spawn' is not EngineData!", this);
             return null;
        }

        // --- Cloning ---
        EngineRuntimeState stateClone = null;
        if (initialStateTemplate != null) {
            // Manual field-by-field clone is safest
            stateClone = new EngineRuntimeState {
                CurrentDurability = initialStateTemplate.CurrentDurability,
                CurrentWear = initialStateTemplate.CurrentWear,
                currentEngineOilLiters = initialStateTemplate.currentEngineOilLiters,
                currentCoolantLiters = initialStateTemplate.currentCoolantLiters,
                totalOperatingHours = initialStateTemplate.totalOperatingHours,
                isSeized = initialStateTemplate.isSeized,
                currentTemperatureCelsius = initialStateTemplate.currentTemperatureCelsius
            };
        } else {
            // If user set template to null, fall back to default creation
            Debug.LogWarning($"[{gameObject.name} EngineItemSpawner] Initial State Template is null. Creating default engine state.", this);
            stateClone = (EngineRuntimeState)CreateDefaultStateForItem(itemToSpawn); // Use base helper
             if (stateClone == null) { // Check if default creation failed
                 Debug.LogError($"[{gameObject.name} EngineItemSpawner] Failed to create default engine state!", this);
                 return null;
             }
        }
        // --- End Cloning ---

        // Create the final InventoryItem with the cloned state
        return new InventoryItem(itemToSpawn, stateClone);
    }

    /// <summary>
    /// EDITOR VALIDATION: Ensure the assigned item is EngineData.
    /// </summary>
    protected override void OnValidate() {
        base.OnValidate(); // Check if itemToSpawn is assigned
        if (itemToSpawn != null && !(itemToSpawn is EngineData)) {
             Debug.LogError($"[{gameObject.name} EngineItemSpawner] Assigned 'Item To Spawn' ({itemToSpawn.name}) is NOT EngineData!", this);
        }
         if (initialStateTemplate == null) {
             Debug.LogWarning($"[{gameObject.name} EngineItemSpawner] 'Initial State Template' is null. Default state will be used.", this);
         }
    }
}