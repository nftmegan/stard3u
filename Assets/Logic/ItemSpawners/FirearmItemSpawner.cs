using UnityEngine;

/// <summary>
/// Auto-generated spawner for FirearmItemData. Configures the initial state
/// (FirearmRuntimeState) directly in the Inspector via the 'initialStateTemplate' field.
/// </summary>
public class FirearmItemSpawner : ItemSpawner {

    [Header("--- FirearmRuntimeState Initial State Template ---")]
    [Tooltip("Configure the desired starting state. This exact state will be cloned.")]
    [SerializeField] private FirearmRuntimeState initialStateTemplate = new FirearmRuntimeState();

    /// <summary>
    /// OVERRIDE: Creates the InventoryItem using the base itemToSpawn data
    /// and a CLONE of the initialStateTemplate.
    /// </summary>
    protected override InventoryItem GetInitialInventoryItem() {
        if (itemToSpawn == null) { Debug.LogError($"[{gameObject.name} FirearmItemSpawner] 'Item To Spawn' not assigned!", this); return null; }
        if (!(itemToSpawn is FirearmItemData)) { Debug.LogError($"[{gameObject.name} FirearmItemSpawner] Assigned 'Item To Spawn' is not FirearmItemData!", this); return null; }

        FirearmRuntimeState stateClone = null;
        if (initialStateTemplate != null) {
            if (initialStateTemplate is ICloneableRuntimeState templateCloneable) {
                 stateClone = templateCloneable.Clone() as FirearmRuntimeState;
                 if (stateClone == null) { Debug.LogError($"[{gameObject.name} FirearmItemSpawner] Failed clone/cast! Check FirearmRuntimeState.Clone().", this); }
            } else { Debug.LogError($"[{gameObject.name} FirearmItemSpawner] Template does not implement ICloneableRuntimeState!", this); }
        }

        // Fallback to default if template null or clone failed
        if (stateClone == null) {
            Debug.LogWarning($"[{gameObject.name} FirearmItemSpawner] Using default state.", this);
            stateClone = CreateDefaultStateForItem(itemToSpawn) as FirearmRuntimeState;
             if (stateClone == null) { Debug.LogError($"[{gameObject.name} FirearmItemSpawner] Failed to create default state!", this); return null; }
        }

        return new InventoryItem(itemToSpawn, stateClone);
    }

    /// <summary>EDITOR VALIDATION</summary>
    protected override void OnValidate() {
        base.OnValidate();
        if (itemToSpawn != null && !(itemToSpawn is FirearmItemData)) { Debug.LogError($"Assigned ItemData is NOT FirearmItemData!", this); }
         if (initialStateTemplate == null && this.enabled) { Debug.LogWarning($"'Initial State Template' is null. Default state will be used.", this); }
         else if(initialStateTemplate != null && !(initialStateTemplate is ICloneableRuntimeState)) { Debug.LogError($"Assigned Initial State Template (FirearmRuntimeState) MUST implement ICloneableRuntimeState!", this); }
    }
}
