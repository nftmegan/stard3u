// In Assets/Scripts/Parts/RuntimeStates/FluidContainerRuntimeState.cs
[System.Serializable]
public class FluidContainerRuntimeState : IPartRuntimeState {
    public float CurrentDurability { get; set; }
    public float CurrentWear { get; set; }
    public float currentFluidLiters { get; set; }
    public string currentFluidID; // Stores itemCode of the FluidData SO if needed
    public float capacityLiters; // Often set from definition, but could be dynamic

    public FluidContainerRuntimeState() { /* Default constructor */ }
}