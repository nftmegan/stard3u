using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class InventoryItem
{
    public ItemData data;  // The item data (e.g., weapon, ammo)

    public bool IsStackable => data != null && data.stackable;
}