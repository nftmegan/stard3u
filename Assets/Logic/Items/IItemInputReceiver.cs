using UnityEngine;

public interface IItemInputReceiver
{
    // Fire1
    void OnFire1Down();
    void OnFire1Hold();
    void OnFire1Up();

    // Fire2
    void OnFire2Down();
    void OnFire2Hold();
    void OnFire2Up();

    // Utility (formerly G)
    void OnUtilityDown();
    void OnUtilityUp();

    // Reload
    void OnReloadDown();
}