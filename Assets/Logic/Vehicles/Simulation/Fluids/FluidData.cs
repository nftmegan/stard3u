// In Assets/Scripts/Simulation/Fluids/FluidData.cs
using UnityEngine;

[CreateAssetMenu(fileName = "NewFluidData", menuName = "Simulation/Fluid Data")]
public class FluidData : ScriptableObject { // Note: Not ItemData unless fluids are individually inventoryable items
    public string fluidID; // e.g., "gasoline95", "engine_oil_10w40", "standard_coolant"
    public string displayName;
    public Color visualColor = Color.grey; // For debug or visual representation
    public float densityKgPerLiter = 0.75f; // Example for gasoline
    // Add other properties: viscosity, flammability, freezingPoint, boilingPoint etc.
}