using UnityEngine;

/// <summary>
/// Auto-generated spawner for GearboxData. Configures the initial state
/// (GearboxRuntimeState) directly in the Inspector via the 'initialStateTemplate' field.
/// </summary>
public class GearboxItemSpawner : ItemSpawner {

    [Header("--- GearboxRuntimeState Initial State Template ---")]
    [Tooltip("Configure the desired starting state. This exact state will be cloned.")]
    [SerializeField] private GearboxRuntimeState initialStateTemplate = new GearboxRuntimeState();

    /// <summary>
    /// OVERRIDE: Creates the InventoryItem using the base itemToSpawn data
    /// and a CLONE of the initialStateTemplate.
    /// </summary>
    protected override InventoryItem GetInitialInventoryItem() {
        if (itemToSpawn == null) { Debug.LogError($"[{gameObject.name} GearboxItemSpawner] 'Item To Spawn' not assigned!", this); return null; }
        if (!(itemToSpawn is GearboxData)) { Debug.LogError($"[{gameObject.name} GearboxItemSpawner] Assigned 'Item To Spawn' is not GearboxData!", this); return null; }

        GearboxRuntimeState stateClone = null;
        if (initialStateTemplate != null) {
            if (initialStateTemplate is ICloneableRuntimeState templateCloneable) {
                 stateClone = templateCloneable.Clone() as GearboxRuntimeState;
                 if (stateClone == null) { Debug.LogError($"[{gameObject.name} GearboxItemSpawner] Failed clone/cast! Check GearboxRuntimeState.Clone().", this); }
            } else { Debug.LogError($"[{gameObject.name} GearboxItemSpawner] Template does not implement ICloneableRuntimeState!", this); }
        }

        // Fallback to default if template null or clone failed
        if (stateClone == null) {
            Debug.LogWarning($"[{gameObject.name} GearboxItemSpawner] Using default state.", this);
            stateClone = CreateDefaultStateForItem(itemToSpawn) as GearboxRuntimeState;
             if (stateClone == null) { Debug.LogError($"[{gameObject.name} GearboxItemSpawner] Failed to create default state!", this); return null; }
        }

        return new InventoryItem(itemToSpawn, stateClone);
    }

    /// <summary>EDITOR VALIDATION</summary>
    protected override void OnValidate() {
        base.OnValidate();
        if (itemToSpawn != null && !(itemToSpawn is GearboxData)) { Debug.LogError($"Assigned ItemData is NOT GearboxData!", this); }
         if (initialStateTemplate == null && this.enabled) { Debug.LogWarning($"'Initial State Template' is null. Default state will be used.", this); }
         else if(initialStateTemplate != null && !(initialStateTemplate is ICloneableRuntimeState)) { Debug.LogError($"Assigned Initial State Template (GearboxRuntimeState) MUST implement ICloneableRuntimeState!", this); }
    }
}
