using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

[DisallowMultipleComponent]
public class EquipmentController : MonoBehaviour {

    [Header("Core Setup")]
    [SerializeField] private Transform itemHolder;
    [SerializeField] private EquipmentRegistry equipmentRegistry;

    [Header("Animation (Optional)")]
    [SerializeField] private EquipTransitionAnimator equipAnimator;

    // Cached references set in Awake
    private IEquipmentHolder _equipmentHolder;
    private IAimProvider _aimProvider;

    // Runtime state
    private readonly Dictionary<string, RuntimeEquippable> _instantiatedEquipmentCache = new();
    private RuntimeEquippable _currentActiveEquipment;
    private IItemInputReceiver _currentInputReceiver;
    private InventoryItem _equippedItemLogical; // The item PlayerInventory *thinks* is equipped

    // Public accessors
    public InventoryItem EquippedItemLogical => _equippedItemLogical;
    public RuntimeEquippable CurrentVisualEquipment => _currentActiveEquipment;
    public EquippableBehavior CurrentEquippableBehavior => _currentActiveEquipment?.GetComponent<EquippableBehavior>();


    private void Awake() {
        if (itemHolder == null) Debug.LogError("[EquipmentController] Item Holder Transform is not assigned!", this);
        if (equipmentRegistry == null) Debug.LogError("[EquipmentController] Equipment Registry SO is not assigned!", this);

        // Get contexts via PlayerManager (preferred) or fallback
        PlayerManager playerManager = GetComponentInParent<PlayerManager>();
        if (playerManager != null) {
            _equipmentHolder = playerManager.Inventory;
            _aimProvider = playerManager.Look as IAimProvider;

            if (_equipmentHolder == null) Debug.LogError($"[EC on {gameObject.name}] PlayerManager's 'Inventory' (IEquipmentHolder) is null on {playerManager.name}!", this);
            if (_aimProvider == null && playerManager.Look != null) Debug.LogError($"[EC on {gameObject.name}] PlayerManager's 'Look' on {playerManager.Look.name} does not implement IAimProvider!", this);
            else if(_aimProvider == null && playerManager.Look == null) Debug.LogWarning($"[EC on {gameObject.name}] PlayerManager ({playerManager.name}) has a null 'Look' component.", this);

        } else {
            _equipmentHolder = GetComponentInParent<PlayerInventory>(true);
            _aimProvider = GetComponentInParent<PlayerLook>(true);
            Debug.LogWarning($"[EC on {gameObject.name}] PlayerManager not found in parent. Using fallback GetComponentInParent searches.", this);
        }

        // Final validation of essential contexts
        if (_equipmentHolder == null) Debug.LogError($"[EC on {gameObject.name}] CRITICAL: Could not find IEquipmentHolder component!", this);
        if (_aimProvider == null) Debug.LogError($"[EC on {gameObject.name}] CRITICAL: Could not find IAimProvider component!", this);
        if (equipmentRegistry == null) { Debug.LogError($"[EC on {gameObject.name}] CRITICAL: EquipmentRegistry is null!", this); this.enabled = false; return; }

        // Pre-instantiate fallback/hands prefab
        InstantiateFallbackPrefab();
    }

    /// <summary>
    /// Called by PlayerManager.Start after all components are likely initialized.
    /// Subscribes to inventory changes and performs the initial equip.
    /// </summary>
    public void ManualStart() {
        if (_equipmentHolder != null) {
            // Subscribe to changes in the selected inventory item
            _equipmentHolder.OnEquippedItemChanged -= HandleEquipRequest; // Ensure no duplicates
            _equipmentHolder.OnEquippedItemChanged += HandleEquipRequest;

            // Perform initial equip based on inventory state
            HandleEquipRequest(_equipmentHolder.GetCurrentEquippedItem());
        } else {
             Debug.LogError($"[EC ManualStart on {gameObject.name}] CRITICAL: IEquipmentHolder is null! Cannot subscribe or perform initial equip.", this);
        }
    }

    private void OnEnable() {
        // Re-subscribe and re-equip if re-enabled after initial start
        if (Application.isPlaying && _equipmentHolder != null) {
            _equipmentHolder.OnEquippedItemChanged -= HandleEquipRequest;
            _equipmentHolder.OnEquippedItemChanged += HandleEquipRequest;
            // Re-sync with current inventory selection
            HandleEquipRequest(_equipmentHolder.GetCurrentEquippedItem());
        }
    }

    private void OnDisable() {
         // Unsubscribe to prevent memory leaks or errors
         if (_equipmentHolder != null) {
            _equipmentHolder.OnEquippedItemChanged -= HandleEquipRequest;
        }
    }

    // --- Input Forwarding Methods ---
    public void HandleFire1Down() => PassInput(r => r.OnFire1Down());
    public void HandleFire1Up() => PassInput(r => r.OnFire1Up());
    public void HandleFire1Hold() => PassInput(r => r.OnFire1Hold());
    public void HandleFire2Down() => PassInput(r => r.OnFire2Down());
    public void HandleFire2Up() => PassInput(r => r.OnFire2Up());
    public void HandleFire2Hold() => PassInput(r => r.OnFire2Hold());
    public void HandleReloadDown() => PassInput(r => r.OnReloadDown());
    // HandleStoreDown is removed as Store action is now handled centrally
    public void HandleUtilityDown() => PassInput(r => r.OnUtilityDown());
    public void HandleUtilityUp() => PassInput(r => r.OnUtilityUp());

    /// <summary>
    /// Forwards an input action to the current active equipment's IItemInputReceiver.
    /// </summary>
    private void PassInput(Action<IItemInputReceiver> inputAction) {
        if (_currentActiveEquipment != null && _currentActiveEquipment.gameObject.activeSelf) {
             // Get receiver each time in case the component structure changes dynamically
             _currentInputReceiver = _currentActiveEquipment.GetComponent<IItemInputReceiver>();
             if (_currentInputReceiver != null) {
                try {
                    inputAction?.Invoke(_currentInputReceiver);
                } catch (Exception e) {
                    Debug.LogError($"Error executing input action on '{_currentActiveEquipment.name}': {e.Message}\n{e.StackTrace}", _currentActiveEquipment);
                }
             }
             // else: Active equipment doesn't implement IItemInputReceiver (e.g., passive item)
        }
    }

    /// <summary>
    /// Public entry point called by PlayerManager when the selected inventory item changes
    /// or when the grab state forces an equip change.
    /// </summary>
    public void HandleEquipRequest(InventoryItem newItemFromInventory) {
        _equippedItemLogical = newItemFromInventory; // Update the logical item state
        Equip(_equippedItemLogical); // Trigger the visual equip process
    }

    /// <summary>
    /// Determines the correct RuntimeEquippable prefab to use based on the InventoryItem.
    /// Falls back to the Hands prefab if the item is null, has no specific prefab registered,
    /// or is a CarPartData without a specific viewmodel override.
    /// </summary>
    private RuntimeEquippable DetermineTargetPrefab(InventoryItem item) {
        if (equipmentRegistry == null) {
            Debug.LogError($"[EC DetermineTargetPrefab] EquipmentRegistry is null!", this);
            return null; // Critical error
        }

        // If no item or no item data, use fallback
        if (item == null || item.data == null) {
            return equipmentRegistry.FallbackPrefab;
        }

        // If it's a car part, default to hands unless explicitly registered
        if (item.data is CarPartData) {
            RuntimeEquippable specificCarPartPrefab = equipmentRegistry.GetPrefabForItem(item.data);
            // If the registry returns null or the *same* fallback prefab for this car part, use the fallback.
            if (specificCarPartPrefab == null || specificCarPartPrefab == equipmentRegistry.FallbackPrefab) {
                 return equipmentRegistry.FallbackPrefab;
            }
             // Otherwise, use the specific prefab registered for this car part (rare case, e.g., a tool-like part).
             return specificCarPartPrefab;
        }

        // For other item types, get the registered prefab or fallback if none exists
        return equipmentRegistry.GetPrefabForItem(item.data); // GetPrefabForItem handles fallback internally
    }

    /// <summary>
    /// Core logic to switch the visually equipped item.
    /// Deactivates the old item, activates/initializes the new one.
    /// </summary>
    private void Equip(InventoryItem itemToEquipLogically) {
        RuntimeEquippable targetPrefab = DetermineTargetPrefab(itemToEquipLogically);

        // Handle critical error: No prefab determined (even fallback is missing)
        if (targetPrefab == null) {
             Debug.LogError($"[EC Equip] CRITICAL: No target prefab could be determined for item '{itemToEquipLogically?.data?.itemName ?? "NULL/Empty Slot"}' (Fallback Prefab might be missing in Registry!). Equipment system disabled.", this);
             if (_currentActiveEquipment != null) _currentActiveEquipment.gameObject.SetActive(false);
             _currentActiveEquipment = null; _currentInputReceiver = null;
             this.enabled = false; // Disable controller if unrecoverable
             return;
        }

        // --- Avoid unnecessary swaps ---
        // Check if requested item *and* prefab match the current active one
        EquippableBehavior currentBehavior = CurrentEquippableBehavior;
        bool isSameItemInstance = (currentBehavior != null && currentBehavior.RuntimeItemInstance == itemToEquipLogically);
        bool isSamePrefab = (_currentActiveEquipment != null && _currentActiveEquipment.name == targetPrefab.name); // Compare prefab names

        // If the prefab is the same AND the logical item instance is the same, do nothing.
        if (isSamePrefab && isSameItemInstance) {
            // Debug.Log($"[EC Equip] Already equipped: {targetPrefab.name} with same item instance.");
            return;
        }
        // --- End Check ---


        // Get or create the instance of the target prefab
        RuntimeEquippable targetInstance = GetOrCreateInstance(targetPrefab);
        if (targetInstance == null) {
             Debug.LogError($"[EC Equip] Failed to get or create instance for prefab '{targetPrefab.name}'. Aborting equip.", this);
             // Optionally try to switch back to fallback?
             if (_currentActiveEquipment != null) _currentActiveEquipment.gameObject.SetActive(false);
             _currentActiveEquipment = null; _currentInputReceiver = null;
             return;
        }

        // Deactivate the currently active equipment if it's different
        if (_currentActiveEquipment != null && _currentActiveEquipment != targetInstance) {
            _currentActiveEquipment.gameObject.SetActive(false);
            // Optional: Call an OnUnequipped method on the behavior if needed
        }

        // Set the new active equipment
        _currentActiveEquipment = targetInstance;

        // Activate and Initialize the new equipment
        if (!_currentActiveEquipment.gameObject.activeSelf) {
            _currentActiveEquipment.gameObject.SetActive(true);
        }

        // Initialize the behavior component on the newly activated equipment
        // It's crucial to pass the correct InventoryItem instance representing the equipped item.
        // For the fallback/hands state, itemToEquipLogically will be null.
        try {
            _currentActiveEquipment.Initialize(itemToEquipLogically, _equipmentHolder, _aimProvider);
        } catch (Exception e) {
            Debug.LogError($"Error during Initialize on '{_currentActiveEquipment.name}': {e.Message}\n{e.StackTrace}", _currentActiveEquipment);
            // Consider disabling the equipment or the controller if init fails critically
        }

        // Cache the input receiver for the new equipment
        _currentInputReceiver = _currentActiveEquipment.GetComponent<IItemInputReceiver>();

        // Play animation if available
        if (equipAnimator) equipAnimator.Play(null); // Consider passing callback if needed
    }

    /// <summary>
    /// Retrieves an instantiated equipment prefab from the cache or instantiates it if not found.
    /// </summary>
    private RuntimeEquippable GetOrCreateInstance(RuntimeEquippable prefab) {
        if (prefab == null) { Debug.LogError($"[EC GetOrCreateInstance] Prefab is null.", this); return null; }

        // Use item code if available, otherwise fallback to prefab name or instance ID for cache key
        string cacheKey = !string.IsNullOrEmpty(prefab.ItemCode) ? prefab.ItemCode : prefab.name;
        if(string.IsNullOrEmpty(cacheKey)) {
             Debug.LogWarning($"[EC GetOrCreateInstance] Prefab '{prefab}' has no ItemCode or name. Using InstanceID as cache key.", prefab);
             cacheKey = prefab.GetInstanceID().ToString();
        }

        if (_instantiatedEquipmentCache.TryGetValue(cacheKey, out RuntimeEquippable cachedInstance)) {
            // Ensure the cached instance hasn't been destroyed
            if (cachedInstance != null) return cachedInstance;
            else {
                 // Instance was destroyed, remove from cache and log warning
                 _instantiatedEquipmentCache.Remove(cacheKey);
                 Debug.LogWarning($"[EC GetOrCreateInstance] Cached instance for key '{cacheKey}' (Prefab: {prefab.name}) was destroyed. Re-instantiating.", this);
            }
        }
        // Instance not found or was destroyed, instantiate a new one
        return InstantiateAndCacheEquipment(prefab, cacheKey);
    }

    /// <summary>
    /// Instantiates the equipment prefab, parents it, sets its name, caches it, and returns the instance.
    /// </summary>
    private RuntimeEquippable InstantiateAndCacheEquipment(RuntimeEquippable prefab, string cacheKey) {
        if (prefab == null || itemHolder == null) { Debug.LogError($"[EC InstantiateAndCache] Instantiate fail: Null prefab or itemHolder.", prefab); return null; }
        try {
            RuntimeEquippable newInstance = Instantiate(prefab, itemHolder);
            newInstance.gameObject.name = prefab.name; // Keep original prefab name for easier debugging
            newInstance.gameObject.SetActive(false); // Start inactive, Equip method will activate
            _instantiatedEquipmentCache[cacheKey] = newInstance;
            return newInstance;
        } catch (Exception e) {
             Debug.LogError($"[EC InstantiateAndCache] Failed to instantiate prefab '{prefab.name}': {e.Message}\n{e.StackTrace}", prefab);
             return null;
        }
    }

     /// <summary>
    /// Instantiates the fallback prefab defined in the registry.
    /// </summary>
    private void InstantiateFallbackPrefab() {
         if (equipmentRegistry != null && equipmentRegistry.FallbackPrefab != null) {
            RuntimeEquippable fallbackPrefab = equipmentRegistry.FallbackPrefab;
            string fallbackCacheKey = !string.IsNullOrEmpty(fallbackPrefab.ItemCode) ? fallbackPrefab.ItemCode : fallbackPrefab.name;
             if(string.IsNullOrEmpty(fallbackCacheKey)) {
                 fallbackCacheKey = fallbackPrefab.GetInstanceID().ToString(); // Use InstanceID if no name/code
            }
            // Only instantiate if not already in cache
            if (!string.IsNullOrEmpty(fallbackCacheKey) && !_instantiatedEquipmentCache.ContainsKey(fallbackCacheKey)) {
                 InstantiateAndCacheEquipment(fallbackPrefab, fallbackCacheKey);
            }
        } else { Debug.LogWarning($"[EC on {gameObject.name}] No Fallback Prefab assigned in Equipment Registry. 'Hands' state might not work.", this); }
    }

    /// <summary>
    /// Destroys all cached equipment instances. Used during cleanup or scene changes.
    /// </summary>
    public void ClearInstantiatedCache() {
        foreach (var kvp in _instantiatedEquipmentCache.ToList()) { // ToList allows modification during iteration
            if (kvp.Value != null) Destroy(kvp.Value.gameObject);
        }
        _instantiatedEquipmentCache.Clear();
        _currentActiveEquipment = null;
        _currentInputReceiver = null;
    }
}