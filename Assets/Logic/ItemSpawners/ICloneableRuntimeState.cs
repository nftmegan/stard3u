public interface ICloneableRuntimeState : IRuntimeState {
    IRuntimeState Clone(); // Returns a deep copy of itself
}