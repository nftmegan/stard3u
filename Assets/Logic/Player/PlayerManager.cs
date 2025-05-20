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

    public PlayerLook Look => playerLook;
    public MyCharacterController CharacterController => characterController;
    public EquipmentController Equipment => equipmentController;
    public WorldInteractor Interactor => worldInteractor;
    public PlayerInventory Inventory => playerInventory;
    public PlayerInputHandler InputHandler => inputHandler;
    public PlayerGrabController GrabController => grabController;

    private bool isRotatingGrabbedItem = false; // True if MMB is held AND an item is grabbed

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

        if (inputHandler == null) Debug.LogError("[PM] Input Handler Missing", this);
        if (playerInventory == null) Debug.LogError("[PM] PlayerInventory Missing", this);
        if (playerLook == null) Debug.LogError("[PM] PlayerLook Missing", this);
        if (characterController == null) Debug.LogError("[PM] CharacterController Missing", this);
        if (equipmentController == null) Debug.LogError("[PM] EquipmentController Missing", this);
        if (worldInteractor == null) Debug.LogError("[PM] WorldInteractor Missing", this);
        if (grabController == null) Debug.LogError("[PM] Grab Controller Missing.", this);
        if (uiStateController == null) Debug.LogError("[PM] UIStateController Missing", this);
        
        if (uiStateController != null) uiStateController.OnStateChanged += HandleUIStateChange;
        if (grabController != null)
        {
            grabController.InitializeController(this);
            grabController.OnGrabStateChanged += HandleGrabStateChanged;
        }
    }

    private void Start()
    {
        if (uiPanelRegistry != null && uiStateController != null) uiPanelRegistry.Hook(uiStateController);
        else if (uiStateController == null && inputHandler != null) inputHandler.EnableGameplayControls();
        else if (uiStateController != null) HandleUIStateChange(new UIStateChanged(uiStateController.Current, uiStateController.Current));

        if (inventoryUIManager != null && playerInventory != null) inventoryUIManager.Show(playerInventory);
        equipmentController?.ManualStart();
    }

    private void OnEnable()
    {
        if (inputHandler == null) { Debug.LogError("[PM] Input Handler Null on Enable.", this); return; }

        inputHandler.MovePerformed += HandleMoveInput;
        inputHandler.LookPerformed += HandleLookInput; // This event provides the delta
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
        
        inputHandler.RotateHeldStarted += HandleRotateHeldStartedEvent; // Renamed to avoid conflict
        inputHandler.RotateHeldEnded += HandleRotateHeldEndedEvent;     // Renamed to avoid conflict
        inputHandler.RotateDeltaPerformed += HandleRotateDeltaInput;    // This receives delta from LookPerformed when MMB is held
        
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

             inputHandler.RotateHeldStarted -= HandleRotateHeldStartedEvent;
             inputHandler.RotateHeldEnded -= HandleRotateHeldEndedEvent;
             inputHandler.RotateDeltaPerformed -= HandleRotateDeltaInput;

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

    private void HandleUIStateChange(UIStateChanged eventArgs) {
        if (inputHandler == null) return;
        if (eventArgs.Current == UIState.Gameplay) inputHandler.EnableGameplayControls();
        else inputHandler.EnableUIControls();
        if (eventArgs.Current != UIState.Gameplay && isRotatingGrabbedItem) {
            isRotatingGrabbedItem = false;
            grabController?.EndGrabRotation();
        }
     }
    private void HandleGrabStateChanged(bool isGrabbing, IGrabbable grabbedItem) {
        if (isGrabbing) {
            equipmentController?.HandleEquipRequest(null); 
        } else {
            equipmentController?.HandleEquipRequest(playerInventory?.GetCurrentEquippedItem()); 
            if (isRotatingGrabbedItem) {
                isRotatingGrabbedItem = false; 
            }
        }
    }

    private void HandleMoveInput(Vector2 m) => characterController?.SetMoveInput(m);

    private void HandleLookInput(Vector2 lookDelta) {
        // This lookDelta is from InputHandler.LookPerformed
        // PlayerInputHandler now gates the LookPerformed event based on IsRotateHeld for camera look
        // but always fires RotateDeltaPerformed if IsRotateHeld is true.
        // So, here we only pass to PlayerLook if not rotating item.
        if (!isRotatingGrabbedItem) {
            playerLook?.SetLookInput(lookDelta);
        }
    }
    private void HandleRotateDeltaInput(Vector2 rotateDelta) {
        // This is called from InputHandler.RotateDeltaPerformed (which is fired by LookPerformed if IsRotateHeld)
        if (isRotatingGrabbedItem && grabController != null && grabController.IsGrabbing) {
            grabController.ApplyGrabbedItemRotationInput(rotateDelta);
        }
    }

    private void HandleJumpInput() => characterController?.OnJumpPressed();
    private void HandleInteract() {
        if (isRotatingGrabbedItem) return; 
        worldInteractor?.OnInteractPressed();
    }
    private void HandleSprintStart() => characterController?.SetSprint(true);
    private void HandleSprintCancel() => characterController?.SetSprint(false);
    private void HandleCrouchStart() => characterController?.SetCrouch(true);
    private void HandleCrouchCancel() => characterController?.SetCrouch(false);
    private void HandleSlowWalkStart() => characterController?.SetSlowWalk(true);
    private void HandleSlowWalkCancel() => characterController?.SetSlowWalk(false);

    private void HandleToolbarScroll(float dir) {
        if (isRotatingGrabbedItem) return; 
        if (grabController != null && grabController.IsGrabbing)
            grabController.AdjustGrabbedItemDistance(dir);
        else
            playerInventory?.HandleToolbarScroll(dir);
    }

    private void HandleToolbarSlotSelection(int idx) {
        if (isRotatingGrabbedItem) return; 
        playerInventory?.HandleToolbarSlotSelection(idx);
    }

    private void HandleFire1Start() {
        equipmentController?.HandleFire1Down();
    }

    private void HandleFire1Cancel() {
        // Only forward if not grabbing AND not rotating (as Fire1 is LMB and might be involved in future drag actions)
        if (!isRotatingGrabbedItem && (grabController == null || !grabController.IsGrabbing))
             equipmentController?.HandleFire1Up();
    }
    
    private void HandleFire2Start() {
        if (isRotatingGrabbedItem) return;
        if (grabController == null || !grabController.IsGrabbing) {
            equipmentController?.HandleFire2Down();
        }
    }
    private void HandleFire2Cancel() {
         if (isRotatingGrabbedItem) return;
         if (grabController == null || !grabController.IsGrabbing)
            equipmentController?.HandleFire2Up();
    }
    private void HandleReload() {
         if (isRotatingGrabbedItem) return; 
         if (grabController == null || !grabController.IsGrabbing) 
            equipmentController?.HandleReloadDown();
    }
    
    private void HandleStore() {
        if (isRotatingGrabbedItem) return; 
        // The PlayerGrabController's HandleStoreAction now decides to store or pull
        grabController?.HandleStoreAction();
    }

    private void HandleUtilityStart() {
        if (isRotatingGrabbedItem) return;
        // If you want Utility to do something specific while grabbing (other than rotation, which uses MMB),
        // you could add logic here to call a method on grabController.
        // For now, it only forwards to equipment if not grabbing.
        if (grabController == null || !grabController.IsGrabbing) {
            equipmentController?.HandleUtilityDown();
        }
    }
    private void HandleUtilityCancel() {
         if (isRotatingGrabbedItem) return;
         if (grabController == null || !grabController.IsGrabbing)
             equipmentController?.HandleUtilityUp();
    }

    // Renamed to avoid conflict with internal state variable
    private void HandleRotateHeldStartedEvent() {
        if (grabController != null && grabController.IsGrabbing) {
            isRotatingGrabbedItem = true; // This is the PM's state flag
            grabController.StartGrabRotation(); // Tell PGC to enter its rotation mode
        }
    }
    private void HandleRotateHeldEndedEvent() {
        if(isRotatingGrabbedItem) { // Only if we were actually in rotation mode
            grabController?.EndGrabRotation(); // Tell PGC to exit its rotation mode
        }
        isRotatingGrabbedItem = false; // Always reset PM's state flag
    }
    
    private void HandleToggleInventory() { 
        if (isRotatingGrabbedItem) HandleRotateHeldEndedEvent(); // Cancel rotation if opening UI
        uiStateController?.ToggleState(UIState.Inventory); 
    }
    private void HandleToggleMenu() { 
        if (isRotatingGrabbedItem) HandleRotateHeldEndedEvent();
        uiStateController?.ToggleState(UIState.Menu); 
    }
    private void HandleUICancel() {
        if (isRotatingGrabbedItem) HandleRotateHeldEndedEvent();
        if (uiStateController?.IsUIOpen ?? false)
            uiStateController.SetState(UIState.Gameplay);
     }
    private void HandleUITabNavigate() {
         if (uiStateController == null) return;
         if (uiStateController.Current == UIState.Inventory) uiStateController.SetState(UIState.Gameplay);
    }

    public void ForceSlowWalk(bool v) => characterController?.ForceSlowWalk(v);
}
// --- End of script