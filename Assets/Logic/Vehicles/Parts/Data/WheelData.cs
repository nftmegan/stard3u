// In Assets/Scripts/Parts/Data/WheelData.cs
using UnityEngine;

[CreateAssetMenu(fileName = "NewWheelData", menuName = "Parts/Data/Wheel")]
public class WheelData : CarPartData {
    [Header("Wheel & Tire Properties")]
    public float radiusMetres = 0.3f;
    // public float tireWidthMetres = 0.205f; // For more advanced grip models
    public float inertiaKgM2 = 1.0f; // Rotational inertia of wheel/tire assembly
    public float rollingResistanceCoefficient = 0.015f;
    public float maxBrakeTorqueNm = 2000f;
    public float maxSteerAngleDegrees = 35f; // Only if this wheel is steerable
    public bool isSteerable = false;
    public bool isDriven = true; // Can this wheel receive power?

    [Header("Mount Points (IDs from providedMountPoints)")]
    public string axleInputMountID = "WheelHub"; // Where it connects to axle/differential

    public override IPartRuntimeState CreateDefaultRuntimeState() {
        return new WheelRuntimeState { CurrentDurability = this.baseMaxDurability, CurrentWear = 0f, tirePressurePsi = 32f };
    }

    protected override void OnValidate() {
        base.OnValidate();
        this.partTypeEnum = PartType.Wheel;
        if (string.IsNullOrEmpty(this.itemName)) this.itemName = "Wheel Assembly";
    }
}