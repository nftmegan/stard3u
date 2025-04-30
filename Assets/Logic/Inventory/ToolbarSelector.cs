using UnityEngine;
using System;

/// <summary>
/// Thin helper that keeps a “current index” inside a fixed slot range.
/// No inventory references; no UnityEvents.  It simply fires <see cref="OnIndexChanged"/>
/// so the UI and gameplay can respond.
/// </summary>
public class ToolbarSelector : MonoBehaviour
{
    [SerializeField] private int slotCount = 9;

    public int SlotCount => slotCount;

    public int CurrentIndex { get; private set; } = 0;

    public event Action<int> OnIndexChanged;    // new index

    public void SetIndex(int idx, bool force = false)
    {
        idx = Mathf.Clamp(idx, 0, slotCount - 1);
        if (!force && idx == CurrentIndex) return;

        CurrentIndex = idx;
        OnIndexChanged?.Invoke(CurrentIndex);
    }

    public void Step(int delta) =>
        SetIndex((CurrentIndex + delta + slotCount) % slotCount);
}