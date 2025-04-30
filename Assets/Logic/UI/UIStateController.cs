using UnityEngine;
using System;

// Removed namespace

public enum UIState { Gameplay, Inventory, Menu }

public struct UIStateChanged
{
    public UIState Previous;
    public UIState Current;
    public UIStateChanged(UIState previous, UIState current) { Previous = previous; Current = current; }
}

/// <summary>
/// Manages the overall UI state enum (Gameplay, Inventory, Menu).
/// Fires events on change and informs the CursorController about UI open status.
/// Does NOT directly control cursor lock/visibility.
/// </summary>
public class UIStateController : MonoBehaviour
{
    [SerializeField] private UIState startState = UIState.Gameplay;
    public UIState Current { get; private set; }
    public event Action<UIStateChanged> OnStateChanged;

    // --- Cached State ---
    // Keep track of whether we last considered the UI "open" to only notify CursorController on change
    private bool _wasUIOpen = false;

    private void Start()
    {
        Current = startState;
        // Initialize the open state and notify CursorController immediately
        _wasUIOpen = IsUIOpen; // Calculate initial state
        CursorController.Instance.SetUIOpen(_wasUIOpen); // Inform controller
    }

    public void ToggleState(UIState targetState)
    {
        if (targetState == UIState.Gameplay) { if (Current != UIState.Gameplay) SetState(UIState.Gameplay); }
        else { SetState(Current == targetState ? UIState.Gameplay : targetState); }
    }

    public void SetState(UIState newState, bool force = false)
    {
        if (!force && newState == Current) return;

        UIState previousState = Current;
        Current = newState;

        // Fire state changed event for listeners like UIPanelRegistry, PlayerManager
        var eventArgs = new UIStateChanged(previousState, Current);
        OnStateChanged?.Invoke(eventArgs);

        // --- Notify Cursor Controller IF Open Status Changed ---
        bool isNowOpen = IsUIOpen; // Check the new open status
        if (isNowOpen != _wasUIOpen) // Did the open status change?
        {
            CursorController.Instance.SetUIOpen(isNowOpen); // Inform cursor controller
            _wasUIOpen = isNowOpen; // Update cached state
        }
        // --- End Notification ---

        // Debug.Log($"[UIStateController] State set to: {Current}");
    }

    /// <summary>
    /// Returns true if any UI panel (non-Gameplay state) is currently active.
    /// </summary>
    public bool IsUIOpen => Current != UIState.Gameplay;

    // REMOVED: All cursor locking/visibility logic (_isCursorTemporarilyUnlockedByDrag, UpdateCursorState, HandleDragStateChanged)
    // REMOVED: OnEnable/OnDisable related to SlotView.DraggingChanged
}