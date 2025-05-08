// Example: Put in Assets/Scripts/Items/Data/Ammo/ArrowItemData.cs
using UnityEngine;

[CreateAssetMenu(fileName = "NewArrowData", menuName = "Items/Ammo/Arrow")]
public class ArrowItemData : ItemData { // Make sure it inherits from ItemData

    [Header("Arrow Specifics")]
    [Tooltip("The projectile prefab containing ArrowProjectile or similar ProjectileBehavior script.")]
    public GameObject projectilePrefab; // This MUST be public GameObject

    // Add other arrow-specific static data if needed (e.g., base penetration value)

    protected override void OnValidate() {
        base.OnValidate();
        this.category = ItemCategory.Ammo; // Set appropriate category
        this.stackable = true; // Arrows are usually stackable
        if (this.maxStack <= 1) this.maxStack = 20; // Default stack size
        if (projectilePrefab == null) Debug.LogWarning($"Projectile Prefab not set for ArrowData: {this.name}", this);
    }
}