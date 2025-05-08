public interface IRotationReceiver {
    // Informs the receiver about the RPM of the source it's connected to.
    void SetInputRPM(float inputRpm);
}