using UnityEngine;

public interface IInteractable
{
    void Interact(PlayerManager player);
}

public class WorldInteractor : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float interactionRange = 3f;

    private PlayerManager playerManager;
    private PlayerLook playerLook;

    private void Awake()
    {
        playerManager = GetComponentInParent<PlayerManager>();
        if (playerManager != null)
        {
            playerLook = playerManager.Look;
        }
    }

    public void HandleInput(IPlayerInput input)
    {
        if (input == null || !input.InteractDown || playerManager == null || playerLook == null)
            return;

        TryInteract();
    }

    private void TryInteract()
    {
        Ray ray = playerLook.GetLookRay();

        LayerMask interactMask = LayerMask.GetMask("Interactable");
        if (Physics.Raycast(ray, out RaycastHit hit, interactionRange, interactMask, QueryTriggerInteraction.Collide))
        {
            if (hit.collider.TryGetComponent(out IInteractable interactable))
            {
                interactable.Interact(playerManager);
            }
        }
    }
}
