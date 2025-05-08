public interface IFluidConnector {
    // Called by a source to push fluid into this connector (which should route it to a container)
    // Returns amount actually transferred.
    float TransferFluidIn(FluidData fluidData, float amountLiters, IFluidConnector sourceConnector);

    // Called by a destination to pull fluid from this connector (which should get it from a container)
    // Returns amount actually transferred and the type of fluid.
    float TransferFluidOut(float requestedAmountLiters, out FluidData fluidTransferred, IFluidConnector destinationConnector);

    bool IsConnected(); // Is this connector currently linked to another?
    ConnectionType GetConnectionType(); // Should return FluidFlow
}