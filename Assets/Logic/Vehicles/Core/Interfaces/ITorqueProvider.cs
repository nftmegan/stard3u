public interface ITorqueProvider {
    /// <summary>
    /// Gets the amount of torque this provider is currently making available at its output.
    /// This is the torque *before* considering the immediate load from the connected receiver,
    /// but it should be calculated based on the provider's own internal state (e.g., engine RPM, throttle).
    /// </summary>
    /// <param name="outputRpmAtSource">The RPM at which this torque is being provided by the source.</param>
    /// <returns>The available torque in Newton-meters (Nm).</returns>
    float GetAvailableTorque(out float outputRpmAtSource);
}