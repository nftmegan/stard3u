using UnityEngine;
using System;
using UnityEngine.InputSystem; // Make sure this is included

public class PlayerManager : MonoBehaviour
{
    [Header("Core Components (Auto-find where possible)")]
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private PlayerLook playerLook;
    [SerializeField] private MyCharacterController characterController;
    [SerializeField] private EquipmentController equipmentController;
    [SerializeField] private WorldInteractor worldInteractor;
    [SerializeField] private PlayerInputHandler inputHandler; // Assign or Find

    [Header("UI Components (Assign or Find)")]
    [SerializeField] private InventoryUIManager inventoryUIManager; // Needs to be assigned or found
    [SerializeField] private UIStateController uiStateController;   // Needs to be assigned or found
    [SerializeField] private UIPanelRegistry uiPanelRegistry;     // Needs to be assigned or found

    // Public accessors
    public PlayerLook Look => playerLook;
    public MyCharacterController CharacterController => characterController;
    public EquipmentController Equipment => equipmentController;
    public WorldInteractor Interactor => worldInteractor;
    public PlayerInventory Inventory => playerInventory;
    public PlayerInputHandler InputHandler => inputHandler;

    private void Awake()
    {
        // Find components
        inputHandler ??= GetComponentInChildren<PlayerInputHandler>(true);
        playerInventory ??= GetComponentInChildren<PlayerInventory>(true);
        playerLook ??= GetComponentInChildren<PlayerLook>(true);
        characterController ??= GetComponentInChildren<MyCharacterController>(true);
        equipmentController ??= GetComponentInChildren<EquipmentController>(true);
        worldInteractor ??= GetComponentInChildren<WorldInteractor>(true);
        // Use FindFirstObjectByType for potentially scene-wide singletons
        uiStateController ??= FindFirstObjectByType<UIStateController>();
        inventoryUIManager ??= FindFirstObjectByType<InventoryUIManager>();
        uiPanelRegistry ??= FindFirstObjectByType<UIPanelRegistry>();

        // Validate components
        if (inputHandler == null) Debug.LogError("[PlayerManager] PlayerInputHandler missing!", this);
        if (uiStateController == null) Debug.LogError("[PlayerManager] UIStateController missing!", this);
        if (playerInventory == null) Debug.LogError("[PlayerManager] PlayerInventory missing!", this);
        if (equipmentController == null) Debug.LogError("[PlayerManager] EquipmentController missing!", this);
        if (worldInteractor == null) Debug.LogError("[PlayerManager] WorldInteractor missing!", this);
        if (playerLook == null) Debug.LogError("[PlayerManager] PlayerLook missing!", this);
        if (characterController == null) Debug.LogError("[PlayerManager] CharacterController missing!", this);
        // Warnings for potentially optional UI components
        if (uiPanelRegistry == null) Debug.LogWarning("[PlayerManager] UIPanelRegistry missing in scene.", this);
        if (inventoryUIManager == null) Debug.LogWarning("[PlayerManager] InventoryUIManager missing in scene.", this);

        // Subscribe to state changes
        if (uiStateController != null) { uiStateController.OnStateChanged += HandleUIStateChange; }
         else { Debug.LogWarning("[PlayerManager] UIStateController null in Awake, cannot subscribe to state changes.", this); }
    }

    private void Start()
    {
        // Hook registry
        if (uiPanelRegistry != null && uiStateController != null) {
            uiPanelRegistry.Hook(uiStateController);
        } else if (uiStateController == null) {
            Debug.LogWarning("[PlayerManager START] UIStateController missing, cannot hook registry or set input state properly.");
            // Manually set initial input state if UI controller is missing
             if (inputHandler != null) inputHandler.EnableGameplayControls();
        } else { // Registry is null
            Debug.LogWarning("[PlayerManager START] UIPanelRegistry missing, UI panels won't be managed.");
            // Set input state based on UI controller's initial state even without registry
             HandleUIStateChange(new UIStateChanged(uiStateController.Current, uiStateController.Current));
        }

        // Show Inventory UI (if available)
        if (inventoryUIManager != null && playerInventory != null) {
            inventoryUIManager.Show(playerInventory);
        }
    }

    private void OnEnable()
    {
        if (inputHandler == null) {
             Debug.LogError("[PlayerManager] Cannot subscribe input events - PlayerInputHandler is null!", this);
             return;
        }

        // Gameplay Actions Subscriptions
        inputHandler.MovePerformed += HandleMoveInput;
        inputHandler.LookPerformed += HandleLookInput;
        inputHandler.JumpPerformed += HandleJumpInput;
        inputHandler.SprintStarted += HandleSprintStart;
        inputHandler.SprintCanceled += HandleSprintCancel;
        inputHandler.CrouchStarted += HandleCrouchStart;
        inputHandler.CrouchCanceled += HandleCrouchCancel;
        inputHandler.SlowWalkStarted += HandleSlowWalkStart;
        inputHandler.SlowWalkCanceled += HandleSlowWalkCancel;
        inputHandler.ToolbarScrollPerformed += HandleToolbarScroll;
        inputHandler.ToolbarSlotSelected += HandleToolbarSlotSelection;
        inputHandler.InteractPerformed += HandleInteract;
        inputHandler.Fire1Started += HandleFire1Start;
        // inputHandler.Fire1Performed += HandleFire1Performed; // Subscribe only if HandleFire1Performed is implemented
        inputHandler.Fire1Canceled += HandleFire1Cancel;
        inputHandler.Fire2Started += HandleFire2Start;
        // inputHandler.Fire2Performed += HandleFire2Performed; // Subscribe only if HandleFire2Performed is implemented
        inputHandler.Fire2Canceled += HandleFire2Cancel;
        inputHandler.ReloadPerformed += HandleReload;
        inputHandler.StorePerformed += HandleStore; // <<< SUBSCRIBE TO STORE EVENT
        inputHandler.UtilityPerformed += HandleUtilityStart;
        inputHandler.UtilityCanceled += HandleUtilityCancel;
        inputHandler.ToggleInventoryPerformed += HandleToggleInventory;
        inputHandler.ToggleMenuPerformed += HandleToggleMenu;

        // UI Actions Subscriptions
        inputHandler.UICancelPerformed += HandleUICancel;
        inputHandler.UITabNavigatePerformed += HandleUITabNavigate;
    }

    private void OnDisable()
    {
        // Unsubscribe from all events to prevent issues
        if (inputHandler != null) {
            inputHandler.MovePerformed -= HandleMoveInput;
            inputHandler.LookPerformed -= HandleLookInput;
            inputHandler.JumpPerformed -= HandleJumpInput;
            inputHandler.SprintStarted -= HandleSprintStart;
            inputHandler.SprintCanceled -= HandleSprintCancel;
            inputHandler.CrouchStarted -= HandleCrouchStart;
            inputHandler.CrouchCanceled -= HandleCrouchCancel;
            inputHandler.SlowWalkStarted -= HandleSlowWalkStart;
            inputHandler.SlowWalkCanceled -= HandleSlowWalkCancel;
            inputHandler.ToolbarScrollPerformed -= HandleToolbarScroll;
            inputHandler.ToolbarSlotSelected -= HandleToolbarSlotSelection;
            inputHandler.InteractPerformed -= HandleInteract;
            inputHandler.Fire1Started -= HandleFire1Start;
            // inputHandler.Fire1Performed -= HandleFire1Performed;
            inputHandler.Fire1Canceled -= HandleFire1Cancel;
            inputHandler.Fire2Started -= HandleFire2Start;
            // inputHandler.Fire2Performed -= HandleFire2Performed;
            inputHandler.Fire2Canceled -= HandleFire2Cancel;
            inputHandler.ReloadPerformed -= HandleReload;
            inputHandler.StorePerformed -= HandleStore; // <<< UNSUBSCRIBE FROM STORE EVENT
            inputHandler.UtilityPerformed -= HandleUtilityStart;
            inputHandler.UtilityCanceled -= HandleUtilityCancel;
            inputHandler.ToggleInventoryPerformed -= HandleToggleInventory;
            inputHandler.ToggleMenuPerformed -= HandleToggleMenu;

            inputHandler.UICancelPerformed -= HandleUICancel;
            inputHandler.UITabNavigatePerformed -= HandleUITabNavigate;
        }
        if (uiStateController != null) { uiStateController.OnStateChanged -= HandleUIStateChange; }
    }

    private void Update() {
        // Forward look direction
        if (characterController != null && playerLook != null) {
            characterController.SetLookDirection(playerLook.GetLookDirection());
        }

        // Handle held inputs
        if (inputHandler != null && equipmentController != null) {
            if (inputHandler.IsFire1Held) equipmentController.HandleFire1Hold();
            if (inputHandler.IsFire2Held) equipmentController.HandleFire2Hold();
        }
    }

    private void LateUpdate() {
        // Sync camera position
         if (characterController != null && playerLook != null) {
             Vector3 targetPos = characterController.GetSmoothedHeadWorldPosition();
             playerLook.transform.position = Vector3.Lerp(playerLook.transform.position, targetPos, Time.deltaTime * 20f);
         }
    }

    // --- Input Event Handlers ---

    private void HandleUIStateChange(UIStateChanged eventArgs) {
        if (inputHandler == null) return;
        // Switch Input Action Map based on UI state
        if (eventArgs.Current == UIState.Gameplay) {
            inputHandler.EnableGameplayControls();
        } else {
            inputHandler.EnableUIControls();
        }
     }

    // --- Gameplay Action Handlers ---
    private void HandleMoveInput(Vector2 m) => characterController?.SetMoveInput(m);
    private void HandleLookInput(Vector2 l) => playerLook?.SetLookInput(l);
    private void HandleJumpInput() => characterController?.OnJumpPressed();
    private void HandleInteract() => worldInteractor?.OnInteractPressed(); // 'E' interacts
    private void HandleSprintStart() => characterController?.SetSprint(true);
    private void HandleSprintCancel() => characterController?.SetSprint(false);
    private void HandleCrouchStart() => characterController?.SetCrouch(true);
    private void HandleCrouchCancel() => characterController?.SetCrouch(false);
    private void HandleSlowWalkStart() => characterController?.SetSlowWalk(true);
    private void HandleSlowWalkCancel() => characterController?.SetSlowWalk(false);
    private void HandleToolbarScroll(float dir) => playerInventory?.HandleToolbarScroll(dir);
    private void HandleToolbarSlotSelection(int idx) => playerInventory?.HandleToolbarSlotSelection(idx);

    // Forward actions to the currently equipped item/behavior via EquipmentController
    private void HandleFire1Start() => equipmentController?.HandleFire1Down();
    private void HandleFire1Cancel() => equipmentController?.HandleFire1Up();
    // If you need specific logic on the *frame* the button is pressed (not just down/up), implement these:
    // private void HandleFire1Performed(InputAction.CallbackContext ctx) => equipmentController?.HandleFire1Performed(); // Requires method in EquipmentController
    private void HandleFire2Start() => equipmentController?.HandleFire2Down();
    private void HandleFire2Cancel() => equipmentController?.HandleFire2Up();
    // private void HandleFire2Performed(InputAction.CallbackContext ctx) => equipmentController?.HandleFire2Performed(); // Requires method in EquipmentController
    private void HandleReload() => equipmentController?.HandleReloadDown(); // 'R' for reload
    private void HandleStore() => equipmentController?.HandleStoreDown();   // 'T' (or other) for store
    private void HandleUtilityStart() => equipmentController?.HandleUtilityDown();
    private void HandleUtilityCancel() => equipmentController?.HandleUtilityUp();
    // --- End Gameplay Action Handlers ---


    // --- UI Toggle/Cancel Handlers ---
    private void HandleToggleInventory() { uiStateController?.ToggleState(UIState.Inventory); }
    private void HandleToggleMenu() { uiStateController?.ToggleState(UIState.Menu); }
    private void HandleUICancel() { // Primarily for Esc key in UI map
        if (uiStateController?.IsUIOpen ?? false) {
            uiStateController.SetState(UIState.Gameplay);
        }
        // Optional: If gameplay state and Esc pressed, maybe open Menu?
        // else if (uiStateController?.Current == UIState.Gameplay) {
        //     uiStateController.SetState(UIState.Menu);
        // }
     }
    private void HandleUITabNavigate() { // Example: Tab out of inventory
         if (uiStateController == null) return;
         if (uiStateController.Current == UIState.Inventory) {
             uiStateController.SetState(UIState.Gameplay);
         }
         else if (uiStateController.Current == UIState.Menu) {
             // Implement tabbing within the menu if needed
             Debug.Log("Tab in Menu - Navigation NYI");
         }
         // Add other potential Tab behaviors in UI states
    }

    // --- Public Helper Methods ---
    public void ForceSlowWalk(bool v) => characterController?.ForceSlowWalk(v);
}