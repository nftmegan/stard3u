// ========================
// WeaponItemData.cs
// ========================
using UnityEngine;

[CreateAssetMenu(fileName = "NewWeaponItem", menuName = "Items/Weapon Item")]
public class WeaponItemData : ItemData
{
    private void OnEnable()
    {
        category = ItemCategory.Weapon;
    }
}