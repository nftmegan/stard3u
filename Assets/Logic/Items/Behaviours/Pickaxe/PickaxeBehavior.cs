using UnityEngine;

public class PickaxeBehavior : MonoBehaviour, IItemInputReceiver
{
    [Header("References")]
    [SerializeField] private PickaxeSwingAnimator pickaxeSwingAnimator;

    private void Awake()
    {
        pickaxeSwingAnimator = GetComponent<PickaxeSwingAnimator>();
    }

    public void OnFire1Down()
    {
        Debug.Log("Pickaxe: Swing down, might mine a resource!");
        
        pickaxeSwingAnimator.TriggerSwing();
    }

    public void OnFire1Hold() {}
    public void OnFire1Up()   {}

    // Fire2
    public void OnFire2Down() { Debug.Log("Pickaxe: Maybe a heavier swing?"); }
    public void OnFire2Hold() {}
    public void OnFire2Up()   {}

    // Utility
    public void OnUtilityDown() { Debug.Log("Pickaxe: Some extra utility action"); }
    public void OnUtilityUp()   {}

    // Reload
    public void OnReloadDown()  {}
}