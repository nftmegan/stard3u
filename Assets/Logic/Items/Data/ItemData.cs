using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Items/Generic")]
public class ItemData : ScriptableObject
{
    [Header("Basic Info")]
    public string itemName = "New Item";
    public Sprite sprite;

    [Header("Stacking")]
    public bool stackable = false;

    [Tooltip("Maximum items per slot when stackable.\n" +
             "Ignored (internally set to 1) when ‘stackable’ is false.")]
    [Min(1)]
    public int maxStack = 1;

    [Header("Metadata")]
    public ItemCategory category = ItemCategory.Generic;
    public string itemCode = "default_item";
}