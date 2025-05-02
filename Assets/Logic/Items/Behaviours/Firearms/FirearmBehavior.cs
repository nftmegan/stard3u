using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    private FirearmItemData def;      // static definition (ScriptableObject)
    [SerializeField] private FirearmState state;    // per-instance runtime payload

    private IAimProvider aimProvider;
    private Coroutine reloadRoutine;
    private Coroutine autoFireRoutine;

    private bool  isCooldown;
    private bool  isReloading;
    private float lastShotTime;

    /* ─── ADS state ──────────────────────────────────────────── */
    private Transform _currentWeaponAimPoint;
    private Vector3   _currentCameraAnchorOffset;

    /* ─── Quick access properties ───────────────────────────── */
    private int   MaxBullets => def.magazineSize;
    private float AutoDelay  => 1f / def.fireRate; // unified delay for tap and auto
    private bool  IsAuto     => def.fireMode == FireMode.Auto;
    public  int   CurrentAmmo => state.magazine[0].quantity;

    /* ─── Inspector refs ────────────────────────────────────── */
    [Header("References")]
    [SerializeField] protected Transform firePoint;
    [SerializeField] protected FirearmAudioHandler audioHandler;
    [SerializeField] protected RecoilHandler recoilHandler;
    [SerializeField] protected MuzzleFlashHandler muzzleHandler;

    [Header("ADS Setup")]
    [Tooltip("Reference to the ADSController component on this GameObject.")]
    [SerializeField] protected ADSController adsController;

    [Tooltip("Fallback aim point when no sight is attached.")]
    [SerializeField] protected Transform defaultWeaponAimPointTransform;

    [Tooltip("Fallback camera offset when no sight is attached.")]
    [SerializeField] private Vector3 defaultCameraAnchorOffset = new Vector3(0, 0, 0.15f);

    [Header("Attachment Mounts")]
    [Tooltip("Parent under which all attachments will be instantiated")]
    [SerializeField] private Transform attachmentsRoot;

    // key = attachment‐slot index, value = instantiated GameObject
    private readonly Dictionary<int, GameObject> _activeAttachmentInstances = new();
    // map of mount-point “tags” (e.g. SightMount, MuzzleMount) → Transform
    private readonly Dictionary<string, Transform> _mountPoints = new();

    /* ─── Initialise (called by EquipmentController) ────────── */
    public override void Initialize(InventoryItem inv, ItemContainer ownerInv)
    {
        base.Initialize(inv, ownerInv);

        def   = inv.data    as FirearmItemData;
        state = inv.runtime as FirearmState;
        if (def == null || state == null)
            Debug.LogError($"{name} equipped with wrong ItemData or payload!");

        // cache all mount points
        foreach (Transform t in attachmentsRoot)
            _mountPoints[t.gameObject.tag] = t;

        // subscribe to attachment changes
        state.attachments.OnSlotChanged += HandleAttachmentSlotChanged;

        // seed ADS defaults
        _currentWeaponAimPoint     = defaultWeaponAimPointTransform;
        _currentCameraAnchorOffset = defaultCameraAnchorOffset;
        adsController.SetWeaponAimPoint(_currentWeaponAimPoint);
        adsController.SetCameraAnchorOffset(_currentCameraAnchorOffset);
        adsController.ForceStopAiming();

        // instantiate any already-attached items
        RefreshAllAttachments();
    }

    protected virtual void Awake()
    {
        aimProvider   = GetComponentInParent<IAimProvider>();
        adsController = GetComponent<ADSController>();
    }

    /* ─── Input ------------------------------------------------ */
    public override void OnFire1Down()
    {
        if (isReloading) return;

        if (IsAuto)
        {
            // start continuous fire
            if (autoFireRoutine != null)
                StopCoroutine(autoFireRoutine);

            autoFireRoutine = StartCoroutine(AutoFireCoroutine());
        }
        else
        {
            if (isCooldown) return;
            SingleFire();
        }
    }

    public override void OnFire1Up()
    {
        // stop continuous fire
        if (autoFireRoutine != null)
        {
            StopCoroutine(autoFireRoutine);
            autoFireRoutine = null;
        }
    }

    public override void OnFire2Down()
    {
        if (isReloading) return;

        // use whatever aim point & offset is currently active
        adsController.SetWeaponAimPoint(_currentWeaponAimPoint);
        adsController.SetCameraAnchorOffset(_currentCameraAnchorOffset);
        adsController.StartAiming();
    }

    public override void OnFire2Up()
    {
        adsController?.StopAiming();
    }

    public override void OnReloadDown() => Reload();

    /* ─── Shooting logic ────────────────────────────────────── */
    private void SingleFire()
    {
        if (Time.time - lastShotTime < AutoDelay) return;

        if (CurrentAmmo <= 0)
        {
            audioHandler?.PlayDryFire();
        }
        else
        {
            Shoot();
            ConsumeRound();
        }

        lastShotTime = Time.time;
        StartCooldown();
    }

    protected virtual void Shoot()
    {
        var projItemData = state.magazine[0].item.data as ProjectileItemData;
        var projPrefab   = projItemData?.projectilePrefab;
        if (projPrefab == null || firePoint == null) return;

        Vector3 target = aimProvider.GetAimHitPoint();
        Vector3 dir    = (target - firePoint.position).normalized;

        var proj = Instantiate(projPrefab, firePoint.position, Quaternion.LookRotation(dir));
        proj.Launch(dir, projItemData.baseShootForce);

        audioHandler?.PlayShootSound();
        muzzleHandler?.Muzzle();
        recoilHandler?.ApplyRecoil();
    }

    private void ConsumeRound() => state.magazine[0].ReduceQuantity(1);

    /* ─── Auto-fire coroutine ───────────────────────────────── */
    private IEnumerator AutoFireCoroutine()
    {
        // fire as long as button held, not reloading, and ammo remains
        while (!isReloading && CurrentAmmo > 0)
        {
            Shoot();
            ConsumeRound();
            yield return new WaitForSeconds(AutoDelay);
        }

        // out of ammo or reloading -> play dry fire sound if needed
        if (CurrentAmmo <= 0)
            audioHandler?.PlayDryFire();

        autoFireRoutine = null;
    }

    /* ─── Cool-down ─────────────────────────────────────────── */
    private void StartCooldown()
    {
        isCooldown = true;
        Invoke(nameof(EndCooldown), AutoDelay);
    }

    private void EndCooldown() => isCooldown = false;

    /* ─── Reload ------------------------------------------------ */
    private void Reload()
    {
        if (isReloading || CurrentAmmo >= MaxBullets) return;
        if (reloadRoutine != null) StopCoroutine(reloadRoutine);
        reloadRoutine = StartCoroutine(ReloadCoroutine());
    }

    private IEnumerator ReloadCoroutine()
    {
        isReloading = true;
        // stop any auto-fire while reloading
        if (autoFireRoutine != null)
        {
            StopCoroutine(autoFireRoutine);
            autoFireRoutine = null;
        }

        yield return new WaitForSeconds(0.5f); // play SFX / anim here

        int need = MaxBullets - CurrentAmmo;
        ownerInventory.Withdraw(def.ammoType, need, state.magazine[0]);

        isReloading   = false;
        reloadRoutine = null;
    }

    /* ─── Attachment plumbing ────────────────────────────────── */
    private void HandleAttachmentSlotChanged(int slotIndex)
    {
        if (slotIndex < 0) return;
        RefreshAttachment(slotIndex);
    }

    private void RefreshAllAttachments()
    {
        for (int i = 0; i < state.attachments.Slots.Length; i++)
            RefreshAttachment(i);
    }

    private void RefreshAttachment(int slotIndex)
    {
        // tear down old
        if (_activeAttachmentInstances.TryGetValue(slotIndex, out var oldGo))
        {
            Destroy(oldGo);
            _activeAttachmentInstances.Remove(slotIndex);
        }

        // spawn new if present
        var slot = state.attachments.Slots[slotIndex];
        if (slot.item != null)
        {
            var data = slot.item.data as AttachmentItemData;
            if (data != null && _mountPoints.TryGetValue(data.mountPointTag, out var mount))
            {
                // align prefab to mount’s rotation too
                var go = Instantiate(data.attachmentPrefab, mount.position, mount.rotation, mount);
                go.transform.localPosition = Vector3.zero;
                _activeAttachmentInstances[slotIndex] = go;
                ApplyAttachmentToADS(data, go);
            }
        }

        // if no sight remains, reset to default
        MaybeResetToDefaultAim();
    }

    private void ApplyAttachmentToADS(AttachmentItemData data, GameObject instance)
    {
        if (data.attachmentType != AttachmentType.Sight) return;

        Transform aimTf = FindDeep(instance.transform, data.sightAimPointName);
        if (aimTf == null)
        {
            Debug.LogError($"[Firearm] '{data.attachmentPrefab.name}' missing '{data.sightAimPointName}'", instance);
            return;
        }

        _currentWeaponAimPoint     = aimTf;
        _currentCameraAnchorOffset = data.overrideCameraAnchorOffset
                                    ? data.customCameraAnchorOffset
                                    : defaultCameraAnchorOffset;
    }

    private void MaybeResetToDefaultAim()
    {
        bool hasSight = state.attachments.Slots.Any(s =>
            s.item != null &&
            (s.item.data as AttachmentItemData)?.attachmentType == AttachmentType.Sight
        );

        if (!hasSight)
        {
            _currentWeaponAimPoint     = defaultWeaponAimPointTransform;
            _currentCameraAnchorOffset = defaultCameraAnchorOffset;
        }
    }

    private Transform FindDeep(Transform root, string name)
    {
        if (root.name == name) return root;
        foreach (Transform child in root)
        {
            var found = FindDeep(child, name);
            if (found != null) return found;
        }
        return null;
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        // stop any reload or auto-fire
        isReloading = false;
        if (reloadRoutine != null)
        {
            StopCoroutine(reloadRoutine);
            reloadRoutine = null;
        }
        if (autoFireRoutine != null)
        {
            StopCoroutine(autoFireRoutine);
            autoFireRoutine = null;
        }

        // unsubscribe and clean up attachments
        if (state?.attachments != null)
            state.attachments.OnSlotChanged -= HandleAttachmentSlotChanged;

        foreach (var go in _activeAttachmentInstances.Values)
            Destroy(go);
        _activeAttachmentInstances.Clear();
    }
}