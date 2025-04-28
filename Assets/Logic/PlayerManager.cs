using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private PlayerUIController uiController;
    [SerializeField] private InventoryUIManager inventoryUIManager; // ✅ Needed!
    [SerializeField] private Transform characterOrientation;

    public PlayerOrientation Orientation { get; private set; }
    public MyCharacterController MyCharacterController { get; private set; }
    public HeadBob HeadBob { get; private set; }
    public IPlayerInput InputProvider { get; private set; }
    public EquipmentController EquipmentController { get; private set; }
    public WorldInteractor WorldInteractor { get; private set; }

    private void Awake()
    {
        Orientation = GetComponent<PlayerOrientation>();
        MyCharacterController = GetComponentInChildren<MyCharacterController>();
        HeadBob = GetComponentInChildren<HeadBob>();
        EquipmentController = GetComponentInChildren<EquipmentController>();
        WorldInteractor = GetComponentInChildren<WorldInteractor>();

        InputProvider = GetComponent<IPlayerInput>();
        playerInventory ??= GetComponent<PlayerInventory>();
        uiController ??= GetComponent<PlayerUIController>();
        inventoryUIManager ??= FindFirstObjectByType<InventoryUIManager>(); // ✅ auto-find if missing

        if (InputProvider == null)
            Debug.LogError("[PlayerManager] No IPlayerInput found!");
    }

    private void Start()
    {
        if (playerInventory == null)
        {
            Debug.LogError("[PlayerManager] No PlayerInventory assigned or found.");
            return;
        }

        playerInventory.Initialize();
        
        if (inventoryUIManager != null)
        {
            inventoryUIManager.Initialize(playerInventory); // ✅ Properly initializing Inventory UI
            Debug.Log("[PlayerManager] InventoryUIManager initialized successfully!");
        }
        else
        {
            Debug.LogError("[PlayerManager] InventoryUIManager not found or assigned.");
        }

        uiController?.SetState(PlayerUIState.Gameplay);
    }

    private void Update()
    {
        if (!IsValid()) return;

        if (!uiController.IsUIOpen())
        {
            Orientation.ApplyLookInput(InputProvider.LookAxisX, InputProvider.LookAxisY);
        }

        characterOrientation.rotation = Quaternion.Euler(Orientation.Pitch, Orientation.Yaw, 0f);

        playerInventory?.HandleInput(InputProvider);

        Vector3 lookDir = Orientation.CurrentOrientation * Vector3.forward;
        MyCharacterController.HandleInput(InputProvider, lookDir);

        if (uiController.IsUIOpen())
            return;
        
        EquipmentController?.HandleInput(InputProvider);
        WorldInteractor?.HandleInput(InputProvider);
    }

    private void LateUpdate()
    {
        if (MyCharacterController != null && characterOrientation != null)
        {
            characterOrientation.position = MyCharacterController.GetSmoothedHeadWorldPosition();
        }
    }

    private bool IsValid()
    {
        return Orientation != null && characterOrientation != null &&
               MyCharacterController != null && InputProvider != null && uiController != null;
    }

    // === Public Accessors ===
    public PlayerInventory GetInventory() => playerInventory;
    public WorldInteractor GetWorldInteractor() => WorldInteractor;

    public void SetSlowWalk(bool value)
    {
        MyCharacterController?.ForceSlowWalk(value);
    }
}
