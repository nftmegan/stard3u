using UnityEngine;

public class HandsBehavior : EquippableBehavior
{
    [Header("Hands Settings")]
    [SerializeField] private float punchCooldown = 0.5f;
    [SerializeField] private float grabReach = 2.0f;

    private bool canPunch = true;

    private void Awake()
    {
        if (TryGetComponent<IRuntimeItem>(out var runtimeItem) && runtimeItem.GetItemData() is HandsItemData handsData)
        {
            punchCooldown = handsData.punchCooldown;
            grabReach = handsData.grabReach;
        }
    }

    public override void OnFire1Down()
    {
        if (canPunch)
        {
            Debug.Log("[Hands] Punch!");
            canPunch = false;
            Invoke(nameof(ResetPunch), punchCooldown);
        }
    }

    public override void OnUtilityDown()
    {
        Debug.Log("[Hands] Trying to grab something...");
    }

    private void ResetPunch()
    {
        canPunch = true;
    }

    // Unused inputs
    public override void OnFire1Hold() { }
    public override void OnFire1Up() { }
    public override void OnFire2Down() { }
    public override void OnFire2Hold() { }
    public override void OnFire2Up() { }
    public override void OnUtilityUp() { }
    public override void OnReloadDown() { }
}