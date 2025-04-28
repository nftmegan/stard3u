using UnityEngine;

public class BulletProjectile : ProjectileBehavior
{
    [Header("Settings")]
    [SerializeField] protected float impactForceMultiplier = 1f;
    [SerializeField] protected float bulletSpeed = 50f; // Bullet speed

    [Header("Gravity Settings")]
    [SerializeField] protected Vector3 gravity = new Vector3(0f, -9.81f, 0f); // Optional, typically not needed for bullets

    protected bool hasLaunched = false;
    protected Vector3 velocity;

    [Header("References")]
    protected BulletAudioHandler audioHandler;
    protected HoleDecalSpawner holeDecalSpawner;

    protected virtual void Start()
    {
        audioHandler = GetComponent<BulletAudioHandler>();
        holeDecalSpawner = GetComponent<HoleDecalSpawner>();
    }

    private void FixedUpdate()
    {
        if (!hasLaunched) return;

        // Apply gravity (optional for realism)
        velocity += gravity * Time.fixedDeltaTime;
        Vector3 movement = velocity * Time.fixedDeltaTime;

        Vector3 currentPosition = transform.position;
        Vector3 nextPosition = currentPosition + movement;

        // Raycast to detect collision
        int interactableLayer = LayerMask.NameToLayer("Interactable");
        int everythingExceptInteractables = ~(1 << interactableLayer);

        if (Physics.Raycast(currentPosition, movement.normalized, out RaycastHit hit, movement.magnitude, everythingExceptInteractables, QueryTriggerInteraction.Ignore))
        {
            OnHit(hit);
        }
        else
        {
            transform.position = nextPosition;
            if (velocity != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(velocity);
        }
    }

    public override void Launch(Vector3 direction, float force)
    {
        if (hasLaunched) return;

        hasLaunched = true;
        velocity = direction.normalized * bulletSpeed;
        transform.rotation = Quaternion.LookRotation(direction);
    }

    protected virtual void OnHit(RaycastHit hit)
    {
        // Apply impact force if possible
        if (hit.rigidbody != null)
        {
            Vector3 force = velocity * impactForceMultiplier;
            hit.rigidbody.AddForceAtPosition(force, hit.point, ForceMode.Impulse);
        }

        // Deal damage
        IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(10f, hit.point, velocity.normalized);
        }

        // Play impact sound
        audioHandler?.PlayImpactSound();

        // Spawn decal
        if (holeDecalSpawner != null)
        {
            holeDecalSpawner.SpawnDecal(hit.point, hit.normal, hit.transform);
        }

        // Immediately destroy bullet
        Destroy(gameObject);
    }
}
