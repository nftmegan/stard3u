using UnityEngine;

[CreateAssetMenu(fileName = "NewArrowItem", menuName = "Items/Arrow Item")]
public class ArrowItemData : ItemData
{
    [Header("Arrow Settings")]
    [Tooltip("The projectile prefab that this arrow represents.")]
    public ProjectileBehavior projectilePrefab;

    private void OnEnable()
    {
        category = ItemCategory.Ammo;
        stackable = true;
    }
}