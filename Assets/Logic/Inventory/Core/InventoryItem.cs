using Game.InventoryLogic;
using UnityEngine;

[System.Serializable]
public class InventoryItem
{
    public ItemData data;                       // static definition (SO)

    // Optional, polymorphic runtime payload (null for simple stacks)
    [SerializeReference]                        // <- required for polymorphism
    public IRuntimeState runtime;

    public bool IsStackable => data != null && data.stackable;

    /* ------------ constructors ------------ */

    // full constructor – used by complex items (weapons etc.)
    public InventoryItem(ItemData def, IRuntimeState payload)
    {
        data    = def;
        runtime = payload;
    }

    // light-weight factory for simple stackables (ammo, wood, coins …)
    public static InventoryItem CreateStack(ItemData def) =>
        new InventoryItem { data = def };       // note: runtime == null

    // parameter-less for the factory above
    private InventoryItem() { }
}