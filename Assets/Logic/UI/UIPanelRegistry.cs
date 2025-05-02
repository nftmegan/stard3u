using UnityEngine;
using System;
using System.Collections.Generic;

// Removed namespace

public class UIPanelRegistry : MonoBehaviour
{
    [Serializable]
    private struct PanelMapping
    {
        [Tooltip("The UI state that activates these panels.")]
        public UIState state;
        [Tooltip("The UI panels (GameObjects) to activate for this state.")]
        public GameObject[] panels;
    }

    [Tooltip("Define which panels are active for each UI state.")]
    [SerializeField] private PanelMapping[] panelMappings;

    private readonly Dictionary<UIState, GameObject[]> _panelLookup = new();
    private UIStateController _hookedController; // Store reference for unhooking and initial state

    private void Awake()
    {
        // Build the lookup dictionary
        _panelLookup.Clear(); // Ensure dictionary is clear if Awake runs again
        foreach (var mapping in panelMappings)
        {
            if (_panelLookup.ContainsKey(mapping.state))
            {
                Debug.LogWarning($"Duplicate UIState mapping found for '{mapping.state}' in UIPanelRegistry. Overwriting.", this);
            }
            _panelLookup[mapping.state] = mapping.panels ?? Array.Empty<GameObject>();
        }

        // Deactivate all managed panels initially to ensure a clean state
        // This prevents panels left active in the editor from showing incorrectly at start
        DeactivateAllManagedPanels();
    }

    /// <summary>
    /// Subscribes this registry to the UIStateController's state change events
    /// AND applies the controller's current state immediately.
    /// </summary>
    public void Hook(UIStateController controller)
    {
        if (controller != null)
        {
            // Unhook previous if necessary
            Unhook(_hookedController);

            _hookedController = controller; // Store reference
            _hookedController.OnStateChanged += HandleStateChange; // Subscribe to future changes

            // --- APPLY INITIAL STATE ---
            // Get the state the controller is *currently* in when Hook is called
            UIState initialState = _hookedController.Current;
            //Debug.Log($"[UIPanelRegistry HOOK] Hooked. Controller's current state is: {initialState}. Applying this state now.");

            // Deactivate everything first (optional but ensures clean state if panels were active in editor)
            // DeactivateAllManagedPanels(); // Can sometimes cause flicker, might not be needed if Awake handles it.

            // Directly try to activate panels for the initial state found
            if (_panelLookup.TryGetValue(initialState, out var panelsToActivate))
            {
                // Debug.Log($"[UIPanelRegistry HOOK] Found panels for initial state {initialState}. Activating...");
                SetPanelActiveState(panelsToActivate, true); // Activate the panels for the current state
            }
            else
            {
                 Debug.LogWarning($"[UIPanelRegistry HOOK] No panel mapping found for initial state: {initialState}");
            }
            // --- END APPLY INITIAL STATE ---
        }
        else
        {
            Debug.LogError("UIPanelRegistry cannot Hook to a null UIStateController!", this);
        }
    }

    /// <summary>
    /// Unsubscribes from the UIStateController's events.
    /// </summary>
    public void Unhook(UIStateController controller)
    {
         // Check if the controller passed is the one we are hooked to OR just unsubscribe from stored ref
         if (_hookedController != null)
         {
            _hookedController.OnStateChanged -= HandleStateChange;
            if (controller == _hookedController) // Clear stored ref only if unhooking the correct one
            {
                 _hookedController = null;
            }
         }
    }

    // Called when the hooked UIStateController's state changes
    private void HandleStateChange(UIStateChanged eventArgs)
    {
        // Debug.Log($"[UIPanelRegistry] HandleStateChange received. Previous: {eventArgs.Previous}, Current: {eventArgs.Current}");

        // Deactivate panels associated with the previous state (if different from current)
        if (eventArgs.Previous != eventArgs.Current && _panelLookup.TryGetValue(eventArgs.Previous, out var panelsToDeactivate))
        {
            // Debug.Log($"[UIPanelRegistry] Deactivating panels for state: {eventArgs.Previous}");
            SetPanelActiveState(panelsToDeactivate, false);
        }

        // Activate panels associated with the new (current) state
        if (_panelLookup.TryGetValue(eventArgs.Current, out var panelsToActivate))
        {
            // Debug.Log($"[UIPanelRegistry] Activating panels for state: {eventArgs.Current}. Panel count: {panelsToActivate?.Length ?? 0}");
             SetPanelActiveState(panelsToActivate, true);
            // if (panelsToActivate != null) foreach(var p in panelsToActivate) if (p!=null) Debug.Log($" - Activating: {p.name}");
        }
        // else Debug.LogWarning($"[UIPanelRegistry] No panel mapping found for state: {eventArgs.Current}");
    }

    // Helper to activate/deactivate panels safely
    private void SetPanelActiveState(GameObject[] panels, bool isActive)
    {
         if (panels == null) return;
         foreach (var panel in panels)
         {
             if (panel != null) panel.SetActive(isActive);
             // else Debug.LogWarning("A null panel reference was found in UIPanelRegistry mapping.", this);
         }
    }

     // Deactivate all panels managed by this registry
    private void DeactivateAllManagedPanels()
    {
        // Added null check for safety during initial Awake runs
        if (_panelLookup == null) return;

        foreach (var kvp in _panelLookup)
        {
            SetPanelActiveState(kvp.Value, false);
        }
    }

    // Ensure cleanup on destroy
    private void OnDestroy() {
        Unhook(_hookedController);
    }
}