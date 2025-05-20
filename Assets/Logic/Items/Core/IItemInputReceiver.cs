using UnityEngine;

public interface IItemInputReceiver
{
    // Fire1 (LMB)
    void OnFire1Down();
    void OnFire1Hold();
    void OnFire1Up();

    // Fire2 (RMB)
    void OnFire2Down();
    void OnFire2Hold();
    void OnFire2Up();

    // Utility (E/F) - Renamed from UtilityPerformed/Canceled for consistency
    void OnUtilityDown();
    void OnUtilityUp();

    // Reload (R)
    void OnReloadDown();

    // Store (T) - NEW METHOD
    void OnStoreDown(); 
}