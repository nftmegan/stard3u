using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewGearboxData", menuName = "Parts/Data/Gearbox")]
public class GearboxData : CarPartData {
    [Header("Gearbox Properties")]
    // --- MAKE SURE THIS IS SPELLED CORRECTLY ---
    public List<float> forwardGearRatios; // e.g., [2.97, 2.07, 1.43, 1.00, 0.84]
    // --- END CHECK ---
    public float reverseGearRatio = -2.5f;
    public float finalDriveRatio = 3.42f;
    [Range(0.5f, 1.0f)] public float efficiency = 0.95f;
    public float shiftTimeSeconds = 0.2f;

    [Header("Mount Points (IDs from providedMountPoints)")]
    public string inputShaftMountID = "GearboxInput";
    public string outputShaftMountID = "GearboxOutput";

    public override IPartRuntimeState CreateDefaultRuntimeState() {
        return new GearboxRuntimeState { /* Default values */ };
    }
    protected override void OnValidate() { /* ... */ }
}