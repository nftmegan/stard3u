// In Assets/Scripts/Parts/RuntimeStates/WheelRuntimeState.cs
[System.Serializable]
public class WheelRuntimeState : IPartRuntimeState {
    public float CurrentDurability { get; set; } // For rim/bearing integrity
    public float CurrentWear { get; set; } // For tire tread
    public float tirePressurePsi { get; set; }
    public bool isPunctured { get; set; }

    public WheelRuntimeState() { /* Default constructor */ }
}