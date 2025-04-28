using UnityEngine;

public class ArrowProjectile : ProjectileBehavior
{
    [Header("Settings")]
    [SerializeField] private float impactForceMultiplier = 1f;

    [Header("Gravity Settings")]
    [SerializeField] private Vector3 gravity = new Vector3(0f, -9.81f, 0f);

    private bool hasLaunched = false;
    private bool hasHit = false;
    private Vector3 velocity;

    private GameObject arrowAnchor;

    [Header("References")]
    private ArrowAudioHandler audioHandler;
    private HoleDecalSpawner holeDecalSpawner;

    private void Start()
    {
        audioHandler = GetComponent<ArrowAudioHandler>();
        holeDecalSpawner = GetComponent<HoleDecalSpawner>();
        //Destroy(gameObject, maxLifetime);
    }

    private void FixedUpdate()
    {
        if (!hasLaunched || hasHit) return;

        // Apply gravity
        velocity += gravity * Time.fixedDeltaTime;
        Vector3 movement = velocity * Time.fixedDeltaTime;

        Vector3 currentPosition = transform.position;
        Vector3 nextPosition = currentPosition + movement;

        // Create a mask that ignores Interactable layer (let's say it's layer 8)
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

    private void LateUpdate()
    {
        if(!hasHit)
            return;

        if (arrowAnchor != null)
        {
            transform.position = arrowAnchor.transform.position;
            transform.rotation = arrowAnchor.transform.rotation;
        }
        else
            Destroy(gameObject);
    }

    public override void Launch(Vector3 direction, float force)
    {
        if (hasLaunched) return;

        hasLaunched = true;
        velocity = direction.normalized * force;
        transform.rotation = Quaternion.LookRotation(direction);
    }

    private void OnHit(RaycastHit hit)
    {
        hasHit = true;

        GameObject newAnchor = new GameObject();
        newAnchor.transform.position = hit.point;
        newAnchor.transform.rotation = Quaternion.LookRotation(velocity);

        newAnchor.transform.SetParent(hit.transform);

        arrowAnchor = newAnchor;

        // Apply force if applicable
        if (hit.rigidbody != null)
        {
            Vector3 force = velocity * impactForceMultiplier;
            hit.rigidbody.AddForceAtPosition(force, hit.point, ForceMode.Impulse);
        }

        //Apply damage
        IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(30f, hit.point, velocity.normalized); // Replace 5f with your damage value
        }

        audioHandler.PlayImpactSound();
        holeDecalSpawner.SpawnDecal(newAnchor.transform.position, hit.normal, arrowAnchor.transform);

        //Debug.Log("Arrow hit: " + hit.collider.name);
    }
}