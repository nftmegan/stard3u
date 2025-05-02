using UnityEngine;

[System.Serializable]
public class InventoryItem
{
    public ItemData data;                       // static definition (SO)

    [SerializeReference] public IRuntimeState runtime;

    public bool IsStackable => data != null && data.stackable;

    // full constructor – used by complex items (weapons etc.)
    public InventoryItem(ItemData def, IRuntimeState payload)
    {
        data    = def;
        runtime = payload;
    }

    public InventoryItem(ItemData def)
    {
        data = def;
    }

    // parameter-less for the factory above
    private InventoryItem() { }
}