using UnityEngine;
using UI.Inventory;          // InventoryUIManager + SlotView
                              //  (SlotView raises drag events)
                              
public class PlayerManager : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private PlayerInventory    playerInventory;
    [SerializeField] private InventoryUIManager inventoryUIManager;

    [Header("New UI pipeline")]
    [SerializeField] private UIStateController  uiState;
    [SerializeField] private UIPanelRegistry    uiRegistry;

    [Header("Look / Movement")]
    [SerializeField] private PlayerLook look;   // drag the rig’s PlayerLook

    public PlayerLook Look => look;
    public MyCharacterController MyCharacterController { get; private set; }
    public HeadBob               HeadBob               { get; private set; }
    public IPlayerInput          InputProvider         { get; private set; }
    public EquipmentController   EquipmentController   { get; private set; }
    public WorldInteractor       WorldInteractor       { get; private set; }

    /* ─────────────────────────── Awake ─────────────────────────── */
    private void Awake()
    {
        MyCharacterController = GetComponentInChildren<MyCharacterController>();
        HeadBob               = GetComponentInChildren<HeadBob>();
        EquipmentController   = GetComponentInChildren<EquipmentController>();
        WorldInteractor       = GetComponentInChildren<WorldInteractor>();

        InputProvider   ??= GetComponent<IPlayerInput>();
        playerInventory ??= GetComponent<PlayerInventory>();
        uiState         ??= GetComponent<UIStateController>();
        if (uiRegistry  == null)  uiRegistry = FindFirstObjectByType<UIPanelRegistry>();

        if (look == null)          Debug.LogError("[PlayerManager] PlayerLook missing!");
        if (InputProvider == null) Debug.LogError("[PlayerManager] IPlayerInput missing!");
        if (uiState == null)       Debug.LogError("[PlayerManager] UIStateController missing!");
        if (uiRegistry == null)    Debug.LogError("[PlayerManager] UIPanelRegistry missing in scene!");

        /* hook the registry so panels follow state changes */
        uiRegistry?.Hook(uiState);
    }

    /* ─────────────────────────── Start ─────────────────────────── */
    private void Start()
    {
        if (!playerInventory) return;

        playerInventory.Initialize();
        inventoryUIManager?.Initialize(playerInventory);

        /* ensure game starts in locked-cursor gameplay mode */
        uiState?.SetState(UIState.Gameplay, true);
    }

    /* ─────────────────────────── Update ────────────────────────── */
    private void Update()
    {
        if (!IsValid()) return;

        /* free-look only when no UI is open */
        if (!uiState.IsUIOpen)
            look.HandleInput(InputProvider);

        playerInventory?.HandleInput(InputProvider);

        Vector3 lookDir = look.Orientation * Vector3.forward;
        MyCharacterController.HandleInput(InputProvider, lookDir);

        if (uiState.IsUIOpen) return;   // stop here if UI visible

        EquipmentController?.HandleInput(InputProvider);
        WorldInteractor    ?.HandleInput(InputProvider);
    }

    private void LateUpdate()
    {
        if (MyCharacterController != null && look != null)
        {
            Vector3 targetPos = MyCharacterController.GetSmoothedHeadWorldPosition();
            look.transform.position = Vector3.Lerp(
                    look.transform.position, targetPos, Time.deltaTime * 20f);
        }
    }

    private bool IsValid() =>
        look != null &&
        MyCharacterController != null &&
        InputProvider != null &&
        uiState != null;

    /* ─────────── public helpers ─────────── */
    public PlayerInventory GetInventory()         => playerInventory;
    public WorldInteractor GetWorldInteractor()   => WorldInteractor;
    public void SetSlowWalk(bool v)               => MyCharacterController?.ForceSlowWalk(v);
}