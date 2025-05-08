using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class MountPointDefinition {
    [Tooltip("Unique ID for this mount point within its parent PartData (e.g., 'EngineOutput', 'WheelHub_FL')")]
    public string mountPointID;
    public Vector3 localPosition = Vector3.zero;
    public Quaternion localRotation = Quaternion.identity;

    [Tooltip("What PartType(s) can attach to this mount point. Leave empty to accept any (not recommended).")]
    public List<PartType> acceptedPartTypes = new List<PartType>();

    [Tooltip("Optional: List of C# interface names (e.g., 'ITorqueReceiver') that the attaching part's PartInstance must implement.")]
    public List<string> requiredInterfaces = new List<string>(); // For runtime checking

    public ConnectionType connectionType = ConnectionType.Structural; // Enum for type of connection

    // Optional: Visual representation for the editor or debugging
    public float gizmoRadius = 0.05f;
    public Color gizmoColor = Color.cyan;
}

// Define these enums in a shared file like Utilities/Enums.cs or directly here if only used by MountPointDefinition
public enum PartType { // Add all your part types
    Undefined, Chassis, Engine, Gearbox, Wheel, FuelTank, Radiator, Turbo,
    Exhaust, Intake, Battery, Alternator, Suspension, Differential, Seat, Door, Hood, Trunk,
    FluidHose, WiringHarness, Structural // ... and so on
}

public enum ConnectionType {
    Structural,         // Just physical attachment
    PowerTransmission,  // Torque/RPM
    FluidFlow,          // Coolant, Fuel, Oil
    ElectricalSignal,   // For sensors, ECU, lights
    MechanicalLinkage,  // Shifter, throttle cable (if physical)
    Generic
}