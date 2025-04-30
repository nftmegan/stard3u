using UnityEngine;
using System.Collections.Generic;
using System; // Keep for Action

// Removed namespace

[DisallowMultipleComponent]
public class EquipmentController : MonoBehaviour
{
    [Header("Core Setup")]
    [Tooltip("The Transform under which equipment viewmodels will be instantiated and managed.")]
    [SerializeField] private Transform itemHolder;
    [Tooltip("Assign the EquipmentRegistry Scriptable Object asset here.")]
    [SerializeField] private EquipmentRegistry equipmentRegistry;

    [Header("Animation (Optional)")]
    [SerializeField] private EquipTransitionAnimator equipAnimator;

    // Runtime References & State
    private IEquipmentHolder _equipmentHolder; // Provides inventory context and equip events
    private readonly Dictionary<string, RuntimeEquippable> _instantiatedEquipmentCache = new(); // Cache instantiated prefabs by ItemCode
    private RuntimeEquippable _currentActiveEquipment; // The currently visible/active equipment instance
    private IItemInputReceiver _currentInputReceiver; // Cached input receiver of the active equipment
    private InventoryItem _equippedItemLogical; // The logical InventoryItem currently equipped

    public InventoryItem EquippedItem => _equippedItemLogical; // Expose the logical item

    private void Awake()
    {
        // 1. Validate Core Setup
        if (itemHolder == null) Debug.LogError("[EquipmentController] Item Holder Transform is not assigned!", this);
        if (equipmentRegistry == null) Debug.LogError("[EquipmentController] Equipment Registry SO is not assigned!", this);

        // 2. Find Player Manager & Equipment Holder (Inventory)
        PlayerManager playerManager = GetComponentInParent<PlayerManager>();
        if (playerManager != null)
        {
            _equipmentHolder = playerManager.Inventory; // Get IEquipmentHolder via PlayerManager
            if (_equipmentHolder == null)
            {
                Debug.LogError("[EquipmentController] Found PlayerManager, but its 'Inventory' property is null or invalid!", this);
            }
        }
        else
        {
            Debug.LogError("[EquipmentController] Could not find PlayerManager component in parent hierarchy!", this);
        }

        // 3. Pre-Instantiate Fallback/Unarmed State (Highly Recommended)
        // This ensures the "hands" are ready immediately without delay on first unequip/start.
        if (equipmentRegistry != null && equipmentRegistry.FallbackPrefab != null)
        {
            // Check if it needs instantiation (it shouldn't be in the cache yet)
            string fallbackCode = equipmentRegistry.FallbackPrefab.ItemCode;
            if (!string.IsNullOrEmpty(fallbackCode) && !_instantiatedEquipmentCache.ContainsKey(fallbackCode))
            {
                InstantiateAndCacheEquipment(equipmentRegistry.FallbackPrefab);
                 // We don't activate it here, Equip will handle the initial state.
            }
            else if(string.IsNullOrEmpty(fallbackCode))
            {
                 Debug.LogError("[EquipmentController] Fallback Prefab in Registry is missing its Item Code!", equipmentRegistry.FallbackPrefab);
            }
        }
        else if (equipmentRegistry != null && equipmentRegistry.FallbackPrefab == null)
        {
             Debug.LogWarning("[EquipmentController] No Fallback Prefab assigned in the Equipment Registry.", this);
        }
    }

    private void OnEnable()
    {
        // Subscribe to the event that signals the logical equipped item has changed
        if (_equipmentHolder != null)
        {
            _equipmentHolder.OnEquippedItemChanged += HandleEquipRequest;
            // Immediately sync with the current state when enabled
            HandleEquipRequest(_equipmentHolder.GetCurrentEquippedItem());
        }
    }

    private void OnDisable()
    {
        // Unsubscribe when disabled/destroyed
        if (_equipmentHolder != null)
        {
            _equipmentHolder.OnEquippedItemChanged -= HandleEquipRequest;
        }
        // Optionally clear cache or destroy instantiated objects on disable/destroy if needed
        // ClearInstantiatedCache();
    }

    // --- Input Handling Methods (Called by PlayerManager) ---
    // These methods pass input down to the currently active equipment's InputReceiver

    public void HandleFire1Down() => PassInput(r => r.OnFire1Down());
    public void HandleFire1Up() => PassInput(r => r.OnFire1Up());
    public void HandleFire1Hold() => PassInput(r => r.OnFire1Hold());
    public void HandleFire2Down() => PassInput(r => r.OnFire2Down());
    public void HandleFire2Up() => PassInput(r => r.OnFire2Up());
    public void HandleFire2Hold() => PassInput(r => r.OnFire2Hold());
    public void HandleReloadDown() => PassInput(r => r.OnReloadDown());
    public void HandleUtilityDown() => PassInput(r => r.OnUtilityDown());
    public void HandleUtilityUp() => PassInput(r => r.OnUtilityUp());

    private void PassInput(Action<IItemInputReceiver> inputAction)
    {
        if (_currentInputReceiver != null)
        {
            inputAction?.Invoke(_currentInputReceiver);
        }
    }

    // --- Equip Flow ---

    // Called when the IEquipmentHolder signals a change in the equipped item
    private void HandleEquipRequest(InventoryItem newItem)
    {
        //Debug.Log($"[EquipmentController] HandleEquipRequest received item: {newItem?.data?.itemName ?? "NULL"}. Cached: {_equippedItemLogical?.data?.itemName ?? "NULL"}"); // <<< ADD LOG

        if (newItem == _equippedItemLogical && _currentActiveEquipment != null)
        {
            //Debug.Log("[EquipmentController] Item is the same instance as current. No change."); // <<< ADD LOG
            return;
        }
        Equip(newItem);
    }

    private void Equip(InventoryItem itemToEquip)
    {
        //Debug.Log($"[EquipmentController] Equip method called for item: {itemToEquip?.data?.itemName ?? "NULL"}"); // <<< ADD LOG
        
        // 1. Determine the Target Prefab using the Registry
        RuntimeEquippable targetPrefab = equipmentRegistry?.GetPrefabForItem(itemToEquip?.data); // Handles null itemToEquip

        if (targetPrefab == null)
        {
            //Debug.LogError($"[EquipmentController] Cannot equip item '{itemToEquip?.data?.itemName ?? "NULL"}'. No suitable prefab found in registry (including fallback)!", this);
            // Attempt to disable current item if possible
            if (_currentActiveEquipment != null)
            {
                 _currentActiveEquipment.gameObject.SetActive(false);
                 _currentActiveEquipment = null;
                 _currentInputReceiver = null;
            }
            _equippedItemLogical = itemToEquip; // Update logical item anyway
            return;
        }

        // 2. Get or Instantiate the Target Instance
        RuntimeEquippable targetInstance = GetOrCreateInstance(targetPrefab);

        if (targetInstance == null)
        {
             //Debug.LogError($"[EquipmentController] Failed to get or create instance for prefab '{targetPrefab.name}'.", this);
             // Attempt to disable current item
             if (_currentActiveEquipment != null) _currentActiveEquipment.gameObject.SetActive(false);
             _currentActiveEquipment = null;
             _currentInputReceiver = null;
             _equippedItemLogical = itemToEquip;
             return;
        }


        // 3. Deactivate the *Currently Active* Equipment (if it exists and is different)
        if (_currentActiveEquipment != null && _currentActiveEquipment != targetInstance)
        {
            _currentActiveEquipment.gameObject.SetActive(false);
        }

        // 4. Activate the Target Equipment Instance
        _currentActiveEquipment = targetInstance;
        if (!_currentActiveEquipment.gameObject.activeSelf)
        {
            _currentActiveEquipment.gameObject.SetActive(true);
        }

        // 5. Initialize the Instance with Item Data
        // Pass the logical item (even if null for unarmed) and the inventory container
        _currentActiveEquipment.Initialize(itemToEquip, _equipmentHolder?.GetContainerForInventory());

        // 6. Cache Input Receiver
        _currentInputReceiver = _currentActiveEquipment.GetComponent<IItemInputReceiver>();

        // 7. Update Logical Item Cache
        _equippedItemLogical = itemToEquip;

        // 8. Play Animation (Optional)
        if (equipAnimator) equipAnimator.Play(null); // Adapt payload if needed

        // Debug.Log($"Equipped: {targetInstance.ItemCode} (Prefab: {targetPrefab.name})");
    }

    // --- Instantiation and Caching Logic ---

    /// <summary>
    /// Gets an existing instance from the cache or instantiates and caches a new one.
    /// </summary>
    private RuntimeEquippable GetOrCreateInstance(RuntimeEquippable prefab)
    {
        if (prefab == null || string.IsNullOrEmpty(prefab.ItemCode))
        {
            Debug.LogError("[EquipmentController] Cannot get/create instance: Prefab is null or missing ItemCode.", prefab);
            return null;
        }

        // Try to get from cache first
        if (_instantiatedEquipmentCache.TryGetValue(prefab.ItemCode, out RuntimeEquippable cachedInstance))
        {
             // Important: Ensure the cached instance isn't destroyed or null
             if (cachedInstance != null) {
                 return cachedInstance;
             } else {
                  // Instance was destroyed somehow, remove from cache
                  _instantiatedEquipmentCache.Remove(prefab.ItemCode);
                  Debug.LogWarning($"Cached instance for '{prefab.ItemCode}' was null/destroyed. Re-instantiating.");
             }
        }

        // If not in cache or was destroyed, instantiate it now
        return InstantiateAndCacheEquipment(prefab);
    }

    /// <summary>
    /// Instantiates the equipment prefab, parents it, disables it, and adds it to the cache.
    /// </summary>
    private RuntimeEquippable InstantiateAndCacheEquipment(RuntimeEquippable prefab)
    {
        if (prefab == null || string.IsNullOrEmpty(prefab.ItemCode) || itemHolder == null)
        {
             Debug.LogError($"[EquipmentController] Failed to instantiate '{prefab?.name ?? "NULL PREFAB"}' - Invalid prefab, ItemCode, or ItemHolder.", prefab);
             return null;
        }

        // Instantiate the prefab under the item holder
        RuntimeEquippable newInstance = Instantiate(prefab, itemHolder);
        newInstance.gameObject.name = prefab.name; // Keep original name for clarity in hierarchy

        // Start disabled - Equip method will activate it
        newInstance.gameObject.SetActive(false);

        // Add to cache using the ItemCode from the prefab's RuntimeEquippable component
        _instantiatedEquipmentCache[prefab.ItemCode] = newInstance;

        // Debug.Log($"Instantiated and cached: {newInstance.ItemCode}");
        return newInstance;
    }

    /// <summary>
    /// (Optional) Clears the cache and destroys instantiated GameObjects.
    /// Call this on scene changes or player destruction if necessary.
    /// </summary>
    public void ClearInstantiatedCache()
    {
         foreach(var kvp in _instantiatedEquipmentCache)
         {
             if (kvp.Value != null) // Check if it wasn't already destroyed
             {
                 Destroy(kvp.Value.gameObject);
             }
         }
         _instantiatedEquipmentCache.Clear();
         _currentActiveEquipment = null; // Reset references
         _currentInputReceiver = null;
         //Debug.Log("Equipment cache cleared.");
    }
}