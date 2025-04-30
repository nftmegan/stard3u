// Assets/YourProjectName/Scripts/Inventory/UI/ToolbarSelector.cs
using UnityEngine;
using System;

/// <summary>
/// Manages the selected index for a toolbar UI representation.
/// Fires OnIndexChanged when the index changes.
/// Does NOT directly interact with inventory logic anymore.
/// </summary>
public class ToolbarSelector : MonoBehaviour
{
    // Configurable in Inspector - how many slots does this UI represent?
    [SerializeField] private int slotCount = 9;
    public int SlotCount => slotCount;

    public int CurrentIndex { get; private set; } = 0;

    // Event for UI and PlayerInventory to listen to
    public event Action<int> OnIndexChanged; // new index (0 to SlotCount-1)

    /// <summary>
    /// Sets the current index, clamping it and invoking the event if changed.
    /// </summary>
    /// <param name="idx">The desired index.</param>
    /// <param name="force">Force invoke event even if index is the same.</param>
    public void SetIndex(int idx, bool force = false)
    {
        idx = Mathf.Clamp(idx, 0, slotCount - 1);
        if (!force && idx == CurrentIndex) return;

        CurrentIndex = idx;
        OnIndexChanged?.Invoke(CurrentIndex);
    }

    /// <summary>
    /// Steps the index by a delta, wrapping around.
    /// </summary>
    /// <param name="delta">+1 for next, -1 for previous.</param>
    public void Step(int delta)
    {
        if (slotCount <= 0) return;
        // Correct modulo for negative numbers
        int newIndex = (CurrentIndex + delta % slotCount + slotCount) % slotCount;
        SetIndex(newIndex);
    }
}