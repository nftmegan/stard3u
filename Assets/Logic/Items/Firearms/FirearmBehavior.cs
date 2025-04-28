using System.Collections;
using UnityEngine;

public abstract class FirearmBehavior : EquippableBehavior
{
    [Header("Firearm Settings")]
    public bool isAuto = false;
    public float fireRate = 0.1f;       // Delay between auto‐shots
    public float shotCooldown = 0.5f;   // Minimum delay between semi‐auto taps

    [Header("Ammo Settings")]
    [Tooltip("How many rounds this magazine can hold")]
    public int maxBullets = 12;
    [Tooltip("The magazine slot that will be filled on reload and drained on fire")]
    public InventorySlot magazineSlot;

    private bool isCooldown  = false;
    private bool isFiring    = false;
    private bool isReloading = false;
    private float lastShotTime;
    private Coroutine reloadRoutine;          // ← keep handle

    [Header("References")]
    [SerializeField] protected EquipmentController   equipment;
    [SerializeField] protected Transform             firePoint;
    [SerializeField] protected FirearmAudioHandler   audioHandler;
    [SerializeField] protected ProjectileItemData    projectileData;

    /// <summary>
    /// How many rounds remain in the magazine right now.
    /// </summary>
    public int CurrentAmmo => magazineSlot != null ? magazineSlot.quantity : 0;

    protected virtual void Awake()
    {
        audioHandler = GetComponent<FirearmAudioHandler>();
        // Do NOT populate magazineSlot.item here.
        // Leave it null until real Reload() is called.
    }

    public override void OnFire1Down()
    {
        if (isCooldown || isReloading || CurrentAmmo <= 0)
            return;

        isFiring = true;
        if (isAuto)
            StartCoroutine(AutoFire());
        else
            SingleFire();
    }

    public override void OnFire1Up()
    {
        isFiring = false;
        if (isAuto)
            StopAllCoroutines();
    }

    private void SingleFire()
    {
        if (Time.time - lastShotTime < shotCooldown || CurrentAmmo <= 0)
            return;

        Shoot();
        ConsumeRound();
        lastShotTime = Time.time;
        StartCooldown();
    }

    private IEnumerator AutoFire()
    {
        while (isFiring && CurrentAmmo > 0)
        {
            if (Time.time - lastShotTime >= fireRate)
            {
                Shoot();
                ConsumeRound();
                lastShotTime = Time.time;
                StartCooldown();
            }
            yield return null;
        }
    }

    /// <summary>
    /// Spawns the projectile and plays firing SFX.
    /// </summary>
    protected virtual void Shoot()
    {
        if (projectileData == null || firePoint == null) return;

        var proj = Instantiate(
            projectileData.projectilePrefab,
            firePoint.position,
            firePoint.rotation
        );
        proj.Launch(firePoint.forward, projectileData.baseShootForce);

        audioHandler?.PlayShootSound();
    }

    private void ConsumeRound()
    {
        magazineSlot.ReduceQuantity(1);
    }

    private void StartCooldown()
    {
        isCooldown = true;
        Invoke(nameof(EndCooldown), shotCooldown);
    }

    private void EndCooldown()
    {
        isCooldown = false;
    }

    // ——— RELOAD ———

    public void Reload()
    {
        if (isReloading || CurrentAmmo >= maxBullets) return;

        // restart if user spam-clicked reload
        if (reloadRoutine != null)
            StopCoroutine(reloadRoutine);

        reloadRoutine = StartCoroutine(ReloadCoroutine());
    }

private IEnumerator ReloadCoroutine()
    {
        isReloading = true;

        // play SFX and wait for its duration or a fallback 1.5 s
        //float sfxLen = audioHandler != null ? audioHandler.PlayReload() : 0f;
        yield return new WaitForSeconds(0.5f);

        var inv = equipment.GetPlayerInventory();
        if (inv != null && projectileData != null)
        {
            int needed = maxBullets - CurrentAmmo;
            inv.Withdraw(projectileData, needed, magazineSlot);
        }

        isReloading  = false;
        reloadRoutine = null;
    }

    public override void OnReloadDown()
    {
        Reload();
    }

    public override void OnUtilityDown() { }
    public override void OnUtilityUp()   { }

    protected virtual void OnDisable()
    {
        // weapon was holstered – abort reload so we can later start a new one
        if (reloadRoutine != null)
        {
            StopCoroutine(reloadRoutine);
            reloadRoutine = null;
        }
        isReloading = false;
        isFiring    = false;
    }
}
