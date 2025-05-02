using UnityEngine;
using System;

// REMOVED: [RequireComponent(typeof(PlayerInputHandler))]
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
        uiStateController ??= GetComponentInChildren<UIStateController>(true); // Might need FindObjectOfType
        inventoryUIManager ??= FindFirstObjectByType<InventoryUIManager>(); // Find
        uiPanelRegistry ??= FindFirstObjectByType<UIPanelRegistry>();       // Often global Canvas or Manager

        // Validate components
        if (inputHandler == null) Debug.LogError("[PlayerManager] PlayerInputHandler missing!", this);
        if (uiStateController == null) Debug.LogError("[PlayerManager] UIStateController missing!", this);
        if (playerInventory == null) Debug.LogError("[PlayerManager] PlayerInventory missing!", this);
        if (uiPanelRegistry == null) Debug.LogError("[PlayerManager] UIPanelRegistry missing in scene!", this);
        // ... other validation checks ...

        // Hook registry (initial state applied within Hook now)
        // uiPanelRegistry?.Hook(uiStateController);
        // Subscribe to state changes
        if (uiStateController != null) { uiStateController.OnStateChanged += HandleUIStateChange; }
    }

    private void Start()
    {
        // PlayerInventory initializes itself in its own Start/OnEnable

        // Show Inventory UI
        if (inventoryUIManager != null && playerInventory != null)
        {
            inventoryUIManager.Show(playerInventory);
        }
        // else Debug.LogWarning("Player Inventory UI cannot be shown - manager or inventory missing.");


        // --- <<< HOOK UP UI REGISTRY IN START >>> ---
        if (uiPanelRegistry != null && uiStateController != null)
        {
            // Debug.Log($"[PlayerManager START] Hooking UIPanelRegistry ({uiPanelRegistry.name}) to UIStateController ({uiStateController.name}).");
            uiPanelRegistry.Hook(uiStateController); // Hook *now* correctly applies initial state
        }
        else if (uiStateController == null)
        {
            Debug.LogWarning("[PlayerManager START] UIStateController missing, cannot hook registry or set initial state properly.");
            // Apply default input map state
            HandleUIStateChange(new UIStateChanged(UIState.Gameplay, UIState.Gameplay));
        }
        else // Registry is null
        {
            Debug.LogWarning("[PlayerManager START] UIPanelRegistry missing, UI panels won't be managed by state.");
            // Apply input map based on controller's initial state
            HandleUIStateChange(new UIStateChanged(UIState.Gameplay, uiStateController.Current));
        }
        // --- <<< END HOOK >>> ---
    }
    
    private void OnEnable()
    {
        if (inputHandler == null) return;
        // Gameplay Actions
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
        inputHandler.Fire1Canceled += HandleFire1Cancel;
        inputHandler.Fire2Started += HandleFire2Start;
        inputHandler.Fire2Canceled += HandleFire2Cancel;
        inputHandler.ReloadPerformed += HandleReload;
        inputHandler.UtilityPerformed += HandleUtilityStart;
        inputHandler.UtilityCanceled += HandleUtilityCancel;
        inputHandler.ToggleInventoryPerformed += HandleToggleInventory; // Listens for Tab press in Gameplay map
        inputHandler.ToggleMenuPerformed += HandleToggleMenu;          // Listens for Esc press in Gameplay map

        // UI Actions
        inputHandler.UICancelPerformed += HandleUICancel;              // Listens for Esc press in UI map
        inputHandler.UITabNavigatePerformed += HandleUITabNavigate;    // Listens for Tab press in UI map
    }

    private void OnDisable()
    {
        if (inputHandler != null)
        {
            // Gameplay Actions
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
            inputHandler.Fire1Canceled -= HandleFire1Cancel;
            inputHandler.Fire2Started -= HandleFire2Start;
            inputHandler.Fire2Canceled -= HandleFire2Cancel;
            inputHandler.ReloadPerformed -= HandleReload;
            inputHandler.UtilityPerformed -= HandleUtilityStart;
            inputHandler.UtilityCanceled -= HandleUtilityCancel;
            inputHandler.ToggleInventoryPerformed -= HandleToggleInventory;
            inputHandler.ToggleMenuPerformed -= HandleToggleMenu;

            // UI Actions
            inputHandler.UICancelPerformed -= HandleUICancel;
            inputHandler.UITabNavigatePerformed -= HandleUITabNavigate;
        }
        if (uiStateController != null) { uiStateController.OnStateChanged -= HandleUIStateChange; }
    }

    private void Update() {
        if (characterController != null && playerLook != null) characterController.SetLookDirection(playerLook.GetLookDirection());
        if (inputHandler != null && equipmentController != null) {
            if (inputHandler.IsFire1Held) equipmentController.HandleFire1Hold();
            if (inputHandler.IsFire2Held) equipmentController.HandleFire2Hold();
        }
    }

    private void LateUpdate() {
         if (characterController != null && playerLook != null) {
             Vector3 targetPos = characterController.GetSmoothedHeadWorldPosition();
             if(playerLook != null) playerLook.transform.position = Vector3.Lerp( playerLook.transform.position, targetPos, Time.deltaTime * 20f);
         }
    }

    // --- Handlers ---
    private void HandleUIStateChange(UIStateChanged eventArgs) {
        if (inputHandler == null) return;
        if (eventArgs.Current == UIState.Gameplay) inputHandler.EnableGameplayControls();
        else inputHandler.EnableUIControls();
     }

    // Gameplay Handlers...
    private void HandleMoveInput(Vector2 m) => characterController?.SetMoveInput(m);
    private void HandleLookInput(Vector2 l) => playerLook?.SetLookInput(l);
    private void HandleJumpInput() => characterController?.OnJumpPressed();
    private void HandleInteract() => worldInteractor?.OnInteractPressed();
    private void HandleSprintStart() => characterController?.SetSprint(true);
    private void HandleSprintCancel() => characterController?.SetSprint(false);
    private void HandleCrouchStart() => characterController?.SetCrouch(true);
    private void HandleCrouchCancel() => characterController?.SetCrouch(false);
    private void HandleSlowWalkStart() => characterController?.SetSlowWalk(true);
    private void HandleSlowWalkCancel() => characterController?.SetSlowWalk(false);
    private void HandleToolbarScroll(float dir) => playerInventory?.HandleToolbarScroll(dir);
    private void HandleToolbarSlotSelection(int idx) => playerInventory?.HandleToolbarSlotSelection(idx);
    private void HandleFire1Start() => equipmentController?.HandleFire1Down();
    private void HandleFire1Cancel() => equipmentController?.HandleFire1Up();
    private void HandleFire2Start() => equipmentController?.HandleFire2Down();
    private void HandleFire2Cancel() => equipmentController?.HandleFire2Up();
    private void HandleReload() => equipmentController?.HandleReloadDown();
    private void HandleUtilityStart() => equipmentController?.HandleUtilityDown();
    private void HandleUtilityCancel() => equipmentController?.HandleUtilityUp();

    // UI Toggle/Cancel Handlers...
    private void HandleToggleInventory() { if (uiStateController?.Current == UIState.Gameplay) uiStateController.SetState(UIState.Inventory); }
    private void HandleToggleMenu() { if (uiStateController?.Current == UIState.Gameplay) uiStateController.SetState(UIState.Menu); }
    private void HandleUICancel() { if (uiStateController?.IsUIOpen ?? false) uiStateController.SetState(UIState.Gameplay); }
    private void HandleUITabNavigate() {
         if (uiStateController == null) return;
         if (uiStateController.Current == UIState.Inventory) { uiStateController.SetState(UIState.Gameplay); }
         else if (uiStateController.Current == UIState.Menu) { /* TODO: Menu Tab Logic */ Debug.Log("Tab in Menu - Navigation NYI"); }
    }

    // Public Helper...
    public void ForceSlowWalk(bool v) => characterController?.ForceSlowWalk(v);
}