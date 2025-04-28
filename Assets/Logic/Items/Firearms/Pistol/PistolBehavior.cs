using UnityEngine;

public class PistolBehavior : FirearmBehavior
{
    [Header("Pistol Settings")]
    [SerializeField] private float semiAutoDelay = 0.2f;

    protected override void Awake()
    {
        base.Awake();
        isAuto   = false;
        fireRate = semiAutoDelay;
    }
}