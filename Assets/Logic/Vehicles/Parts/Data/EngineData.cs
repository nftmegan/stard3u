// In Assets/Scripts/Parts/Data/EngineData.cs
using UnityEngine;

[CreateAssetMenu(fileName = "NewEngineData", menuName = "Parts/Data/Engine")]
public class EngineData : CarPartData {
    [Header("Engine Performance")]
    public AnimationCurve torqueCurveNmVsRPM; // X-axis: RPM, Y-axis: Torque (Nm)
    public float maxRPM = 7000f;
    public float idleRPM = 800f;
    public float inertiaKgM2 = 0.5f; // Rotational inertia

    [Header("Engine Requirements & Capacities")]
    public float fuelConsumptionLitersPerFixedTickAtFullLoad = 0.01f; // Tune this!
    public float oilCapacityLiters = 5.0f;
    public float coolantCapacityLiters = 7.0f; // If engine block directly holds coolant
    public float idealOperatingTemperatureCelsius = 90f;
    public float overheatTemperatureCelsius = 120f;

    [Header("Mount Points (IDs from providedMountPoints)")]
    [Tooltip("ID of the MountPointDefinition that serves as the main power output.")]
    public string engineOutputShaftMountID = "EngineOutput";
    // Add more for oil filter, spark plugs, exhaust manifold, intake, etc. if needed by specific systems

    public override IPartRuntimeState CreateDefaultRuntimeState() {
        return new EngineRuntimeState {
            CurrentDurability = this.baseMaxDurability,
            CurrentWear = 0f,
            currentEngineOilLiters = 0f, // Start empty or full?
            currentCoolantLiters = 0f
        };
    }

    protected override void OnValidate() {
        base.OnValidate();
        this.partTypeEnum = PartType.Engine;
        //this.isBulky = true; // Engines are bulky
        if (string.IsNullOrEmpty(this.itemName)) this.itemName = "Engine Block";
    }
}