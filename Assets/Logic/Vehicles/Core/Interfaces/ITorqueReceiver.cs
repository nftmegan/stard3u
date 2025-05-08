// In Assets/Scripts/Core/Interfaces/ITorqueReceiver.cs
public interface ITorqueReceiver {
    /// <summary>
    /// Called by an upstream ITorqueProvider to deliver torque and its source RPM to this receiver.
    /// The receiver will use this information to update its own state or pass torque further downstream.
    /// </summary>
    /// <param name="torqueNm">The amount of torque being applied to this receiver's input (in Newton-meters).</param>
    /// <param name="sourceOutputRpm">The RPM of the ITorqueProvider's output shaft that is delivering this torque.</param>
    void ApplyReceivedTorque(float torqueNm, float sourceOutputRpm);

    /// <summary>
    /// Calculates and returns the total resistive load torque that this receiver
    /// (and any components connected downstream from it) imposes back onto the
    /// ITorqueProvider connected to its input. This includes internal friction,
    /// inertia effects, and loads from further downstream parts, all reflected
    /// to this receiver's input shaft.
    /// </summary>
    /// <returns>The imposed load torque in Newton-meters (Nm).</returns>
    float GetImposedLoadTorque();

    /// <summary>
    /// Gets the current rotational speed (RPM) of this receiver's input shaft.
    /// This RPM is typically influenced by the ITorqueProvider it's connected to
    /// (e.g., an engine forces a gearbox's input RPM) or by downstream components
    /// if the connection allows slip or is coasting (e.g. wheels driving the engine during engine braking).
    /// </summary>
    /// <returns>The current RPM of the input shaft.</returns>
    float GetCurrentInputRPM();
}