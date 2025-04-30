using UnityEngine;
using Game.InventoryLogic;   // ItemContainer

public class BowBehavior : EquippableBehavior
{
    private IAimProvider aimProvider;

    private bool  isPulling            = false;
    private bool  isCooldown           = false;
    private bool  isHoldingRightClick  = false;
    private float pullTime             = 0f;
    private float currentPullForce     = 0f;

    [Header("Bow Settings")]
    [SerializeField] private float maxPullTime       = 1.5f;
    [SerializeField] private float minShootThreshold= 0.85f;
    [SerializeField] private float baseShootForce   = 40f;
    [SerializeField] private float shotCooldown     = 0.5f;

    [Header("References")]
    [SerializeField] private EquipmentController   equipment;
    [SerializeField] private Transform             arrowSpawnPoint;
    [SerializeField] private BowDrawEffect         drawEffect;
    [SerializeField] private BowAudioHandler       audioHandler;
    [SerializeField] private BowArrowVisualEffect  arrowVisualEffect;

    [Header("Ammo Settings")]
    [SerializeField] private ArrowItemData requiredArrowData;

    private PlayerInventory playerInventory;

    /* ────────────────────────── Lifecycle ───────────────────────── */
    private void Awake()
    {
        aimProvider        = GetComponentInParent<IAimProvider>();
        equipment          = GetComponentInParent<EquipmentController>();
        playerInventory    = equipment?.GetPlayerInventory();

        drawEffect         = GetComponent<BowDrawEffect>();
        audioHandler       = GetComponent<BowAudioHandler>();
        arrowVisualEffect  = GetComponent<BowArrowVisualEffect>();
        arrowVisualEffect?.SetArrowVisibility(false);
    }

    private void Update()
    {
        if (!isPulling) return;

        pullTime        += Time.deltaTime;
        currentPullForce = Mathf.Clamp01(pullTime / maxPullTime);

        drawEffect?.UpdateDraw(currentPullForce);
        arrowVisualEffect?.UpdateDraw(currentPullForce);

        if (currentPullForce >= minShootThreshold)
            audioHandler?.StartLoop();
    }

    /* ────────────────────────── Input events ────────────────────── */
    public override void OnFire2Down()
    {
        isHoldingRightClick = true;
        if (!isPulling && !isCooldown) TryStartPull();
    }

    public override void OnFire2Up()
    {
        isHoldingRightClick = false;
        if (isPulling) CancelPull();
    }

    public override void OnFire1Down()
    {
        if (isPulling && currentPullForce >= minShootThreshold)
            Shoot(currentPullForce);
    }

    /* ────────────────────────── Pull logic ─────────────────────── */
    private void TryStartPull()
    {
        if (equipment == null || requiredArrowData == null || playerInventory == null)
            return;

        if (!HasArrow(playerInventory.Container))   // only scan, don’t consume
        {
            Debug.Log("[Bow] No required arrows available.");
            return;
        }

        StartPull();
    }

    private void StartPull()
    {
        isPulling        = true;
        pullTime         = 0f;
        currentPullForce = 0f;

        equipment?.SetForceSlowWalk(true);
        audioHandler?.PlayDrawStart();
        arrowVisualEffect?.SetArrowVisibility(true);
    }

    private void CancelPull()
    {
        isPulling        = false;
        pullTime         = 0f;
        currentPullForce = 0f;

        equipment?.SetForceSlowWalk(false);
        drawEffect?.StopDraw();
        audioHandler?.StopLoop();
        arrowVisualEffect?.SetArrowVisibility(false);
        arrowVisualEffect?.UpdateDraw(0f);
    }

    /* ────────────────────────── Shooting ───────────────────────── */
    private void Shoot(float normalizedPower)
    {
        isPulling        = false;
        pullTime         = 0f;
        currentPullForce = 0f;
        equipment?.SetForceSlowWalk(false);

        drawEffect?.StopDraw();
        audioHandler?.PlayRelease();
        audioHandler?.StopLoop();
        arrowVisualEffect?.SetArrowVisibility(false);
        arrowVisualEffect?.UpdateDraw(0f);

        // Ensure an arrow is actually available now
        if (!playerInventory.TryConsume(requiredArrowData))        // consumes 1
        {
            Debug.LogWarning("[Bow] No arrows left to shoot.");
            return;
        }

        // Spawn projectile
        Vector3 targetPoint = aimProvider.GetAimHitPoint();
        Vector3 dir         = (targetPoint - arrowSpawnPoint.position).normalized;

        var proj = Instantiate(requiredArrowData.projectilePrefab,
                               arrowSpawnPoint.position,
                               Quaternion.LookRotation(dir));
        proj.Launch(dir, baseShootForce * normalizedPower);

        StartCooldown();
    }

    /* ────────────────────────── Cooldown ───────────────────────── */
    private void StartCooldown()
    {
        isCooldown = true;
        Invoke(nameof(EndCooldown), shotCooldown);
    }

    private void EndCooldown()
    {
        isCooldown = false;
        if (isHoldingRightClick) TryStartPull();
    }

    /* ────────────────────────── Helpers ────────────────────────── */
    private bool HasArrow(ItemContainer c)
    {
        for (int i = 0; i < c.Size; i++)
        {
            var s = c[i];
            if (s.item != null && s.item.data == requiredArrowData && s.quantity > 0)
                return true;
        }
        return false;
    }

    /* unused overrides */
    public override void OnFire1Hold()  { }
    public override void OnFire1Up()    { }
    public override void OnUtilityDown(){ }
    public override void OnUtilityUp()  { }
    public override void OnReloadDown() { }
}