using System.Collections;
using UnityEngine;

/// <summary>
/// Base class for any firearm (pistol, rifle, SMG …).
/// It pulls static data from <see cref="FireArmItemData"/> and
/// per-instance state (magazine, attachments, durability) from
/// the <see cref="FirearmState"/> payload stored inside the
/// <see cref="InventoryItem"/> passed by <see cref="EquipmentController"/>.
/// </summary>
public abstract class FirearmBehavior : EquippableBehavior
{
    /* ─── Cached static + runtime refs ───────────────────────── */
    private FireArmItemData def;      // static definition (ScriptableObject)
    private FirearmState    state;    // per-instance runtime payload

    private IAimProvider aimProvider;
    private Coroutine    reloadRoutine;

    private bool  isCooldown;
    private bool  isReloading;
    private float lastShotTime;

    /* ─── Quick access properties ───────────────────────────── */
    private int   MaxBullets => def.magazineSize;
    private float TapDelay   => def.shotCooldown;
    private float AutoDelay  => 1f / def.fireRate;
    private bool  IsAuto     => def.fireMode == FireMode.Auto;
    public  int   CurrentAmmo => state.magazine[0].quantity;

    /* ─── Inspector refs ────────────────────────────────────── */
    [Header("References")]
    [SerializeField] protected Transform           firePoint;
    [SerializeField] protected FirearmAudioHandler audioHandler;
    [SerializeField] protected RecoilHandler recoilHandler;
    [SerializeField] protected MuzzleFlashHandler muzzleHandler;

    /* ─── Initialise (called by EquipmentController) ────────── */
    public override void Initialize(InventoryItem inv, ItemContainer ownerInv)
    {
        base.Initialize(inv, ownerInv);

        def   = inv.data    as FireArmItemData;
        state = inv.runtime as FirearmState;

        if (def == null || state == null)
            Debug.LogError($"{name} equipped with wrong ItemData or payload!");
    }

    /* ─── Unity lifecycle ───────────────────────────────────── */
    protected virtual void Awake()
    {
        aimProvider  = GetComponentInParent<IAimProvider>();
    }

    /* ─── Input ------------------------------------------------ */
    public override void OnFire1Down()
    {
        if (isCooldown || isReloading) return;

        SingleFire();
    }

    public override void OnFire1Up()
    {   

    }

    /* ─── Shooting logic ────────────────────────────────────── */
    private void SingleFire()
    {
        if (Time.time - lastShotTime < TapDelay) return;

        if(CurrentAmmo <= 0) {
            audioHandler?.PlayDryFire();            
        }
        else {
            Shoot();
            ConsumeRound();
        }

        lastShotTime = Time.time;
        StartCooldown();
    }

    protected virtual void Shoot()
    {
        var projItemData = state.magazine[0].item.data as ProjectileItemData;
        var projPrefab = projItemData.projectilePrefab;
        
        if (!projPrefab || !firePoint) return;

        Vector3 target = aimProvider.GetAimHitPoint();
        Vector3 dir    = (target - firePoint.position).normalized;

        var proj = Instantiate(projPrefab,
                               firePoint.position,
                               Quaternion.LookRotation(dir));

        proj.Launch(dir, projItemData.baseShootForce);

        audioHandler?.PlayShootSound();
        muzzleHandler?.Muzzle();
        recoilHandler?.ApplyRecoil();
    }

    private void ConsumeRound() => state.magazine[0].ReduceQuantity(1);

    /* ─── Cool-down ─────────────────────────────────────────── */
    private void StartCooldown()
    {
        isCooldown = true;
        Invoke(nameof(EndCooldown), TapDelay);
    }
    private void EndCooldown() => isCooldown = false;

    /* ─── Reload ------------------------------------------------ */
    public override void OnReloadDown() => Reload();

    private void Reload()
    {
        if (isReloading || CurrentAmmo >= MaxBullets) return;
        if (reloadRoutine != null) StopCoroutine(reloadRoutine);
        reloadRoutine = StartCoroutine(ReloadCoroutine());
    }

    private IEnumerator ReloadCoroutine()
    {
        isReloading = true;
        yield return new WaitForSeconds(0.5f);     // play SFX / anim here

        int need = MaxBullets - CurrentAmmo;
        ownerInventory.Withdraw(def.ammoType, need, state.magazine[0]);

        isReloading  = false;
        reloadRoutine = null;
    }

    /* ─── Util ------------------------------------------------- */
    public override void OnUtilityDown() { }
    public override void OnUtilityUp()   { }

    protected override void OnDisable() // Change virtual/protected to protected override
    {
        base.OnDisable(); // Optional: Call base class logic
        // Your existing Firearm disable logic (like setting isReloading = false)
        isReloading = false;
        // Stop reload coroutine if running
        if (reloadRoutine != null)
        {
            StopCoroutine(reloadRoutine);
            reloadRoutine = null;
        }
    }
}