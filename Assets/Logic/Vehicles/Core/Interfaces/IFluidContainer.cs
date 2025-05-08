// In Assets/Scripts/Core/Interfaces/IFluidContainer.cs
public interface IFluidContainer {
    FluidData ContainedFluidType { get; } // The type of fluid currently in the container (can be null)
    float CurrentFluidLiters { get; }
    float MaxCapacityLiters { get; }
    float RemainingCapacityLiters { get; }

    /// <summary>
    /// Attempts to add fluid to the container.
    /// </summary>
    /// <param name="fluidData">The type of fluid to add.</param>
    /// <param name="amountLiters">The amount to try and add.</param>
    /// <returns>The amount of fluid actually added (might be less than requested if full or wrong type).</returns>
    float TryAddFluid(FluidData fluidData, float amountLiters);

    /// <summary>
    /// Attempts to remove fluid from the container.
    /// </summary>
    /// <param name="amountLiters">The amount to try and remove.</param>
    /// <returns>The amount of fluid actually removed (might be less than requested if not enough available).</returns>
    float TryRemoveFluid(float amountLiters);

    /// <summary>
    /// Checks if this container can accept the specified fluid type.
    /// </summary>
    bool CanAcceptFluidType(FluidData fluidData);
}