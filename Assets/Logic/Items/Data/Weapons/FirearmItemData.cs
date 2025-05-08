using UnityEngine;
using System.Collections.Generic; // Needed for List

public enum FireMode { Semi, Auto, Burst }

// Ensure your enums exist or are defined elsewhere
// public enum FireMode { Semi, Auto, Burst }
// public struct RecoilPattern { /* Define fields */ }
// public struct SpreadPattern { public void Validate() {} /* Define fields and Validate */ }

[CreateAssetMenu(menuName = "Items/Weapon/Firearm")]
public class FirearmItemData : WeaponItemData { // Inherits WeaponItemData (which inherits ItemData)

    [System.Serializable]
    public struct DefaultAttachmentMapping {
        public string mountPointTag;
        public GameObject defaultPrefab;
    }

    [Header("Firing Mechanism")]
    public FireMode fireMode = FireMode.Semi;
    [Min(0.1f)] public float fireRate = 10f;
    [Min(0.1f)] public float reloadTime = 1.5f;

    [Header("Magazine & Ammunition")]
    [Min(1)] public int magazineSize = 12;
    public ItemData ammoType; // Assign Ammo ItemData SO here

    [Header("Recoil")]
    public RecoilPattern baseRecoilPattern = new RecoilPattern();

    [Header("Spread / Spray")]
    public SpreadPattern baseSpreadPattern = new SpreadPattern();

    [Header("ADS Settings")]
    public Vector3 defaultCameraAnchorOffset = new Vector3(0, 0, 0.15f);
    [Range(0f, 1f)] public float adsVisualRecoilMultiplier = 0.5f;
    [Range(0f, 1f)] public float adsSpreadMultiplier = 0.2f;

    [Header("Attachments")]
    [Min(0)] public int attachmentSlots = 4;
    public List<DefaultAttachmentMapping> defaultAttachments;

    // Factory Method to create an InventoryItem instance with runtime state
    public InventoryItem CreateInventoryItemInstance() {
        // Assuming FirearmState exists and takes attachmentSlots and durability
        var payload = new FirearmRuntimeState(attachmentSlots, 100);
        var item = new InventoryItem(this, payload);
        return item;
    }

    // Editor Validation using override
    protected override void OnValidate() { // Changed private to protected override
        base.OnValidate(); // IMPORTANT: Call the base (WeaponItemData's) OnValidate first

        // --- Firearm Specific Validation ---
        // Clamps
        if (fireRate <= 0) fireRate = 0.1f;
        if (magazineSize < 1) magazineSize = 1;
        if (reloadTime < 0.1f) reloadTime = 0.1f;
        adsVisualRecoilMultiplier = Mathf.Clamp01(adsVisualRecoilMultiplier);
        adsSpreadMultiplier = Mathf.Clamp01(adsSpreadMultiplier);

        // Validate sub-patterns (make sure these structs/classes have Validate methods)
        baseRecoilPattern?.Validate(); // Added null check just in case
        baseSpreadPattern?.Validate(); // Added null check just in case

        // Checks
        if (ammoType == null) Debug.LogWarning($"[{this.name}] Ammo Type is not assigned.", this);

        if (defaultAttachments != null) {
            for (int i = 0; i < defaultAttachments.Count; i++) {
                if (string.IsNullOrEmpty(defaultAttachments[i].mountPointTag)) Debug.LogWarning($"[{this.name}] Default Attachment at index {i} missing Mount Point Tag.", this);
                if (defaultAttachments[i].defaultPrefab == null) Debug.LogWarning($"[{this.name}] Default Attachment at index {i} (Tag: '{defaultAttachments[i].mountPointTag}') missing Default Prefab.", this);
            }
        }
        // --- End Firearm Specific Validation ---
    }

    // --- Placeholder Structs/Classes if not defined elsewhere ---
    // Remove these if you have them defined properly
    // [System.Serializable] public struct RecoilPattern { public void Validate() {} }
    // [System.Serializable] public struct SpreadPattern { public void Validate() {} }

}

// --- Ensure FirearmState exists (Example) ---
// [System.Serializable]
// public class FirearmState : IRuntimeState {
//     public ItemContainer magazine;
//     public ItemContainer attachments;
//     public int durability;
//     public FirearmState(int attachmentSlots, int initialDurability) {
//         magazine = new ItemContainer(1);
//         attachments = new ItemContainer(attachmentSlots);
//         durability = initialDurability;
//     }
// }