using UnityEngine;

// Removed namespace

/// <summary>
/// Centralized manager for controlling the visibility and lock state of the mouse cursor.
/// Uses a Singleton pattern for easy access.
/// Other systems request state changes via public methods.
/// </summary>
public class CursorController : MonoBehaviour
{
    // --- Singleton Instance ---
    private static CursorController _instance;
    public static CursorController Instance
    {
        get
        {
            if (_instance == null)
            {
                // Use the new method
                _instance = FindFirstObjectByType<CursorController>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("CursorController");
                    _instance = go.AddComponent<CursorController>();
                    Debug.LogWarning("CursorController instance was not found, creating one automatically.");
                }
            }
            return _instance;
        }
    }

    // --- State Flags ---
    private bool _isUIOpen = false;     // Is any major UI (Inventory, Menu) currently considered open?
    private bool _isDragging = false;   // Is a UI element currently being dragged?

    // Keep track of the last applied state to avoid redundant calls
    private CursorLockMode _lastAppliedLockMode = CursorLockMode.None;
    private bool _lastAppliedVisibility = true;

    private void Awake()
    {
        // Singleton enforcement
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning("Duplicate CursorController detected. Destroying this instance.", gameObject);
            Destroy(gameObject);
            return;
        }
        _instance = this;

        // Optional: Persist across scenes if needed
        // DontDestroyOnLoad(gameObject);

        // Apply initial state based on default flags (usually gameplay)
        UpdateCursorState();
    }

    /// <summary>
    /// Updates the cursor lock/visibility based on the current state flags.
    /// This should be called whenever a relevant state flag changes.
    /// </summary>
    private void UpdateCursorState()
    {
        // Determine the desired state
        bool shouldUnlock = _isUIOpen || _isDragging; // Unlock if UI is open OR dragging is happening

        CursorLockMode targetLockMode = shouldUnlock ? CursorLockMode.None : CursorLockMode.Locked;
        bool targetVisibility = shouldUnlock;

        // Apply the state only if it has actually changed
        ApplyStateToUnityCursor(targetLockMode, targetVisibility);
    }

    /// <summary>
    /// Directly sets the Unity Cursor properties if they differ from the last applied state.
    /// </summary>
    private void ApplyStateToUnityCursor(CursorLockMode mode, bool visible)
    {
        if (Cursor.lockState != mode || Cursor.visible != visible) // Check if change is needed
        {
            // Debug.Log($"[CursorController] Applying State: LockMode={mode} (Was {_lastAppliedLockMode}), Visible={visible} (Was {_lastAppliedVisibility})");
            Cursor.lockState = mode;
            Cursor.visible = visible;

            _lastAppliedLockMode = mode;
            _lastAppliedVisibility = visible;
        }
        // else Debug.Log($"[CursorController] State Unchanged: LockMode={mode}, Visible={visible}");

    }


    // --- Public Methods for Other Systems to Call ---

    /// <summary>
    /// Call this when a major UI panel (Inventory, Menu, etc.) opens or closes.
    /// </summary>
    /// <param name="isOpen">True if the UI is now open, false if closed.</param>
    public void SetUIOpen(bool isOpen)
    {
        if (_isUIOpen != isOpen)
        {
            // Debug.Log($"[CursorController] SetUIOpen called: {isOpen}");
            _isUIOpen = isOpen;
            UpdateCursorState(); // Recalculate and apply cursor state
        }
    }

    /// <summary>
    /// Call this when a UI drag operation starts or stops.
    /// </summary>
    /// <param name="isDragging">True if dragging started, false if stopped.</param>
    public void SetDragging(bool isDragging)
    {
        if (_isDragging != isDragging)
        {
             // Debug.Log($"[CursorController] SetDragging called: {isDragging}");
            _isDragging = isDragging;
            UpdateCursorState(); // Recalculate and apply cursor state
        }
    }

     // Optional: Add a LateUpdate check as a safety net (usually not needed if Set methods are called correctly)
     // private void LateUpdate()
     // {
     //     UpdateCursorState(); // Ensures state is correct at end of frame
     // }
}