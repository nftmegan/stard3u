using UnityEngine;

[CreateAssetMenu(fileName = "NewProjectileItem", menuName = "Items/Projectile")]
public class ProjectileItemData : ItemData
{
    [Header("Projectile Settings")]
    [Tooltip("The projectile prefab that this item represents.")]
    public ProjectileBehavior projectilePrefab;

    [Header("Projectile Attributes")]
    [Tooltip("The damage dealt by this projectile.")]
    public float damage = 10f;

    [Tooltip("The speed of this projectile.")]
    public float speed = 20f;

    [Tooltip("How long the projectile will last before it disappears.")]
    public float lifetime = 5f;

    [Header("Shoot Settings")]
    [Tooltip("The base shoot force applied when firing the projectile.")]
    public float baseShootForce = 40f; // New field to define base shoot force for projectiles

    private void OnEnable()
    {
        category = ItemCategory.Ammo;
        stackable = true;
    }
}