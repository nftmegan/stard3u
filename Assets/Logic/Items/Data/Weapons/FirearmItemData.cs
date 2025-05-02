using UnityEngine;
using System.Collections.Generic; // Needed for List

public enum FireMode { Semi, Auto, Burst }

[CreateAssetMenu(menuName = "Items/Weapon/Firearm")]
public class FirearmItemData : WeaponItemData
{
    // Struct for Default Attachment Mapping
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
    public ItemData ammoType;

    [Header("Recoil")]
    public RecoilPattern baseRecoilPattern = new RecoilPattern();

    // --- ADDED SPREAD PATTERN ---
    [Header("Spread / Spray")]
    public SpreadPattern baseSpreadPattern = new SpreadPattern(); // Defines base inaccuracy/bloom
    // --- END ADDED ---

    [Header("ADS Settings")]
    public Vector3 defaultCameraAnchorOffset = new Vector3(0, 0, 0.15f);
    [Tooltip("Multiplier applied to VISUAL recoil magnitude when Aiming Down Sights. < 1 reduces visual kick.")]
    [Range(0f, 1f)] public float adsVisualRecoilMultiplier = 0.5f; // How much visual kick is reduced by ADS
    [Tooltip("Multiplier applied to calculated spread angle when Aiming Down Sights. < 1 reduces inaccuracy.")]
    [Range(0f, 1f)] public float adsSpreadMultiplier = 0.2f; // How much random spread is reduced by ADS
    // --- END ADS ---

    [Header("Attachments")]
    [Min(0)] public int attachmentSlots = 4;
    public List<DefaultAttachmentMapping> defaultAttachments;

    // Factory Method
    public InventoryItem CreateInventoryItem() {
        var payload = new FirearmState(attachmentSlots, 100);
        var item = new InventoryItem(this, payload);
        return item;
    }

    // Editor Validation
    private void OnValidate() {
        // Clamps
        if (fireRate <= 0) fireRate = 0.1f;
        if (magazineSize < 1) magazineSize = 1;
        if (reloadTime < 0.1f) reloadTime = 0.1f;
        adsVisualRecoilMultiplier = Mathf.Clamp01(adsVisualRecoilMultiplier);
        adsSpreadMultiplier = Mathf.Clamp01(adsSpreadMultiplier);

        // Validate sub-patterns
        baseSpreadPattern?.Validate();

        // Checks
        if (ammoType == null) Debug.LogWarning($"[{this.name}] Ammo Type is not assigned.", this);
        // Prefab check removed - relies on Registry

        if (defaultAttachments != null) {
            for (int i = 0; i < defaultAttachments.Count; i++) {
                if (string.IsNullOrEmpty(defaultAttachments[i].mountPointTag)) Debug.LogWarning($"[{this.name}] Default Attachment at index {i} is missing a Mount Point Tag.", this);
                if (defaultAttachments[i].defaultPrefab == null) Debug.LogWarning($"[{this.name}] Default Attachment at index {i} (Tag: '{defaultAttachments[i].mountPointTag}') is missing a Default Prefab.", this);
            }
        }
    }
}