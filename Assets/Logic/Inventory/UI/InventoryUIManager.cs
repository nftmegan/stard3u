using UnityEngine;
using System; // Keep for Action

// Removed namespace

public class InventoryUIManager : MonoBehaviour
{
    [Header("UI Element Parents")]
    [SerializeField] private Transform toolbarParent;
    [SerializeField] private Transform bagParent;

    [Header("Visuals")]
    [SerializeField] private Color selectedColor = Color.yellow;
    [SerializeField] private Color normalColor = Color.white;

    [Header("Prefabs")]
    [SerializeField] private SlotView slotPrefab;

    [Header("Sub-Panels")]
    [SerializeField] private ItemDetailUI detailPanel;

    private SlotView[] _toolbarViews = Array.Empty<SlotView>(); // Initialize to empty array
    private SlotView[] _bagViews = Array.Empty<SlotView>();     // Initialize to empty array
    private IInventoryViewDataSource _currentDataSource;
    private ToolbarSelector _linkedToolbarSelector; // Store reference for unsubscribing

    // Show the UI for a specific data source
    public void Show(IInventoryViewDataSource dataSource)
    {
        if (dataSource == null)
        {
            Debug.LogError("[InventoryUIManager] Cannot show Inventory UI with a null data source!");
            Hide();
            return;
        }

        _currentDataSource = dataSource;
        InitializeSlots(); // Setup slot views first

        // Subscribe to data source for content changes
        if (_currentDataSource != null)
        {
             // Debug.Log("[InventoryUIManager] Subscribing to DataSource.OnSlotChanged");
            _currentDataSource.OnSlotChanged += HandleSlotChanged;
        }

        // --- Subscribe to Toolbar for Highlight Changes ---
        _linkedToolbarSelector = null; // Reset previous link
        if (_currentDataSource is PlayerInventory playerInv && playerInv.Toolbar != null)
        {
            _linkedToolbarSelector = playerInv.Toolbar; // Store the reference
            // Debug.Log("[InventoryUIManager] Subscribing to ToolbarSelector.OnIndexChanged for highlighting.");
            _linkedToolbarSelector.OnIndexChanged += UpdateHighlight; // Subscribe UI method
        }
        else Debug.LogWarning("[InventoryUIManager] Cannot subscribe to ToolbarSelector: DataSource is not PlayerInventory or Toolbar is null.");
        // --- End Toolbar Subscription ---

        RedrawAll(); // Initial draw including highlight
        gameObject.SetActive(true);
        detailPanel?.Hide();
    }

    // Hide the UI and unsubscribe
    public void Hide()
    {
        gameObject.SetActive(false);

        // Unsubscribe from data source
        if (_currentDataSource != null)
        {
            // Debug.Log("[InventoryUIManager] Unsubscribing from DataSource.OnSlotChanged");
            _currentDataSource.OnSlotChanged -= HandleSlotChanged;
        }

        // Unsubscribe from Toolbar
        if (_linkedToolbarSelector != null)
        {
             // Debug.Log("[InventoryUIManager] Unsubscribing from ToolbarSelector.OnIndexChanged.");
             _linkedToolbarSelector.OnIndexChanged -= UpdateHighlight;
        }

        _currentDataSource = null;
        _linkedToolbarSelector = null; // Clear reference
    }

    // Creates/finds SlotView instances under parents
    private void InitializeSlots()
    {
        _toolbarViews = toolbarParent?.GetComponentsInChildren<SlotView>(true) ?? Array.Empty<SlotView>();
        _bagViews = bagParent?.GetComponentsInChildren<SlotView>(true) ?? Array.Empty<SlotView>();

        int totalUISlots = _toolbarViews.Length + _bagViews.Length;
        if (_currentDataSource != null && _currentDataSource.SlotCount != totalUISlots && totalUISlots > 0)
        {
            Debug.LogWarning($"Inventory UI has {totalUISlots} slots, but data source has {_currentDataSource.SlotCount}. UI may not show all items.");
        }

        int idx = 0;
        foreach (var v in _toolbarViews) if(v != null) v.Setup(idx++, this);
        foreach (var v in _bagViews) if(v != null) v.Setup(idx++, this);
    }

    // --- UI Interaction Requests ---
    public void RequestMergeOrSwap(int fromSlotIndex, int toSlotIndex) =>
        _currentDataSource?.RequestMergeOrSwap(fromSlotIndex, toSlotIndex);

    public void RequestInspect(int slotIndex)
    {
        if (_currentDataSource == null || detailPanel == null) return;
        var slot = _currentDataSource.GetSlotByIndex(slotIndex);
        if (slot == null || slot.IsEmpty()) detailPanel.Hide();
        else detailPanel.ShowFor(slot.item, slot.quantity, slotIndex);
    }

    // --- Drawing Logic ---
    private void RedrawAll()
    {
        if (_currentDataSource == null) return;

        // Draw content
        int uiSlotIndex = 0;
        foreach (var view in _toolbarViews) if (view != null) Draw(view, _currentDataSource.GetSlotByIndex(uiSlotIndex++));
        foreach (var view in _bagViews) if (view != null) Draw(view, _currentDataSource.GetSlotByIndex(uiSlotIndex++));

        // Set initial highlight
        UpdateHighlight(_linkedToolbarSelector?.CurrentIndex ?? 0); // Use stored selector's index or default
    }

    // Called by data source when slot content changes
    private void HandleSlotChanged(int index)
    {
        // Debug.Log($"[InventoryUIManager] HandleSlotChanged received index: {index}");
        if (_currentDataSource == null) return;

        if (index == -1) // Structural change
        {
            InitializeSlots(); // Re-find/setup views
            RedrawAll();      // Redraw everything including highlight
        }
        else if (index >= 0) // Single slot change
        {
            UpdateSingleSlot(index); // Update only the affected slot's content
        }
    }

    // Updates the icon/quantity of a single slot view
    private void UpdateSingleSlot(int index)
    {
        SlotView view = GetViewForIndex(index);
        if (view != null)
        {
            // Debug.Log($"[InventoryUIManager] Updating view for Slot Index: {index}");
            Draw(view, _currentDataSource.GetSlotByIndex(index));
        }
        // else Debug.LogWarning($"[InventoryUIManager] No view found for Slot Index: {index} in UpdateSingleSlot");
    }

    // Sets the visual state of a SlotView based on InventorySlot data
    private void Draw(SlotView view, InventorySlot slot)
    {
        if (view == null) return;
        if (slot == null || slot.IsEmpty()) view.DrawEmpty();
        else view.DrawStack(slot.item.data.sprite, slot.quantity);
    }

    // --- Highlight Logic ---
    // Called by ToolbarSelector when the selected index changes
    private void UpdateHighlight(int newlySelectedIndex)
    {
        // Debug.Log($"[InventoryUIManager] Updating Highlight for Toolbar Index: {newlySelectedIndex}");
        if (_toolbarViews == null) return;

        for (int i = 0; i < _toolbarViews.Length; i++)
        {
            if (_toolbarViews[i] != null) // Check view exists
            {
                _toolbarViews[i].SetSelected(i == newlySelectedIndex, selectedColor, normalColor);
            }
        }
    }

    // Helper to find the SlotView associated with a container index
    private SlotView GetViewForIndex(int index)
    {
        if (index < 0) return null;
        if (index < _toolbarViews.Length) return _toolbarViews[index]; // Check toolbar first
        int bagIndex = index - _toolbarViews.Length;                   // Adjust index for bag
        if (bagIndex < _bagViews.Length) return _bagViews[bagIndex];   // Check bag
        return null; // Index out of range for both toolbar and bag views
    }
}