using UnityEngine;

// IInteractable interface remains the same
public interface IInteractable
{
    void Interact(PlayerManager player); // Pass PlayerManager if interaction needs player context
    // Alternatively: void Interact(GameObject interactor); for more generic interaction
}

public class WorldInteractor : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private LayerMask interactableLayer = 1 << 9; // Example: Assign layer 9 as "Interactable" in the editor

    private PlayerManager playerManager;
    private PlayerLook playerLook; // Still useful for raycasting origin/direction

    private void Awake()
    {
        playerManager = GetComponentInParent<PlayerManager>();
        if (playerManager != null)
        {
            playerLook = playerManager.Look; // Get reference via PlayerManager
        }
        else
        {
            Debug.LogError("WorldInteractor could not find PlayerManager in parent!", this);
        }

        if (playerLook == null && playerManager != null)
        {
             Debug.LogError("WorldInteractor could not find PlayerLook via PlayerManager!", this);
        }

         // Ensure the layer is set - Default to 'Default' layer if not set to avoid issues
         if (interactableLayer == 0) { // LayerMask value is 0 if unset or set to 'Nothing'
              interactableLayer = 1; // Layer 0 is 'Default'
              Debug.LogWarning("Interactable Layer not set on WorldInteractor. Defaulting to 'Default' layer.", this);
         }
    }

    // This method will now be called by an event subscription in PlayerManager
    public void OnInteractPressed()
    {
        if (playerManager == null || playerLook == null)
            return;

        TryInteract();
    }

    private void TryInteract()
    {
        // Use PlayerLook to get the ray for interaction
        Ray ray = playerLook.GetLookRay();

        // Use the LayerMask assigned in the inspector
        if (Physics.Raycast(ray, out RaycastHit hit, interactionRange, interactableLayer, QueryTriggerInteraction.Collide))
        {
            // TryGetComponent is efficient
            if (hit.collider.TryGetComponent(out IInteractable interactable))
            {
                // Pass the PlayerManager reference for context if needed by the interactable
                interactable.Interact(playerManager);
                Debug.Log($"Interacted with {hit.collider.name}");
            }
            // else { Debug.Log($"Hit {hit.collider.name} but it has no IInteractable component."); }
        }
        // else { Debug.DrawRay(ray.origin, ray.direction * interactionRange, Color.red, 0.5f); } // Optional: Visualize failed interaction ray
    }
}