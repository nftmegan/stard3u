using UnityEngine;

public class NineMMBulletProjectile : BulletProjectile
{
    [Header("9mm Specific Settings")]
    [SerializeField] private float specificBulletSpeed = 70f; // Override speed for 9mm bullets

    // Use Awake instead of Start to modify bullet speed in the derived class
    protected override void Start()
    {
        base.Start();  // Call base class Awake method if it exists
        bulletSpeed = specificBulletSpeed;  // Set the specific speed for 9mm bullets
    }

    protected override void OnHit(RaycastHit hit)
    {
        base.OnHit(hit);

        // Apply 9mm bullet damage
        IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(15f, hit.point, velocity.normalized); // Specific damage for 9mm
        }
    }
}