// Assets/Logic/Items/Core/EquipmentController.cs
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

    private IEquipmentHolder _equipmentHolder;
    private IAimProvider _aimProvider;

    private readonly Dictionary<string, RuntimeEquippable> _instantiatedEquipmentCache = new();
    private RuntimeEquippable _currentActiveEquipment;
    private IItemInputReceiver _currentInputReceiver;
    private InventoryItem _equippedItemLogical;

    public InventoryItem EquippedItemLogical => _equippedItemLogical;
    public RuntimeEquippable CurrentVisualEquipment => _currentActiveEquipment;
    public EquippableBehavior CurrentEquippableBehavior => _currentActiveEquipment?.GetComponent<EquippableBehavior>();


    private void Awake() {
        if (itemHolder == null) Debug.LogError("[EquipmentController] Item Holder Transform is not assigned!", this);
        if (equipmentRegistry == null) Debug.LogError("[EquipmentController] Equipment Registry SO is not assigned!", this);

        // Attempt to get contexts from PlayerManager or fallback
        PlayerManager playerManager = GetComponentInParent<PlayerManager>();
        if (playerManager != null) {
            _equipmentHolder = playerManager.Inventory; // PlayerInventory implements IEquipmentHolder
            _aimProvider = playerManager.Look as IAimProvider; // PlayerLook implements IAimProvider

            if (_equipmentHolder == null) Debug.LogError($"[EC on {gameObject.name}] PlayerManager's 'Inventory' (IEquipmentHolder) is null on {playerManager.name}!", this);
            if (_aimProvider == null && playerManager.Look != null) Debug.LogError($"[EC on {gameObject.name}] PlayerManager's 'Look' on {playerManager.Look.name} does not implement IAimProvider!", this);
            else if(_aimProvider == null && playerManager.Look == null) Debug.LogWarning($"[EC on {gameObject.name}] PlayerManager ({playerManager.name}) has a null 'Look' component.", this);

        } else { // Fallback if no PlayerManager
            _equipmentHolder = GetComponentInParent<PlayerInventory>(true);
            _aimProvider = GetComponentInParent<PlayerLook>(true);
            Debug.LogWarning($"[EC on {gameObject.name}] PlayerManager not found in parent. Using fallback GetComponentInParent searches. This might be problematic for AI or other setups.", this);
        }

        if (_equipmentHolder == null) Debug.LogError($"[EC on {gameObject.name}] CRITICAL: Could not find IEquipmentHolder component!", this);
        if (_aimProvider == null) Debug.LogError($"[EC on {gameObject.name}] CRITICAL: Could not find IAimProvider component!", this);
        if (equipmentRegistry == null) { Debug.LogError($"[EC on {gameObject.name}] CRITICAL: EquipmentRegistry is null!", this); this.enabled = false; return; }

        // Instantiate and cache fallback/hands prefab
        if (equipmentRegistry.FallbackPrefab != null) {
            RuntimeEquippable fallbackPrefab = equipmentRegistry.FallbackPrefab;
            string fallbackCacheKey = !string.IsNullOrEmpty(fallbackPrefab.ItemCode) ? fallbackPrefab.ItemCode : fallbackPrefab.name;
            if(string.IsNullOrEmpty(fallbackCacheKey)) {
                 Debug.LogWarning($"[EC on {gameObject.name}] Fallback Prefab '{fallbackPrefab}' has no ItemCode/name. Using InstanceID.", fallbackPrefab);
                 fallbackCacheKey = fallbackPrefab.GetInstanceID().ToString();
            }
            if (!string.IsNullOrEmpty(fallbackCacheKey) && !_instantiatedEquipmentCache.ContainsKey(fallbackCacheKey)) {
                 InstantiateAndCacheEquipment(fallbackPrefab, fallbackCacheKey);
            }
        } else { Debug.LogWarning($"[EC on {gameObject.name}] No Fallback Prefab assigned in Equipment Registry.", this); }
    }

    // Called by PlayerManager.Start() to ensure dependencies are ready
    public void ManualStart() {
        if (_equipmentHolder != null) {
            _equipmentHolder.OnEquippedItemChanged -= HandleEquipRequest;
            _equipmentHolder.OnEquippedItemChanged += HandleEquipRequest;

            // Check if holder is ready before getting current item
            if (_equipmentHolder is PlayerInventory pi && pi.Container == null) {
                 Debug.LogError($"[EC ManualStart on {gameObject.name}] EquipmentHolder ({_equipmentHolder.GetType().Name}) is PlayerInventory, but its Container is NULL. Cannot perform initial equip. Check PlayerInventory setup.", _equipmentHolder as MonoBehaviour);
                 HandleEquipRequest(null); // Default to hands if container isn't ready
            } else {
                 HandleEquipRequest(_equipmentHolder.GetCurrentEquippedItem());
            }
        } else {
             Debug.LogError($"[EC ManualStart on {gameObject.name}] CRITICAL: IEquipmentHolder is null! Cannot subscribe or perform initial equip.", this);
        }
    }

    private void OnEnable() {
        // It's generally safer for PlayerManager.Start to call ManualStart after all Awakes.
        // If this component can be enabled/disabled at runtime independently:
        if (Application.isPlaying && _equipmentHolder != null) {
            // Re-subscribe and re-equip if being re-enabled
            // This assumes ManualStart (or equivalent logic) has run at least once initially.
            _equipmentHolder.OnEquippedItemChanged -= HandleEquipRequest;
            _equipmentHolder.OnEquippedItemChanged += HandleEquipRequest;
            HandleEquipRequest(_equipmentHolder.GetCurrentEquippedItem());
        }
    }

    private void OnDisable() {
         if (_equipmentHolder != null) {
            _equipmentHolder.OnEquippedItemChanged -= HandleEquipRequest;
        }
    }

    // --- Input Forwarding ---
    public void HandleFire1Down() => PassInput(r => r.OnFire1Down());
    public void HandleFire1Up() => PassInput(r => r.OnFire1Up());
    public void HandleFire1Hold() => PassInput(r => r.OnFire1Hold());
    public void HandleFire2Down() => PassInput(r => r.OnFire2Down());
    public void HandleFire2Up() => PassInput(r => r.OnFire2Up());
    public void HandleFire2Hold() => PassInput(r => r.OnFire2Hold());
    public void HandleReloadDown() => PassInput(r => r.OnReloadDown());
    public void HandleStoreDown() => PassInput(r => r.OnStoreDown());
    public void HandleUtilityDown() => PassInput(r => r.OnUtilityDown());
    public void HandleUtilityUp() => PassInput(r => r.OnUtilityUp());

    private void PassInput(Action<IItemInputReceiver> inputAction) {
        if (_currentActiveEquipment != null && _currentActiveEquipment.gameObject.activeSelf) {
             _currentInputReceiver = _currentActiveEquipment.GetComponent<IItemInputReceiver>();
             if (_currentInputReceiver != null) {
                inputAction?.Invoke(_currentInputReceiver);
             }
        }
    }

    private void HandleEquipRequest(InventoryItem newItemFromInventory) {
        _equippedItemLogical = newItemFromInventory; // Store the logical item
        Equip(_equippedItemLogical);
    }

    private RuntimeEquippable DetermineTargetPrefab(InventoryItem item) {
        if (equipmentRegistry == null) {
            Debug.LogError($"[EC DetermineTargetPrefab on {gameObject.name}] EquipmentRegistry is null!", this);
            return null;
        }
        if (item == null || item.data == null) return equipmentRegistry.FallbackPrefab;

        // Special handling for CarPartData - usually falls back to Hands unless a specific "carryable part" viewmodel exists
        if (item.data is CarPartData) {
            // If you ever wanted a specific viewmodel FOR HOLDING a car part (e.g. carrying an engine block visually)
            // you could register that. Otherwise, it defaults to FallbackPrefab (Hands).
            RuntimeEquippable specificCarPartPrefab = equipmentRegistry.GetPrefabForItem(item.data);
            if (specificCarPartPrefab == equipmentRegistry.FallbackPrefab || specificCarPartPrefab == null) {
                return equipmentRegistry.FallbackPrefab;
            }
            return specificCarPartPrefab; // This case is rare for CarPartData unless it's a tool-like part.
        }
        return equipmentRegistry.GetPrefabForItem(item.data);
    }

    private void Equip(InventoryItem itemToEquipLogically) {
        RuntimeEquippable targetPrefab = DetermineTargetPrefab(itemToEquipLogically);
        if (targetPrefab == null) {
             Debug.LogError($"[EC Equip on {gameObject.name}] CRITICAL: No target prefab for '{itemToEquipLogically?.data?.itemName ?? "NULL"}' (even FallbackPrefab is null). EC may be disabled or broken.", this);
             if (_currentActiveEquipment != null) _currentActiveEquipment.gameObject.SetActive(false);
             _currentActiveEquipment = null; _currentInputReceiver = null;
             // Consider disabling this.enabled = false; if this state is unrecoverable.
             return;
        }

        EquippableBehavior currentBehavior = CurrentEquippableBehavior;
        if (_currentActiveEquipment != null &&
            _currentActiveEquipment.name == targetPrefab.name && // Check if visual prefab is the same
            currentBehavior != null &&
            currentBehavior.RuntimeItemInstance == itemToEquipLogically) { // Check if logical item is the same
            return; // Already equipped this exact item and visual
        }

        RuntimeEquippable targetInstance = GetOrCreateInstance(targetPrefab);
        if (targetInstance == null) {
             Debug.LogError($"[EC Equip on {gameObject.name}] Failed to get or create instance for '{targetPrefab.name}'. Aborting equip.", this);
             if (_currentActiveEquipment != null) _currentActiveEquipment.gameObject.SetActive(false);
             _currentActiveEquipment = null; _currentInputReceiver = null;
             return;
        }

        if (_currentActiveEquipment != null && _currentActiveEquipment != targetInstance) {
            _currentActiveEquipment.gameObject.SetActive(false);
        }

        _currentActiveEquipment = targetInstance;
        if (!_currentActiveEquipment.gameObject.activeSelf) {
            _currentActiveEquipment.gameObject.SetActive(true);
        }

        // Call Initialize with the simplified signature
        _currentActiveEquipment.Initialize(itemToEquipLogically, _equipmentHolder, _aimProvider);
        _currentInputReceiver = _currentActiveEquipment.GetComponent<IItemInputReceiver>();

        if (equipAnimator) equipAnimator.Play(null);
    }

    private RuntimeEquippable GetOrCreateInstance(RuntimeEquippable prefab) {
        if (prefab == null) { Debug.LogError($"[EC GetOrCreateInstance on {gameObject.name}] Prefab is null.", this); return null; }
        string cacheKey = !string.IsNullOrEmpty(prefab.ItemCode) ? prefab.ItemCode : prefab.name;
        if(string.IsNullOrEmpty(cacheKey)) {
             Debug.LogWarning($"[EC GetOrCreateInstance on {gameObject.name}] Prefab '{prefab}' using InstanceID as cache key.", prefab);
             cacheKey = prefab.GetInstanceID().ToString();
        }

        if (_instantiatedEquipmentCache.TryGetValue(cacheKey, out RuntimeEquippable cachedInstance)) {
            if (cachedInstance != null) return cachedInstance;
            else {
                 _instantiatedEquipmentCache.Remove(cacheKey);
                 Debug.LogWarning($"[EC GetOrCreateInstance on {gameObject.name}] Cached instance '{cacheKey}' was destroyed. Re-instantiating.", this);
            }
        }
        return InstantiateAndCacheEquipment(prefab, cacheKey);
    }

    private RuntimeEquippable InstantiateAndCacheEquipment(RuntimeEquippable prefab, string cacheKey) {
        if (prefab == null || itemHolder == null) { Debug.LogError($"[EC InstantiateAndCache on {gameObject.name}] Instantiate fail: Null prefab or itemHolder.", prefab); return null; }
        RuntimeEquippable newInstance = Instantiate(prefab, itemHolder);
        newInstance.gameObject.name = prefab.name; // Keep prefab name for easier identification
        newInstance.gameObject.SetActive(false); // Start inactive, Equip will activate
        _instantiatedEquipmentCache[cacheKey] = newInstance;
        return newInstance;
    }

    public void ClearInstantiatedCache() {
        foreach (var kvp in _instantiatedEquipmentCache.ToList()) { // ToList allows modification during iteration
            if (kvp.Value != null) Destroy(kvp.Value.gameObject);
        }
        _instantiatedEquipmentCache.Clear();
        _currentActiveEquipment = null;
        _currentInputReceiver = null;
    }
}
