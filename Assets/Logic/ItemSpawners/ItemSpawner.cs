// Assets/Logic/ItemSpawners/ItemSpawner.cs
using UnityEngine;

/// <summary>
/// Generic base class for spawning item prefabs in the world.
/// Handles prefab instantiation based on ItemData, InventoryItem creation
/// (using default or derived-provided state), initialization of the spawned instance.
/// Provides options for spawn offsets and self-destruction.
/// </summary>
public class ItemSpawner : MonoBehaviour {

    [Header("Item Definition")]
    [Tooltip("The ItemData defining the item to spawn.")]
    [SerializeField] protected ItemData itemToSpawn; // Use protected for derived access

    // --- Restored Spawner Settings ---
    [Header("Spawner Settings")]
    [Tooltip("Should the spawner destroy itself after successfully spawning the item?")]
    [SerializeField] private bool destroySpawnerAfterSpawn = true;
    [Tooltip("Optional offset from the spawner's position for the item spawn point (relative to spawner's transform).")]
    [SerializeField] private Vector3 spawnPositionOffset = Vector3.zero;
    [Tooltip("Optional rotation relative to the spawner's rotation.")]
    [SerializeField] private Vector3 spawnRotationOffset = Vector3.zero;
    // --- End Restored Spawner Settings ---

    /// <summary>
    /// Spawns the item and potentially destroys the spawner GameObject.
    /// </summary>
    protected virtual void Awake() {
        SpawnItem();
        // Conditionally destroy the spawner based on the setting
        if (destroySpawnerAfterSpawn) {
            Destroy(gameObject);
        } else {
             // Optionally disable the component if not destroying,
             // prevents respawning on scene reload or re-enable
             enabled = false;
        }
    }

    /// <summary>
    /// Spawns the item at the spawner's position/rotation plus offsets.
    /// </summary>
    protected virtual void SpawnItem() {
        if (itemToSpawn == null) { Debug.LogError($"[{gameObject.name}] No 'Item To Spawn' assigned!", this); return; }
        if (itemToSpawn.worldPrefab == null) { Debug.LogError($"[{gameObject.name}] ItemData '{itemToSpawn.itemName}' has no World Prefab!", itemToSpawn); return; }

        // Get the fully configured InventoryItem (with cloned state) from potentially derived class
        InventoryItem newItemInstance = GetInitialInventoryItem(); // Calls derived override
        if (newItemInstance == null) { Debug.LogError($"[{gameObject.name}] Failed to get initial InventoryItem for '{itemToSpawn.itemName}'!", this); return; }

        // --- Calculate Spawn Transform using Offsets ---
        // Position: Spawner position + Spawner rotation * Local Offset
        Vector3 spawnPos = transform.position + transform.rotation * spawnPositionOffset;
        // Rotation: Spawner rotation * Local Rotation Offset
        Quaternion spawnRot = transform.rotation * Quaternion.Euler(spawnRotationOffset);
        // --- End Calculation ---

        // Instantiate & Initialize AT CALCULATED TRANSFORM
        GameObject spawnedGO = Instantiate(itemToSpawn.worldPrefab, spawnPos, spawnRot); // Use calculated pos/rot
        if (spawnedGO == null) { Debug.LogError($"[{gameObject.name}] Failed to Instantiate worldPrefab: {itemToSpawn.worldPrefab.name}", this); return; }
        spawnedGO.name = $"{itemToSpawn.worldPrefab.name}_Spawned"; // Optional: Rename

        InitializeSpawnedInstance(spawnedGO, newItemInstance);
    }

    /// <summary>
    /// Handles initializing the spawned GameObject based on its components.
    /// It will correctly call the Initialize(InventoryItem) method on ItemInstance derivatives (like FirearmInstance or PartInstance when loose)
    /// or on WorldItem.
    /// </summary>
    protected virtual void InitializeSpawnedInstance(GameObject spawnedGO, InventoryItem itemInstanceToAssign) {
        // Try to get the ItemInstance component. This covers PartInstance, FirearmInstance, etc.
        // It will call the most specific override of Initialize(InventoryItem).
        if (spawnedGO.TryGetComponent<ItemInstance>(out var itemInst)) {
            itemInst.Initialize(itemInstanceToAssign);
        }
        // WorldItem is distinct and does not inherit ItemInstance in the current structure.
        else if (spawnedGO.TryGetComponent<WorldItem>(out var worldItem)) {
            worldItem.Initialize(itemInstanceToAssign);
        }
        // If neither, then it's something else the spawner doesn't know how to generically initialize.
        else {
             Debug.LogWarning($"[{gameObject.name} ItemSpawner] Spawned prefab '{spawnedGO.name}' does not have a recognized initialization component (e.g., ItemInstance or WorldItem). The prefab should have a script that inherits from ItemInstance or is a WorldItem.", spawnedGO);
        }
    }

    /// <summary>
    /// VIRTUAL HOOK (PROTECTED): Derived spawners override to create/return InventoryItem with cloned state template.
    /// Base attempts to create item with default state.
    /// </summary>
    protected virtual InventoryItem GetInitialInventoryItem() {
        if (itemToSpawn == null) return null;
        IRuntimeState state = GetConfiguredRuntimeStateClone() ?? CreateDefaultStateForItem(itemToSpawn);
        if (state != null) return new InventoryItem(itemToSpawn, state);
        else return new InventoryItem(itemToSpawn); // No runtime state
    }

    /// <summary>
    /// VIRTUAL HOOK (PROTECTED): Derived spawners override to return CLONE of their state template. Base returns null.
    /// </summary>
    protected virtual IRuntimeState GetConfiguredRuntimeStateClone() { return null; }

    /// <summary>
    /// Helper to create the default state object based on ItemData type. Protected.
    /// </summary>
    protected IRuntimeState CreateDefaultStateForItem(ItemData itemData) {
        if (itemData is CarPartData carPartData) return carPartData.CreateDefaultRuntimeState();
        if (itemData is FirearmItemData firearmData) return new FirearmRuntimeState(firearmData.attachmentSlots, 100); // Correct Name
        // Add other types...
        return null;
    }

    protected virtual void OnValidate() {
        if (itemToSpawn == null && this.enabled) {
            Debug.LogWarning($"ItemSpawner on {gameObject.name} has no 'Item To Spawn' assigned.");
        }
    }
}