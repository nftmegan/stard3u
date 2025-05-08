// In Assets/Scripts/Parts/RuntimeStates/IPartRuntimeState.cs
public interface IPartRuntimeState : IRuntimeState { // Your existing IRuntimeState
    float CurrentDurability { get; set; }
    float CurrentWear { get; set; } // Normalized 0 (new) to 1 (fully worn)
}