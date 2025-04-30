using UnityEngine;

public enum FireMode { Semi, Auto, Burst }

[CreateAssetMenu(menuName = "Items/Weapon/Firearm")]
public class FireArmItemData : WeaponItemData  // inherits from your existing ItemData
{
    /* ────────── Static weapon tuning (designer edits) ────────── */

    [Header("Ballistics / Fire")]
    public FireMode fireMode       = FireMode.Semi;
    [Tooltip("Shots per second for full-auto, or rpm/interval for burst")]
    public float    fireRate       = 10f;     // rounds-per-second
    public float    shotCooldown   = 0.5f;    // enforced between semi taps

    [Header("Magazine")]
    [Min(1)]
    public int      magazineSize   = 12;
    public ItemData ammoType;                 // which ItemData counts as ammo

    [Header("Attachments")]
    [Min(0)]
    public int      attachmentSlots = 4;      // simple fixed-count model

    [Header("Presentation")]
    public GameObject prefab;                 // RuntimeEquippable root
    public Sprite     hudSprite;

    /* ────────── Factory: build runtime InventoryItem ────────── */

    /// <summary>
    /// Creates a brand-new InventoryItem instance fully initialised with
    /// its own magazine & attachment containers.
    /// </summary>
    public InventoryItem CreateInventoryItem()
    {
        var payload = new FirearmState(magazineSize, attachmentSlots);
        var item    = new InventoryItem(this, payload);
        return item;
    }
}