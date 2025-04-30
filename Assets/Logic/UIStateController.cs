using UnityEngine;
using UnityEngine.InputSystem;     // swap to UnityEngine if you use the legacy system
using System;
using UI.Inventory;

public enum UIState { Gameplay, Inventory, Menu }

public struct UIStateChanged
{
    public UIState Previous;
    public UIState Current;

    public UIStateChanged(UIState previous, UIState current)
    {
        Previous = previous;
        Current  = current;
    }
}

public class UIStateController : MonoBehaviour
{
    [SerializeField] private UIState startState = UIState.Gameplay;

    public UIState Current { get; private set; }

    public event Action<UIStateChanged> OnStateChanged;

    private void Start() => SetState(startState, true);

    private void Update()
    {
        if (Keyboard.current.tabKey.wasPressedThisFrame)   Toggle(UIState.Inventory);
        if (Keyboard.current.escapeKey.wasPressedThisFrame)Toggle(UIState.Menu);
    }

    public void Toggle(UIState target)
        => SetState(Current == target ? UIState.Gameplay : target);

    public void SetState(UIState newState, bool force = false)
    {
        if (!force && newState == Current) return;

        var ev = new UIStateChanged { Previous = Current, Current = newState };
        Current = newState;

        OnStateChanged?.Invoke(ev);
        UpdateCursor();
    }

    /* ---------------- helpers ---------------- */
    public bool IsUIOpen   => Current != UIState.Gameplay;
    public bool IsMenuOpen => Current == UIState.Menu;

    private void UpdateCursor()
    {
        bool unlock = IsUIOpen || _dragUnlocked;
        Cursor.lockState = unlock ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible   = unlock;
    }

    /* ---------- optional drag unlock ---------- */
    private bool _dragUnlocked;

    private void OnEnable()  => SlotView.DraggingChanged += HandleDrag;
    private void OnDisable() => SlotView.DraggingChanged -= HandleDrag;

    private void HandleDrag(bool started)
    {
        _dragUnlocked = started;
        UpdateCursor();
    }
}
