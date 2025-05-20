// --- Start of script: Assets/Logic/Input/PlayerInputHandler.cs ---
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
    public event Action StorePerformed;
    public event Action<Vector2> LookPerformed;          // For camera look
    public event Action<Vector2> RotateDeltaPerformed; // For item rotation delta (driven by Look when MMB is held)
    public event Action<float> ToolbarScrollPerformed;
    public event Action<int> ToolbarSlotSelected;
    public event Action ToggleInventoryPerformed;
    public event Action ToggleMenuPerformed;
    
    public event Action RotateHeldStarted;  // For MMB press
    public event Action RotateHeldEnded;    // For MMB release

    // UI Control Events
    public event Action UITabNavigatePerformed;
    public event Action UICancelPerformed;

    // State Properties
    public bool IsSprintHeld { get; private set; }
    public bool IsCrouchHeld { get; private set; }
    public bool IsSlowWalkHeld { get; private set; }
    public bool IsFire1Held { get; private set; }
    public bool IsFire2Held { get; private set; }
    public bool IsRotateHeld { get; private set; } // Tracks if MMB ("RotateHeld" action) is currently pressed

    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        if (playerInput == null) { Debug.LogError("PlayerInput missing on PlayerInputHandler!", this); this.enabled = false; return; }
    }

    private void OnEnable()
    {
        if (playerInput == null || playerInput.actions == null) return;

        SubscribeToAction("Gameplay", "Move", performed: HandleMovePerformed, canceled: HandleMoveCanceled);
        SubscribeToAction("Gameplay", "Look", performed: HandleLookPerformed); // Look input drives both camera and item rotation delta
        SubscribeToAction("Gameplay", "Jump", performed: HandleJumpPerformed);
        SubscribeToAction("Gameplay", "Sprint", started: HandleSprintStarted, canceled: HandleSprintCanceled);
        SubscribeToAction("Gameplay", "Crouch", started: HandleCrouchStarted, canceled: HandleCrouchCanceled);
        SubscribeToAction("Gameplay", "SlowWalk", started: HandleSlowWalkStarted, canceled: HandleSlowWalkCanceled);
        SubscribeToAction("Gameplay", "Fire1", started: HandleFire1Started, performed: HandleFire1Performed, canceled: HandleFire1Canceled);
        SubscribeToAction("Gameplay", "Fire2", started: HandleFire2Started, performed: HandleFire2Performed, canceled: HandleFire2Canceled);
        SubscribeToAction("Gameplay", "Utility", performed: HandleUtilityPerformed, canceled: HandleUtilityCanceled);
        SubscribeToAction("Gameplay", "Reload", performed: HandleReloadPerformed);
        SubscribeToAction("Gameplay", "Interact", performed: HandleInteractPerformed);
        SubscribeToAction("Gameplay", "Store", performed: HandleStorePerformed);
        SubscribeToAction("Gameplay", "ToolbarScroll", performed: HandleToolbarScrollPerformed);
        SubscribeToAction("Gameplay", "ToggleInventory", performed: HandleToggleInventoryPerformed);
        SubscribeToAction("Gameplay", "ToggleMenu", performed: HandleToggleMenuPerformed);
        SubscribeToAction("Gameplay", "RotateHeld", started: HandleRotateHeldStartedCallback, canceled: HandleRotateHeldEndedCallback);


        InputActionMap gameplayMap = playerInput.actions.FindActionMap("Gameplay");
        if (gameplayMap != null) {
            for (int i = 1; i <= 9; i++) {
                InputAction action = gameplayMap.FindAction($"ToolbarSlot{i}");
                if (action != null) { int slotIndex = i - 1; action.performed += ctx => HandleToolbarSlotSelected(slotIndex); }
            }
        } else { Debug.LogError("Gameplay Action Map not found!", this); }

        SubscribeToAction("UI", "Cancel", performed: HandleUICancelPerformed);
        SubscribeToAction("UI", "UITabNavigate", performed: HandleUITabNavigatePerformed);
    }

    private void OnDisable()
    {
        if (playerInput == null || playerInput.actions == null) return;

        UnsubscribeFromAction("Gameplay", "Move", performed: HandleMovePerformed, canceled: HandleMoveCanceled);
        UnsubscribeFromAction("Gameplay", "Look", performed: HandleLookPerformed);
        UnsubscribeFromAction("Gameplay", "Jump", performed: HandleJumpPerformed);
        UnsubscribeFromAction("Gameplay", "Sprint", started: HandleSprintStarted, canceled: HandleSprintCanceled);
        UnsubscribeFromAction("Gameplay", "Crouch", started: HandleCrouchStarted, canceled: HandleCrouchCanceled);
        UnsubscribeFromAction("Gameplay", "SlowWalk", started: HandleSlowWalkStarted, canceled: HandleSlowWalkCanceled);
        UnsubscribeFromAction("Gameplay", "Fire1", started: HandleFire1Started, performed: HandleFire1Performed, canceled: HandleFire1Canceled);
        UnsubscribeFromAction("Gameplay", "Fire2", started: HandleFire2Started, performed: HandleFire2Performed, canceled: HandleFire2Canceled);
        UnsubscribeFromAction("Gameplay", "Utility", performed: HandleUtilityPerformed, canceled: HandleUtilityCanceled);
        UnsubscribeFromAction("Gameplay", "Reload", performed: HandleReloadPerformed);
        UnsubscribeFromAction("Gameplay", "Interact", performed: HandleInteractPerformed);
        UnsubscribeFromAction("Gameplay", "Store", performed: HandleStorePerformed);
        UnsubscribeFromAction("Gameplay", "ToolbarScroll", performed: HandleToolbarScrollPerformed);
        UnsubscribeFromAction("Gameplay", "ToggleInventory", performed: HandleToggleInventoryPerformed);
        UnsubscribeFromAction("Gameplay", "ToggleMenu", performed: HandleToggleMenuPerformed);
        UnsubscribeFromAction("Gameplay", "RotateHeld", started: HandleRotateHeldStartedCallback, canceled: HandleRotateHeldEndedCallback);

        InputActionMap gameplayMap = playerInput.actions?.FindActionMap("Gameplay");
        if (gameplayMap != null) {
            for (int i = 1; i <= 9; i++) {
                InputAction action = gameplayMap.FindAction($"ToolbarSlot{i}");
                if (action != null) { int slotIndex = i - 1; action.performed -= ctx => HandleToolbarSlotSelected(slotIndex); }
            }
        }

        UnsubscribeFromAction("UI", "Cancel", performed: HandleUICancelPerformed);
        UnsubscribeFromAction("UI", "UITabNavigate", performed: HandleUITabNavigatePerformed);
    }

    private void HandleMovePerformed(InputAction.CallbackContext ctx) => MovePerformed?.Invoke(ctx.ReadValue<Vector2>());
    private void HandleMoveCanceled(InputAction.CallbackContext ctx) => MovePerformed?.Invoke(Vector2.zero);
    
    private void HandleLookPerformed(InputAction.CallbackContext ctx) {
        if (Time.timeScale < 0.01f) return;
        Vector2 lookDelta = ctx.ReadValue<Vector2>();

        LookPerformed?.Invoke(lookDelta); // Always send for camera look (PlayerManager will gate it if IsRotateHeld)
        
        if (IsRotateHeld) { // If MMB is held, also send this delta for item rotation
            RotateDeltaPerformed?.Invoke(lookDelta);
        }
    }

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
    private void HandleStorePerformed(InputAction.CallbackContext ctx) => StorePerformed?.Invoke();
    private void HandleToolbarScrollPerformed(InputAction.CallbackContext ctx) {
        float scrollY = ctx.ReadValue<Vector2>().y;
        if (Mathf.Abs(scrollY) > 0.01f) ToolbarScrollPerformed?.Invoke(Mathf.Sign(scrollY));
    }
    private void HandleToolbarSlotSelected(int slotIndex) => ToolbarSlotSelected?.Invoke(slotIndex);
    private void HandleToggleInventoryPerformed(InputAction.CallbackContext ctx) => ToggleInventoryPerformed?.Invoke();
    private void HandleToggleMenuPerformed(InputAction.CallbackContext ctx) => ToggleMenuPerformed?.Invoke();
    private void HandleUICancelPerformed(InputAction.CallbackContext ctx) => UICancelPerformed?.Invoke();
    private void HandleUITabNavigatePerformed(InputAction.CallbackContext ctx) => UITabNavigatePerformed?.Invoke();

    private void HandleRotateHeldStartedCallback(InputAction.CallbackContext ctx) {
        IsRotateHeld = true;
        RotateHeldStarted?.Invoke();
    }
    private void HandleRotateHeldEndedCallback(InputAction.CallbackContext ctx) {
        IsRotateHeld = false;
        RotateHeldEnded?.Invoke();
    }

    public void EnableGameplayControls() => playerInput?.SwitchCurrentActionMap("Gameplay");
    public void EnableUIControls() => playerInput?.SwitchCurrentActionMap("UI");

    private void SubscribeToAction(string mapName, string actionName, 
                                   Action<InputAction.CallbackContext> performed = null, 
                                   Action<InputAction.CallbackContext> canceled = null, 
                                   Action<InputAction.CallbackContext> started = null) {
        var action = playerInput?.actions?.FindActionMap(mapName)?.FindAction(actionName);
        if (action != null) {
            if (started != null) action.started += started;
            if (performed != null) action.performed += performed;
            if (canceled != null) action.canceled += canceled;
        } else { Debug.LogWarning($"Action '{actionName}' in map '{mapName}' not found during subscription.", this); }
    }

    private void UnsubscribeFromAction(string mapName, string actionName, 
                                     Action<InputAction.CallbackContext> performed = null, 
                                     Action<InputAction.CallbackContext> canceled = null, 
                                     Action<InputAction.CallbackContext> started = null) {
        if (playerInput?.actions == null) return;
        var action = playerInput.actions.FindActionMap(mapName)?.FindAction(actionName);
        if (action != null) {
             if (started != null) action.started -= started;
             if (performed != null) action.performed -= performed;
             if (canceled != null) action.canceled -= canceled;
        }
    }
}
// --- End of script: Assets/Logic/Input/PlayerInputHandler.cs ---