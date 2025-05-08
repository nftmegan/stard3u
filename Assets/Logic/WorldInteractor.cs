using UnityEngine;

// Interface for objects that can be interacted with using the 'Interact' key (e.g., 'E')
public interface IInteractable
{
    // Pass PlayerManager if interaction needs player context (inventory, stats, etc.)
    void Interact(PlayerManager player); 
    // Alternatively: void Interact(GameObject interactor); for more generic interaction
}

public class WorldInteractor : MonoBehaviour
{
    [Header("Interaction Settings")]
    [Tooltip("Maximum distance to check for interactable objects.")]
    [SerializeField] private float interactionRange = 3f;

    [Tooltip("Assign the specific layer(s) containing objects with IInteractable components (e.g., buttons, switches, NPCs). This should be different from layers used for grabbing parts.")]
    [SerializeField] private LayerMask interactableLayer = 0; // <<--- ASSIGN THIS IN INSPECTOR

    [Header("References")]
    [Tooltip("Reference to the PlayerLook component for raycasting direction.")]
    [SerializeField] private PlayerLook playerLook; // Reference needed for ray origin/direction

    // Reference to PlayerManager, usually obtained from parent or passed in
    private PlayerManager playerManager;

    private void Awake()
    {
        // Find PlayerManager (e.g., in parent)
        playerManager = GetComponentInParent<PlayerManager>();
        if (playerManager == null)
        {
            Debug.LogError("[WorldInteractor] Could not find PlayerManager in parent!", this);
        }

        // Attempt to get PlayerLook if not assigned
        if (playerLook == null && playerManager != null)
        {
            playerLook = playerManager.Look;
        }
        // Final check if PlayerLook is still missing
        if (playerLook == null)
        {
             Debug.LogError("[WorldInteractor] PlayerLook component is not assigned and could not be found via PlayerManager!", this);
             this.enabled = false; // Disable if look component is missing
             return;
        }

        // Ensure the interactable layer mask is assigned in the editor
         if (interactableLayer.value == 0) { // LayerMask value is 0 if unset or set to 'Nothing'
              Debug.LogWarning($"[WorldInteractor] Interactable LayerMask is not set on {gameObject.name}. Interaction will likely fail. Assign a layer in the Inspector.", this);
              // You could optionally default it here, but it's better to force assignment:
              // interactableLayer = LayerMask.GetMask("Interactable"); // Example if you have a layer named "Interactable"
              // interactableLayer = 1; // Default layer (usually not desired)
         }
    }

    // This method is called by PlayerManager when the 'Interact' input action is performed.
    public void OnInteractPressed()
    {
        if (playerManager == null || playerLook == null || interactableLayer.value == 0) {
            // Don't interact if setup is incomplete or layer mask is unassigned
            if (interactableLayer.value == 0) Debug.LogWarning("[WorldInteractor] Cannot interact: Interactable LayerMask not set.", this);
            return;
        }

        TryInteract();
    }

    private void TryInteract()
    {
        // Use PlayerLook to get the camera's ray for interaction
        Ray ray = playerLook.GetLookRay();

        // Raycast using the dedicated interactableLayer mask assigned in the inspector
        if (Physics.Raycast(ray, out RaycastHit hit, interactionRange, interactableLayer, QueryTriggerInteraction.Collide))
        {
            // Check if the hit object has an IInteractable component
            // Using GetComponentInParent allows interaction triggers on child colliders
            if (hit.collider.TryGetComponent<IInteractable>(out IInteractable interactable)) 
            // Alternatively, use GetComponentInParent if interactable logic is on a parent object:
            // IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
            // if (interactable != null)
            {
                // Found an interactable object, call its Interact method
                interactable.Interact(playerManager);
                // Optional: Debug log
                // Debug.Log($"[WorldInteractor] Interacted with {hit.collider.name}");
            }
            // else { Debug.Log($"[WorldInteractor] Hit {hit.collider.name} on Interactable layer, but it has no IInteractable component."); }
        }
        // else { Debug.DrawRay(ray.origin, ray.direction * interactionRange, Color.red, 0.5f); } // Optional: Visualize failed interaction ray
    }
}