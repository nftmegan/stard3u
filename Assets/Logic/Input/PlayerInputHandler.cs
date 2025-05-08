using UnityEngine;
using UnityEngine.InputSystem;
using System;

[RequireComponent(typeof(PlayerInput))]
public class PlayerInputHandler : MonoBehaviour
{
    private PlayerInput playerInput;

    // Gameplay Events
    public event Action<Vector2> MovePerformed;
    public event Action JumpPerformed;
    public event Action SprintStarted, SprintCanceled;
    public event Action CrouchStarted, CrouchCanceled;
    public event Action SlowWalkStarted, SlowWalkCanceled;
    public event Action Fire1Started, Fire1Performed, Fire1Canceled;
    public event Action Fire2Started, Fire2Performed, Fire2Canceled;
    public event Action UtilityPerformed, UtilityCanceled;
    public event Action ReloadPerformed;
    public event Action InteractPerformed;
    public event Action StorePerformed; // <<< NEW EVENT for Store Action
    public event Action<Vector2> LookPerformed;
    public event Action<float> ToolbarScrollPerformed;
    public event Action<int> ToolbarSlotSelected;
    public event Action ToggleInventoryPerformed;
    public event Action ToggleMenuPerformed;

    // UI Control Events
    public event Action UITabNavigatePerformed;
    public event Action UICancelPerformed;

    // State Properties
    public bool IsSprintHeld { get; private set; }
    public bool IsCrouchHeld { get; private set; }
    public bool IsSlowWalkHeld { get; private set; }
    public bool IsFire1Held { get; private set; }
    public bool IsFire2Held { get; private set; }

    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        if (playerInput == null)
        {
             Debug.LogError("PlayerInput component missing on this GameObject!", this);
             this.enabled = false;
             return; // Added return
        }
    }

    private void OnEnable()
    {
        if (playerInput == null || playerInput.actions == null) return;

        // --- Gameplay Actions Subs ---
        SubscribeToAction("Gameplay", "Move", HandleMovePerformed, HandleMoveCanceled);
        SubscribeToAction("Gameplay", "Look", HandleLookPerformed);
        SubscribeToAction("Gameplay", "Jump", HandleJumpPerformed);
        SubscribeToAction("Gameplay", "Sprint", HandleSprintStarted, HandleSprintCanceled);
        SubscribeToAction("Gameplay", "Crouch", HandleCrouchStarted, HandleCrouchCanceled);
        SubscribeToAction("Gameplay", "SlowWalk", HandleSlowWalkStarted, HandleSlowWalkCanceled);
        SubscribeToAction("Gameplay", "Fire1", HandleFire1Started, HandleFire1Canceled, HandleFire1Performed);
        SubscribeToAction("Gameplay", "Fire2", HandleFire2Started, HandleFire2Canceled, HandleFire2Performed);
        SubscribeToAction("Gameplay", "Utility", HandleUtilityPerformed, HandleUtilityCanceled);
        SubscribeToAction("Gameplay", "Reload", HandleReloadPerformed);
        SubscribeToAction("Gameplay", "Interact", HandleInteractPerformed);
        SubscribeToAction("Gameplay", "Store", HandleStorePerformed); // <<< SUBSCRIBE TO NEW ACTION
        SubscribeToAction("Gameplay", "ToolbarScroll", HandleToolbarScrollPerformed);
        SubscribeToAction("Gameplay", "ToggleInventory", HandleToggleInventoryPerformed);
        SubscribeToAction("Gameplay", "ToggleMenu", HandleToggleMenuPerformed);

        // Toolbar Slots Subs
        InputActionMap gameplayMap = playerInput.actions.FindActionMap("Gameplay");
        if (gameplayMap != null) {
            for (int i = 1; i <= 9; i++) {
                InputAction action = gameplayMap.FindAction($"ToolbarSlot{i}");
                if (action != null) { int slotIndex = i - 1; action.performed += ctx => HandleToolbarSlotSelected(slotIndex); }
                 else { Debug.LogWarning($"ToolbarSlot{i} action not found in Input Actions.", this); } // Added warning
            }
        } else { Debug.LogError("Gameplay Action Map not found!", this); }


        // --- UI Actions Subs ---
        SubscribeToAction("UI", "Cancel", HandleUICancelPerformed);
        SubscribeToAction("UI", "UITabNavigate", HandleUITabNavigatePerformed);
    }

    private void OnDisable()
    {
        if (playerInput == null || playerInput.actions == null) return;

        // --- Gameplay Actions Unsubs ---
        UnsubscribeFromAction("Gameplay", "Move", HandleMovePerformed, HandleMoveCanceled);
        UnsubscribeFromAction("Gameplay", "Look", HandleLookPerformed);
        UnsubscribeFromAction("Gameplay", "Jump", HandleJumpPerformed);
        UnsubscribeFromAction("Gameplay", "Sprint", HandleSprintStarted, HandleSprintCanceled);
        UnsubscribeFromAction("Gameplay", "Crouch", HandleCrouchStarted, HandleCrouchCanceled);
        UnsubscribeFromAction("Gameplay", "SlowWalk", HandleSlowWalkStarted, HandleSlowWalkCanceled);
        UnsubscribeFromAction("Gameplay", "Fire1", HandleFire1Started, HandleFire1Canceled, HandleFire1Performed);
        UnsubscribeFromAction("Gameplay", "Fire2", HandleFire2Started, HandleFire2Canceled, HandleFire2Performed);
        UnsubscribeFromAction("Gameplay", "Utility", HandleUtilityPerformed, HandleUtilityCanceled);
        UnsubscribeFromAction("Gameplay", "Reload", HandleReloadPerformed);
        UnsubscribeFromAction("Gameplay", "Interact", HandleInteractPerformed);
        UnsubscribeFromAction("Gameplay", "Store", HandleStorePerformed); // <<< UNSUBSCRIBE FROM NEW ACTION
        UnsubscribeFromAction("Gameplay", "ToolbarScroll", HandleToolbarScrollPerformed);
        UnsubscribeFromAction("Gameplay", "ToggleInventory", HandleToggleInventoryPerformed);
        UnsubscribeFromAction("Gameplay", "ToggleMenu", HandleToggleMenuPerformed);

        // Toolbar Slot Unsubs (Simplified - assumes playerInput handles this internally on disable/destroy)
         InputActionMap gameplayMap = playerInput.actions?.FindActionMap("Gameplay"); // Safe navigation
         if (gameplayMap != null) {
             for (int i = 1; i <= 9; i++) {
                 InputAction action = gameplayMap.FindAction($"ToolbarSlot{i}");
                 if (action != null) {
                     // This is complex as we don't store the lambda easily.
                     // Relying on PlayerInput's own cleanup is often sufficient.
                     // Or use a more robust event management system if needed.
                 }
             }
         }

        // --- UI Actions Unsubs ---
        UnsubscribeFromAction("UI", "Cancel", HandleUICancelPerformed);
        UnsubscribeFromAction("UI", "UITabNavigate", HandleUITabNavigatePerformed);
    }

    // --- Action Handler Methods ---
    private void HandleMovePerformed(InputAction.CallbackContext ctx) => MovePerformed?.Invoke(ctx.ReadValue<Vector2>());
    private void HandleMoveCanceled(InputAction.CallbackContext ctx) => MovePerformed?.Invoke(Vector2.zero);
    private void HandleLookPerformed(InputAction.CallbackContext ctx) => LookPerformed?.Invoke(ctx.ReadValue<Vector2>());
    private void HandleJumpPerformed(InputAction.CallbackContext ctx) => JumpPerformed?.Invoke();
    private void HandleSprintStarted(InputAction.CallbackContext ctx) { SprintStarted?.Invoke(); IsSprintHeld = true; }
    private void HandleSprintCanceled(InputAction.CallbackContext ctx) { SprintCanceled?.Invoke(); IsSprintHeld = false; }
    private void HandleCrouchStarted(InputAction.CallbackContext ctx) { CrouchStarted?.Invoke(); IsCrouchHeld = true; }
    private void HandleCrouchCanceled(InputAction.CallbackContext ctx) { CrouchCanceled?.Invoke(); IsCrouchHeld = false; }
    private void HandleSlowWalkStarted(InputAction.CallbackContext ctx) { SlowWalkStarted?.Invoke(); IsSlowWalkHeld = true; }
    private void HandleSlowWalkCanceled(InputAction.CallbackContext ctx) { SlowWalkCanceled?.Invoke(); IsSlowWalkHeld = false; }
    private void HandleFire1Started(InputAction.CallbackContext ctx) { Fire1Started?.Invoke(); IsFire1Held = true; }
    private void HandleFire1Performed(InputAction.CallbackContext ctx) => Fire1Performed?.Invoke();
    private void HandleFire1Canceled(InputAction.CallbackContext ctx) { Fire1Canceled?.Invoke(); IsFire1Held = false; }
    private void HandleFire2Started(InputAction.CallbackContext ctx) { Fire2Started?.Invoke(); IsFire2Held = true; }
    private void HandleFire2Performed(InputAction.CallbackContext ctx) => Fire2Performed?.Invoke();
    private void HandleFire2Canceled(InputAction.CallbackContext ctx) { Fire2Canceled?.Invoke(); IsFire2Held = false; }
    private void HandleUtilityPerformed(InputAction.CallbackContext ctx) => UtilityPerformed?.Invoke();
    private void HandleUtilityCanceled(InputAction.CallbackContext ctx) => UtilityCanceled?.Invoke();
    private void HandleReloadPerformed(InputAction.CallbackContext ctx) => ReloadPerformed?.Invoke();
    private void HandleInteractPerformed(InputAction.CallbackContext ctx) => InteractPerformed?.Invoke();
    private void HandleStorePerformed(InputAction.CallbackContext ctx) => StorePerformed?.Invoke(); // <<< NEW HANDLER
    private void HandleToolbarScrollPerformed(InputAction.CallbackContext ctx) {
        float scrollY = ctx.ReadValue<Vector2>().y;
        if (Mathf.Abs(scrollY) > 0.01f) ToolbarScrollPerformed?.Invoke(Mathf.Sign(scrollY));
    }
    private void HandleToolbarSlotSelected(int slotIndex) => ToolbarSlotSelected?.Invoke(slotIndex);
    private void HandleToggleInventoryPerformed(InputAction.CallbackContext ctx) => ToggleInventoryPerformed?.Invoke();
    private void HandleToggleMenuPerformed(InputAction.CallbackContext ctx) => ToggleMenuPerformed?.Invoke();
    private void HandleUICancelPerformed(InputAction.CallbackContext ctx) => UICancelPerformed?.Invoke();
    private void HandleUITabNavigatePerformed(InputAction.CallbackContext ctx) => UITabNavigatePerformed?.Invoke();

    // --- Methods to Switch Action Maps ---
    public void EnableGameplayControls() => playerInput?.SwitchCurrentActionMap("Gameplay");
    public void EnableUIControls() => playerInput?.SwitchCurrentActionMap("UI");

    // --- Helper Methods for Sub/Unsub ---
    private void SubscribeToAction(string mapName, string actionName, Action<InputAction.CallbackContext> performed = null, Action<InputAction.CallbackContext> canceled = null, Action<InputAction.CallbackContext> started = null) {
        var action = playerInput?.actions?.FindActionMap(mapName)?.FindAction(actionName);
        if (action != null) {
            if (started != null) action.started += started;
            if (performed != null) action.performed += performed;
            if (canceled != null) action.canceled += canceled;
        } else {
            Debug.LogWarning($"Action '{actionName}' not found in map '{mapName}' during subscription.", this);
        }
    }
    private void UnsubscribeFromAction(string mapName, string actionName, Action<InputAction.CallbackContext> performed = null, Action<InputAction.CallbackContext> canceled = null, Action<InputAction.CallbackContext> started = null) {
         // Check playerInput and actions validity before proceeding
        if (playerInput == null || playerInput.actions == null) return;

        var action = playerInput.actions.FindActionMap(mapName)?.FindAction(actionName);
        if (action != null) {
            // Use ?. operator for safe unsubscription
             if (started != null) action.started -= started;
             if (performed != null) action.performed -= performed;
             if (canceled != null) action.canceled -= canceled;
        }
         // No warning on unsubscribe if not found, might have already unsubscribed or never subscribed.
    }
}