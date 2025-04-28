using UnityEngine;
using System;

public enum PlayerUIState
{
    Gameplay,
    Inventory,
    Menu
}

public class PlayerUIController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject inventoryUI;
    [SerializeField] private GameObject menuUI;

    private PlayerUIState currentState = PlayerUIState.Gameplay;
    private bool isDraggingItem = false;

    public event Action OnInventoryOpened;
    public event Action OnInventoryClosed;
    public event Action OnMenuOpened;
    public event Action OnMenuClosed;

    private void Start()
    {
        SetState(PlayerUIState.Gameplay, true);
    }

    private void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            Debug.Log("[PlayerUIController] TAB pressed → toggling inventory.");
            ToggleInventory();
        }
        else if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log("[PlayerUIController] ESC pressed → toggling menu.");
            ToggleMenu();
        }
    }

    private void ToggleInventory()
    {
        if (currentState == PlayerUIState.Inventory)
            SetState(PlayerUIState.Gameplay);
        else
            SetState(PlayerUIState.Inventory);
    }

    private void ToggleMenu()
    {
        if (currentState == PlayerUIState.Menu)
            SetState(PlayerUIState.Gameplay);
        else
            SetState(PlayerUIState.Menu);
    }

    public void SetState(PlayerUIState newState, bool force = false)
    {
        if (!force && currentState == newState)
        {
            Debug.Log($"[PlayerUIController] Already in state {newState}, skipping SetState.");
            return;
        }

        PlayerUIState previousState = currentState;
        currentState = newState;

        Debug.Log($"[PlayerUIController] State changed: {previousState} → {currentState}");

        UpdateUIVisibility();
        UpdateCursorLock();
        FireEvents(previousState, currentState);
    }

    private void FireEvents(PlayerUIState previous, PlayerUIState current)
    {
        if (previous == PlayerUIState.Inventory && current != PlayerUIState.Inventory)
            OnInventoryClosed?.Invoke();
        if (previous == PlayerUIState.Menu && current != PlayerUIState.Menu)
            OnMenuClosed?.Invoke();

        if (current == PlayerUIState.Inventory)
            OnInventoryOpened?.Invoke();
        if (current == PlayerUIState.Menu)
            OnMenuOpened?.Invoke();
    }

    private void UpdateUIVisibility()
    {
        if (inventoryUI != null)
        {
            bool active = (currentState == PlayerUIState.Inventory);
            inventoryUI.SetActive(active);
            Debug.Log($"[PlayerUIController] Inventory UI {(active ? "opened" : "closed")}");
        }

        if (menuUI != null)
        {
            bool active = (currentState == PlayerUIState.Menu);
            menuUI.SetActive(active);
            Debug.Log($"[PlayerUIController] Menu UI {(active ? "opened" : "closed")}");
        }
    }

    private void UpdateCursorLock()
    {
        if (isDraggingItem)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Debug.Log("[PlayerUIController] Dragging item → Cursor unlocked.");
            return;
        }

        bool shouldUnlock = currentState != PlayerUIState.Gameplay;
        Cursor.lockState = shouldUnlock ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = shouldUnlock;

        Debug.Log($"[PlayerUIController] Cursor {(shouldUnlock ? "unlocked (UI open)" : "locked (gameplay)")}.");
    }

    // === Utility Methods ===
    public bool IsUIOpen()
    {
        return currentState != PlayerUIState.Gameplay;
    }

    public bool IsInventoryOpen()
    {
        return currentState == PlayerUIState.Inventory;
    }

    public bool IsMenuOpen()
    {
        return currentState == PlayerUIState.Menu;
    }

    public void CloseInventory()
    {
        if (currentState == PlayerUIState.Inventory)
        {
            Debug.Log("[PlayerUIController] CloseInventory() called from UI Button.");
            SetState(PlayerUIState.Gameplay);
        }
    }

    public void CloseMenu()
    {
        if (currentState == PlayerUIState.Menu)
        {
            Debug.Log("[PlayerUIController] CloseMenu() called from UI Button.");
            SetState(PlayerUIState.Gameplay);
        }
    }

    public void OpenInventory()
    {
        Debug.Log("[PlayerUIController] OpenInventory() called from UI Button.");
        SetState(PlayerUIState.Inventory);
    }

    public void OpenMenu()
    {
        Debug.Log("[PlayerUIController] OpenMenu() called from UI Button.");
        SetState(PlayerUIState.Menu);
    }
}
