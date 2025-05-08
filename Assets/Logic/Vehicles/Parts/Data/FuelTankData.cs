using UnityEngine;

[CreateAssetMenu(fileName = "NewFuelTankData", menuName = "Parts/Data/Fuel Tank")]
public class FuelTankData : CarPartData {
    [Header("Fuel Tank Properties")]
    public float capacityLiters = 50f;
    public FluidData acceptedFuelType; // Reference to a FluidData ScriptableObject

    [Header("Mount Points (IDs from providedMountPoints)")]
    public string fuelOutletMountID = "FuelOutlet";
    public string fuelFillerMountID = "FuelFillerCap";

    public override IPartRuntimeState CreateDefaultRuntimeState() {
        return new FluidContainerRuntimeState { CurrentDurability = this.baseMaxDurability, CurrentWear = 0f, capacityLiters = this.capacityLiters };
    }
    protected override void OnValidate() {
        base.OnValidate();
        this.partTypeEnum = PartType.FuelTank;
        if (string.IsNullOrEmpty(this.itemName)) this.itemName = "Fuel Tank";
    }
}