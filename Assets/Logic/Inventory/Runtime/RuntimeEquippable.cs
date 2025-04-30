using UnityEngine;
using Game.InventoryLogic;

[DisallowMultipleComponent]
public sealed class RuntimeEquippable : MonoBehaviour
{
    [SerializeField] private string itemCode;     // matches ItemData.itemCode

    private IEquippableInstance instance;        // cached target script

    private void Awake() =>
        instance = GetComponent<IEquippableInstance>();

    public string  ItemCode => itemCode;

    /// Called by EquipmentController right after SetActive(true)
    public void Initialize(InventoryItem runtimeItem, ItemContainer playerInv)
        => instance?.Initialize(runtimeItem, playerInv);
}