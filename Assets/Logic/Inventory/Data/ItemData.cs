// ========================
// ItemData.cs
// ========================
using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Items/Generic Item")]
public class ItemData : ScriptableObject
{
    [Header("Basic Info")]
    public string itemName = "New Item";
    public Sprite icon;
    public bool stackable = false;

    [Header("Metadata")]
    public ItemCategory category = ItemCategory.Generic;
    public string itemCode = "default_item";

    [Header("Optional References")]
    public Sprite uiSprite;
    public GameObject worldPrefab;
}