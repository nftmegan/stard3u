// --- Start of script: Assets/Logic/Player/PlayerManager.cs ---
using UnityEngine;
using System;
using UnityEngine.InputSystem;

public class PlayerManager : MonoBehaviour
{
    [Header("Core Components (Auto-find where possible)")]
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private PlayerLook playerLook;
    [SerializeField] private MyCharacterController characterController;
    [SerializeField] private EquipmentController equipmentController;
    [SerializeField] private WorldInteractor worldInteractor;
    [SerializeField] private PlayerInputHandler inputHandler;
    [SerializeField] private PlayerGrabController grabController;

    [Header("UI Components (Assign or Find)")]
    [SerializeField] private InventoryUIManager inventoryUIManager;
    [SerializeField] private UIStateController uiStateController;
    [SerializeField] private UIPanelRegistry uiPanelRegistry;

    // Public accessors
    public PlayerLook Look => playerLook;
    public MyCharacterController CharacterController => characterController;
    public EquipmentController Equipment => equipmentController;
    public WorldInteractor Interactor => worldInteractor;
    public PlayerInventory Inventory => playerInventory;
    public PlayerInputHandler InputHandler => inputHandler;
    public PlayerGrabController GrabController => grabController;

    private bool isRotatingGrabbedItem = false; // State to track if player is actively rotating a grabbed item

    private void Awake()
    {
        inputHandler ??= GetComponentInChildren<PlayerInputHandler>(true);
        playerInventory ??= GetComponentInChildren<PlayerInventory>(true);
        playerLook ??= GetComponentInChildren<PlayerLook>(true);
        characterController ??= GetComponentInChildren<MyCharacterController>(true);
        equipmentController ??= GetComponentInChildren<EquipmentController>(true);
        worldInteractor ??= GetComponentInChildren<WorldInteractor>(true);
        grabController ??= GetComponentInChildren<PlayerGrabController>(true);
        uiStateController ??= FindFirstObjectByType<UIStateController>();
        inventoryUIManager ??= FindFirstObjectByType<InventoryUIManager>();
        uiPanelRegistry ??= FindFirstObjectByType<UIPanelRegistry>();

        // --- Validation --- //
        if (inputHandler == null) Debug.LogError("[PM] Input Handler Missing", this);
        if (playerInventory == null) Debug.LogError("[PM] PlayerInventory Missing", this);
        if (playerLook == null) Debug.LogError("[PM] PlayerLook Missing", this);
        if (characterController == null) Debug.LogError("[PM] CharacterController Missing", this);
        if (equipmentController == null) Debug.LogError("[PM] EquipmentController Missing", this);
        if (worldInteractor == null) Debug.LogError("[PM] WorldInteractor Missing", this);
        if (grabController == null) Debug.LogError("[PM] Grab Controller Missing", this);
        if (uiStateController == null) Debug.LogError("[PM] UIStateController Missing", this);


        if (uiPanelRegistry == null) Debug.LogWarning("[PM] UIPanelRegistry missing in scene.", this);
        if (inventoryUIManager == null) Debug.LogWarning("[PM] InventoryUIManager missing in scene.", this);

        if (uiStateController != null) uiStateController.OnStateChanged += HandleUIStateChange;
        else Debug.LogWarning("[PM] UIStateController null in Awake, cannot subscribe to state changes.", this);

        if (grabController != null)
        {
            grabController.InitializeController(this);
            grabController.OnGrabStateChanged += HandleGrabStateChanged;
        } else Debug.LogError("[PM] Grab Controller Null! Grab functionality will be broken.", this);
    }

    private void Start()
    {
        if (uiPanelRegistry != null && uiStateController != null) uiPanelRegistry.Hook(uiStateController);
        else if (uiStateController == null) { if (inputHandler != null) inputHandler.EnableGameplayControls(); Debug.LogWarning("[PM START] UIStateController missing."); }
        else { HandleUIStateChange(new UIStateChanged(uiStateController.Current, uiStateController.Current)); Debug.LogWarning("[PM START] UIPanelRegistry missing."); }

        if (inventoryUIManager != null && playerInventory != null) inventoryUIManager.Show(playerInventory);
        else Debug.LogWarning("[PM START] InventoryUIManager or PlayerInventory missing.");

        equipmentController?.ManualStart();
    }

    private void OnEnable()
    {
        if (inputHandler == null) { Debug.LogError("[PM] Input Handler Null on Enable. Cannot subscribe events.", this); return; }

        // Gameplay Actions Subscriptions
        inputHandler.MovePerformed += HandleMoveInput;
        inputHandler.LookPerformed += HandleLookInput;
        inputHandler.JumpPerformed += HandleJumpInput;
        inputHandler.SprintStarted += HandleSprintStart; inputHandler.SprintCanceled += HandleSprintCancel;
        inputHandler.CrouchStarted += HandleCrouchStart; inputHandler.CrouchCanceled += HandleCrouchCancel;
        inputHandler.SlowWalkStarted += HandleSlowWalkStart; inputHandler.SlowWalkCanceled += HandleSlowWalkCancel;
        inputHandler.ToolbarScrollPerformed += HandleToolbarScroll;
        inputHandler.ToolbarSlotSelected += HandleToolbarSlotSelection;
        inputHandler.InteractPerformed += HandleInteract;
        inputHandler.Fire1Started += HandleFire1Start; inputHandler.Fire1Canceled += HandleFire1Cancel;
        inputHandler.Fire2Started += HandleFire2Start; inputHandler.Fire2Canceled += HandleFire2Cancel;
        inputHandler.ReloadPerformed += HandleReload;
        inputHandler.StorePerformed += HandleStore;
        inputHandler.UtilityPerformed += HandleUtilityStart; inputHandler.UtilityCanceled += HandleUtilityCancel;
        inputHandler.ToggleInventoryPerformed += HandleToggleInventory; inputHandler.ToggleMenuPerformed += HandleToggleMenu;
        // Rotation Events
        inputHandler.RotateHeldStarted += HandleRotateHeldStarted;
        inputHandler.RotateHeldEnded += HandleRotateHeldEnded;
        inputHandler.RotateDeltaPerformed += HandleRotateDeltaPerformed;

        // UI Actions Subscriptions
        inputHandler.UICancelPerformed += HandleUICancel;
        inputHandler.UITabNavigatePerformed += HandleUITabNavigate;
    }

    private void OnDisable()
    {
        if (inputHandler != null) {
             inputHandler.MovePerformed -= HandleMoveInput;
             inputHandler.LookPerformed -= HandleLookInput;
             inputHandler.JumpPerformed -= HandleJumpInput;
             inputHandler.SprintStarted -= HandleSprintStart; inputHandler.SprintCanceled -= HandleSprintCancel;
             inputHandler.CrouchStarted -= HandleCrouchStart; inputHandler.CrouchCanceled -= HandleCrouchCancel;
             inputHandler.SlowWalkStarted -= HandleSlowWalkStart; inputHandler.SlowWalkCanceled -= HandleSlowWalkCancel;
             inputHandler.ToolbarScrollPerformed -= HandleToolbarScroll;
             inputHandler.ToolbarSlotSelected -= HandleToolbarSlotSelection;
             inputHandler.InteractPerformed -= HandleInteract;
             inputHandler.Fire1Started -= HandleFire1Start; inputHandler.Fire1Canceled -= HandleFire1Cancel;
             inputHandler.Fire2Started -= HandleFire2Start; inputHandler.Fire2Canceled -= HandleFire2Cancel;
             inputHandler.ReloadPerformed -= HandleReload;
             inputHandler.StorePerformed -= HandleStore;
             inputHandler.UtilityPerformed -= HandleUtilityStart; inputHandler.UtilityCanceled -= HandleUtilityCancel;
             inputHandler.ToggleInventoryPerformed -= HandleToggleInventory; inputHandler.ToggleMenuPerformed -= HandleToggleMenu;
             inputHandler.RotateHeldStarted -= HandleRotateHeldStarted;
             inputHandler.RotateHeldEnded -= HandleRotateHeldEnded;
             inputHandler.RotateDeltaPerformed -= HandleRotateDeltaPerformed;
             inputHandler.UICancelPerformed -= HandleUICancel;
             inputHandler.UITabNavigatePerformed -= HandleUITabNavigate;
        }
        if (uiStateController != null) { uiStateController.OnStateChanged -= HandleUIStateChange; }
        if (grabController != null) { grabController.OnGrabStateChanged -= HandleGrabStateChanged; }
    }

    private void Update() {
        if (characterController != null && playerLook != null) {
            characterController.SetLookDirection(playerLook.GetLookDirection());
        }

        // Forward hold actions only if not grabbing AND not actively rotating a grabbed item
        if (inputHandler != null && equipmentController != null && !isRotatingGrabbedItem && (grabController == null || !grabController.IsGrabbing)) {
            if (inputHandler.IsFire1Held) equipmentController.HandleFire1Hold();
            if (inputHandler.IsFire2Held) equipmentController.HandleFire2Hold();
        }
    }

    private void LateUpdate() {
         if (characterController != null && playerLook != null && playerLook.transform != null) {
             Vector3 targetPos = characterController.GetSmoothedHeadWorldPosition();
             playerLook.transform.position = Vector3.Lerp(playerLook.transform.position, targetPos, Time.deltaTime * 20f);
         }
    }

    // --- Event Handlers ---
    private void HandleUIStateChange(UIStateChanged eventArgs) {
        if (inputHandler == null) return;
        if (eventArgs.Current == UIState.Gameplay) inputHandler.EnableGameplayControls();
        else inputHandler.EnableUIControls();
     }
    private void HandleGrabStateChanged(bool isGrabbing, IGrabbable grabbedItem) {
        if (isGrabbing) equipmentController?.HandleEquipRequest(null); // Force equip Hands
        else equipmentController?.HandleEquipRequest(playerInventory?.GetCurrentEquippedItem()); // Re-evaluate
    }

    // --- Gameplay Action Handlers ---
    private void HandleMoveInput(Vector2 m) => characterController?.SetMoveInput(m);
    private void HandleLookInput(Vector2 l) {
        if (!isRotatingGrabbedItem) playerLook?.SetLookInput(l);
        // RotateDeltaPerformed event now handles forwarding look delta for item rotation
    }
    private void HandleJumpInput() => characterController?.OnJumpPressed();
    private void HandleInteract() => worldInteractor?.OnInteractPressed();
    private void HandleSprintStart() => characterController?.SetSprint(true);
    private void HandleSprintCancel() => characterController?.SetSprint(false);
    private void HandleCrouchStart() => characterController?.SetCrouch(true);
    private void HandleCrouchCancel() => characterController?.SetCrouch(false);
    private void HandleSlowWalkStart() => characterController?.SetSlowWalk(true);
    private void HandleSlowWalkCancel() => characterController?.SetSlowWalk(false);

    private void HandleToolbarScroll(float dir)
    {
        // If player is actively rotating a grabbed item with middle mouse, scroll should NOT adjust distance.
        // Scroll should only adjust distance if IsGrabbing is true AND IsRotatingGrabbedItem is false.
        if (grabController != null && grabController.IsGrabbing && !isRotatingGrabbedItem) // Check !isRotatingGrabbedItem
            grabController.AdjustGrabbedItemDistance(dir);
        else if (!isRotatingGrabbedItem) // Only scroll toolbar if not actively rotating grabbed item
            playerInventory?.HandleToolbarScroll(dir);
    }

    private void HandleToolbarSlotSelection(int idx) => playerInventory?.HandleToolbarSlotSelection(idx);

    private void HandleFire1Start() {
    if (grabController != null && grabController.IsGrabbing) {
            grabController.DropGrabbedItemWithLMB(); // LMB drops if holding
        } else if (grabController != null) {
            grabController.TryGrabOrDetachWorldObject(); // LMB grabs if not holding
        }
        else {
            equipmentController?.HandleFire1Down(); // Fallback if no grabController
        }
    }

    private void HandleFire1Cancel() {
        // Only forward if not grabbing AND not rotating (as Fire1 is LMB and might be involved in future drag actions)
        if (!isRotatingGrabbedItem && (grabController == null || !grabController.IsGrabbing))
             equipmentController?.HandleFire1Up();
    }
    private void HandleFire2Start() {
        // If not grabbing, Fire2 (RMB) is for equipment (e.g., ADS)
        // If grabbing, RMB could do something else, or nothing. For now, let's say it does nothing if grabbing.
        if (grabController == null || !grabController.IsGrabbing) {
            equipmentController?.HandleFire2Down();
        }
        // else: RMB does nothing while actively grabbing/holding for now
    }
    private void HandleFire2Cancel() {
         if (grabController == null || !grabController.IsGrabbing)
            equipmentController?.HandleFire2Up();
    }
    private void HandleReload() {
         if (grabController == null || !grabController.IsGrabbing) // Only allow reload if not grabbing
            equipmentController?.HandleReloadDown();
    }
    private void HandleStore() {
        // This action is now handled centrally by PlayerGrabController
        grabController?.HandleStoreAction();
    }
    private void HandleUtilityStart() {
        // Example: Utility key ('E' or 'F')
        // If grabbing, could be used for a specific action on the grabbed item, or ignored.
        // For now, let's assume it's forwarded to equipment if not grabbing.
        if (grabController == null || !grabController.IsGrabbing) {
            equipmentController?.HandleUtilityDown();
        }
        // If grabbing, you might add: else { grabController.PerformUtilityOnGrabbed(); }
    }
    private void HandleUtilityCancel() {
         if (grabController == null || !grabController.IsGrabbing)
             equipmentController?.HandleUtilityUp();
    }

    // --- Grab Rotation Handlers ---
    private void HandleRotateHeldStarted() {
        if (grabController != null && grabController.IsGrabbing) {
            isRotatingGrabbedItem = true;
            //grabController.StartGrabRotation();
            // Debug.Log("PlayerManager: Rotate Held Started - isRotatingGrabbedItem = true");
        }
    }
    private void HandleRotateHeldEnded() {
        isRotatingGrabbedItem = false; // Always set this to false on release
        //grabController?.EndGrabRotation();
        // Debug.Log("PlayerManager: Rotate Held Ended - isRotatingGrabbedItem = false");
    }
    private void HandleRotateDeltaPerformed(Vector2 delta) {
        // This event is fired by PlayerInputHandler if IsRotateHeld is true during a LookPerformed event.
        if (isRotatingGrabbedItem && grabController != null) {
            // Debug.Log("PlayerManager: Forwarding Rotate Delta: " + delta);
            //grabController.ApplyGrabbedItemRotationInput(delta);
        }
    }

    // --- UI Toggle/Cancel Handlers ---
    private void HandleToggleInventory() { uiStateController?.ToggleState(UIState.Inventory); }
    private void HandleToggleMenu() { uiStateController?.ToggleState(UIState.Menu); }
    private void HandleUICancel() {
        if (uiStateController?.IsUIOpen ?? false)
            uiStateController.SetState(UIState.Gameplay);
     }
    private void HandleUITabNavigate() {
         if (uiStateController == null) return;
         if (uiStateController.Current == UIState.Inventory) uiStateController.SetState(UIState.Gameplay);
         else if (uiStateController.Current == UIState.Menu) Debug.Log("Tab in Menu - Navigation NYI");
    }

    public void ForceSlowWalk(bool v) => characterController?.ForceSlowWalk(v);
}
// --- End of script: Assets/Logic/Player/PlayerManager.cs ---