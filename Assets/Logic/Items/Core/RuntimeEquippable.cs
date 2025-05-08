using UnityEngine;

[DisallowMultipleComponent]
public sealed class RuntimeEquippable : MonoBehaviour {
    [SerializeField] private string itemCode;
    private IEquippableInstance instanceBehavior;
    public string ItemCode => itemCode;

    private void Awake() {
        instanceBehavior = GetComponent<IEquippableInstance>();
        if (instanceBehavior == null) Debug.LogError($"[RE] Missing IEquippableInstance on {gameObject.name}!", this);
    }

    // Signature matches the new IEquippableInstance
    public void Initialize(InventoryItem itemInstance, IEquipmentHolder holder, IAimProvider aimProvider) {
        if (instanceBehavior != null) {
            instanceBehavior.Initialize(itemInstance, holder, aimProvider);
        } else {
             Debug.LogError($"[RE] Cannot forward Initialize on {gameObject.name}: IEquippableInstance missing!", this);
        }
    }
}