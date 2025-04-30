using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[System.Serializable]
public class EquipmentMapping
{
    public ItemData itemDefinition; // The ScriptableObject defining the item
    public RuntimeEquippable prefab; // The prefab to instantiate/activate
}

[CreateAssetMenu(menuName = "Registry/Equipment Registry")]
public class EquipmentRegistry : ScriptableObject
{
    [SerializeField] private List<EquipmentMapping> equipmentMappings = new List<EquipmentMapping>();
    [SerializeField] private RuntimeEquippable fallbackHandsPrefab; // The prefab for unarmed state

    private Dictionary<ItemData, RuntimeEquippable> _lookup;

    private void OnEnable()
    {
        // Build lookup dictionary for fast access
        _lookup = equipmentMappings.Where(m => m.itemDefinition != null && m.prefab != null)
                                    .ToDictionary(m => m.itemDefinition, m => m.prefab);
    }

    public RuntimeEquippable GetPrefabForItem(ItemData itemData)
    {
        if (itemData != null && _lookup != null && _lookup.TryGetValue(itemData, out var prefab))
        {
            return prefab;
        }
        // Return fallback if itemData is null or not found in the registry
        return fallbackHandsPrefab;
    }

    public RuntimeEquippable FallbackPrefab => fallbackHandsPrefab;
}