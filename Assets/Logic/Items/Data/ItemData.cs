using UnityEngine;
using System.Collections.Generic;

public abstract class ItemData : ScriptableObject {
    [Header("Basic Info")]
    public string itemName = "New Item";
    [Tooltip("Unique code for this item type. Used for lookups, e.g., in registries.")]
    public string itemCode = "default_item_code";
    public Sprite sprite;
    [TextArea] public string description;

    [Header("Inventory & World")]
    public ItemCategory category = ItemCategory.Generic;
    [Tooltip("Prefab to instantiate when this item is in the world. For generic items, this is often a 'WorldItem' prefab. For Car Parts, this is the 'PartInstance' prefab itself (e.g., EngineInstance prefab).")]
    public GameObject worldPrefab; // This is the key prefab
    public bool stackable = false;
    [Min(1)] public int maxStack = 1;
    public float weightKg = 0.1f;
    [Tooltip("If true, this item might be restricted from certain small inventories (e.g., player's main bag) and might require special handling or storage (e.g., vehicle trunk, hands-only carry).")]
    public bool isBulky = false;

    protected virtual void OnValidate() {
        if (string.IsNullOrEmpty(itemCode)) {
            itemCode = name.ToLowerInvariant().Replace(" ", "_").Replace("(", "").Replace(")", "");
        }
        if (!stackable) {
            maxStack = 1;
        } else {
            if (maxStack <= 0) maxStack = 1;
        }
    }
}