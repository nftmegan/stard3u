// --- Assets/Logic/Items/Behaviours/Hands/HandsBehavior.cs ---
using UnityEngine;

public class HandsBehavior : EquippableBehavior {
    // PlayerGrabController is now inherited from EquippableBehavior and set in its Initialize

    public override void Initialize(InventoryItem itemInstance, IEquipmentHolder holder, IAimProvider aimProvider) {
        base.Initialize(itemInstance, holder, aimProvider); // This will find playerGrabController

        if (this.playerGrabController == null) { // playerGrabController is inherited
             Debug.LogError($"[HandsBehavior Initialize] CRITICAL: PlayerGrabController not found via base.Initialize! Hands actions will not function.", this);
             this.enabled = false;
        }
    }

    /// <summary>
    /// Fire1 (LMB) with Hands:
    /// - If PlayerGrabController is holding an item, try to attach it. If attach fails, drop it.
    /// - If PlayerGrabController is NOT holding, try to grab a loose item or detach an installed part.
    /// </summary>
    public override void OnFire1Down() {
        if (playerGrabController == null) return;
        playerGrabController.TryGrabOrDetachWorldObject(); // This method in PGC handles both grab/detach and attach attempts
    }

    /// <summary>
    /// Fire2 (RMB) with Hands:
    /// - If PlayerGrabController is holding an item, drop it.
    /// </summary>
    public override void OnFire2Down() {
         if (playerGrabController == null) return;
         if (playerGrabController.IsGrabbing) {
            playerGrabController.DropGrabbedItemWithLMB(); // Or a generic DropGrabbedItem() if you prefer
         }
     }

    /// <summary>
    /// Store (T) with Hands:
    /// - If PlayerGrabController is holding an item, attempt to store it in inventory.
    /// - If PlayerGrabController is NOT holding, attempt to pull selected toolbar item into hands.
    /// </summary>
    public override void OnStoreDown() {
        if (playerGrabController == null) return;
        // PlayerGrabController.HandleStoreAction already implements the desired dual logic
        playerGrabController.HandleStoreAction();
    }

    // Other inputs usually do nothing for basic hands
    public override void OnFire1Hold() {} 
    public override void OnFire1Up() {}
    public override void OnFire2Hold() {}
    public override void OnFire2Up() {}
    public override void OnUtilityDown() { 
        // Could be used to toggle rotation lock if PGC supports it directly
        // if (playerGrabController != null && playerGrabController.IsGrabbing) { /* playerGrabController.ToggleRotationLock(); */ }
    }
    public override void OnUtilityUp() {}
    public override void OnReloadDown() {}
}