using System;

/// <summary>
/// Interface for providing inventory data to a UI manager.
/// </summary>
public interface IInventoryViewDataSource
{
    int SlotCount { get; }
    InventorySlot GetSlotByIndex(int index);
    event Action<int> OnSlotChanged; // int = index of changed slot, -1 for structural change

    // Callback for UI actions
    void RequestMergeOrSwap(int fromIndex, int toIndex);
    // Add other requests if needed (e.g., RequestDrop, RequestUse)
}