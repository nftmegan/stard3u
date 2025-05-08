using UnityEngine;
using System.Collections.Generic;

// Base CarPartData remains largely the same, just ensure it aligns.
public abstract class CarPartData : ItemData {
    [Header("Car Part General Stats")]
    public float baseMaxDurability = 100f;
    public PartType partTypeEnum;

    [Header("Mounting Points Provided by this Part")]
    public List<MountPointDefinition> providedMountPoints;

    public abstract IPartRuntimeState CreateDefaultRuntimeState();

    protected override void OnValidate() {
        base.OnValidate();
        this.category = ItemCategory.CarPart;
        this.stackable = false;
        this.maxStack = 1;
        // Derived classes (like EngineData) should set 'isBulky = true;' if appropriate.
        // worldPrefab on CarPartData should point to the prefab containing the PartInstance-derived script.
        if (this.worldPrefab != null && this.worldPrefab.GetComponent<PartInstance>() == null) {
            Debug.LogWarning($"CarPartData '{this.name}' has a worldPrefab '{this.worldPrefab.name}' that is missing a PartInstance-derived component. This is usually incorrect.", this.worldPrefab);
        }
    }
}