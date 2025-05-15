using UnityEngine;

public class HandsBehavior : EquippableBehavior {

    // Dependencies
    private PlayerGrabController _playerGrabController;
    // Inherited: ownerEquipmentHolder, ownerAimProvider

    public override void Initialize(InventoryItem itemInstance, IEquipmentHolder holder, IAimProvider aimProvider) {
        base.Initialize(itemInstance, holder, aimProvider);

        // Find PlayerGrabController via PlayerManager (most reliable way)
        if (this.ownerEquipmentHolder is Component componentHolder) {
            PlayerManager pm = componentHolder.GetComponentInParent<PlayerManager>();
            if (pm != null) { _playerGrabController = pm.GrabController; }
        }
        // Fallback if PlayerManager context isn't available
        if (_playerGrabController == null) { _playerGrabController = FindFirstObjectByType<PlayerGrabController>(); }

        if (_playerGrabController == null) {
             Debug.LogError($"[HandsBehavior Initialize] CRITICAL: PlayerGrabController not found! Hands actions will not function.", this);
             this.enabled = false;
        }
    }

    // --- Input Handlers: Act as Proxies to PlayerGrabController ---

    /// <summary>
    /// Fire1: Tries to grab/detach if hands are empty (not holding via grab controller),
    /// or tries to attach if hands are holding a part via grab controller.
    /// </summary>
    public override void OnFire1Down() {
        if (_playerGrabController == null) return;

        // PlayerManager now routes Fire1 based on grab state.
        // This method will only be called if HandsBehavior is the active equippable,
        // which usually means the player is *not* currently grabbing via PlayerGrabController.
        // Therefore, the primary action here is TryGrabOrDetach.
        // The TryAttach case is handled when Fire1 is pressed *while* PlayerGrabController.IsGrabbing is true.
        // PlayerManager's HandleFire1Start checks IsGrabbing.

        // If this method IS called, it means hands are "active", so try grabbing.
         _playerGrabController.TryGrabOrDetachWorldObject();

        // Old logic (kept for reference, but PlayerManager now decides):
        // if (_playerGrabController.IsGrabbing) {
        //     _playerGrabController.TryAttachGrabbedPart();
        // } else {
        //     _playerGrabController.TryGrabOrDetachWorldObject();
        // }
     }

    /// <summary>
    /// Fire2: Drops the item currently held by PlayerGrabController.
    /// </summary>
    public override void OnFire2Down() {
         if (_playerGrabController == null || !_playerGrabController.IsGrabbing) return;

         // PlayerManager now routes Fire2 based on grab state. This will only be called
         // if Hands are active AND PlayerGrabController isn't grabbing (which shouldn't happen).
         // The drop logic is handled in PlayerManager's HandleFire2Start when IsGrabbing is true.

         // If somehow called when grabbing:
         // Vector3 dropVelocity = Vector3.zero;
         // if (this.ownerAimProvider != null) { dropVelocity = this.ownerAimProvider.GetLookRay().direction * 2f; }
         // _playerGrabController.DropGrabbedItem(dropVelocity);
     }

    /// <summary>
    /// Utility: Toggles rotation lock for the item held by PlayerGrabController.
    /// </summary>
    public override void OnUtilityDown() {
         if (_playerGrabController == null || !_playerGrabController.IsGrabbing) return;

         // PlayerManager now routes Utility based on grab state. This will only be called
         // if Hands are active AND PlayerGrabController isn't grabbing. In that case, do nothing.

         // If somehow called when grabbing:
         // _playerGrabController.ToggleGrabbedItemRotationLock();
     }

    // OnStoreDown() is REMOVED from IItemInputReceiver and this class.
    // The 'Store' action ('T') is handled directly by PlayerManager calling PlayerGrabController.HandleStoreAction().

    // --- Unused Base Input Methods (for HandsBehavior) ---
    public override void OnFire1Hold() {} // Hold actions might be needed later?
    public override void OnFire1Up() {}
    public override void OnFire2Hold() {}
    public override void OnFire2Up() {}
    public override void OnUtilityUp() {}
    public override void OnReloadDown() {} // Hands don't reload
}