using UnityEngine;

public class BowBehavior : EquippableBehavior
{
    private IAimProvider aimProvider;
    private bool isPulling = false;
    private bool isCooldown = false;
    private bool isHoldingRightClick = false;

    private float pullTime = 0f;
    private float currentPullForce = 0f;

    [Header("Bow Settings")]
    [SerializeField] private float maxPullTime = 1.5f;
    [SerializeField] private float minShootThreshold = 0.85f;
    [SerializeField] private float baseShootForce = 40f;
    [SerializeField] private float shotCooldown = 0.5f;

    [Header("References")]
    [SerializeField] private EquipmentController equipment;
    [SerializeField] private Transform arrowSpawnPoint;
    [SerializeField] private BowDrawEffect drawEffect;
    [SerializeField] private BowAudioHandler audioHandler;
    [SerializeField] private BowArrowVisualEffect arrowVisualEffect;

    [Header("Ammo Settings")]
    [SerializeField] private ArrowItemData requiredArrowData; // 👈 Reference to the correct arrow type (assign in Inspector!)

    private ArrowItemData currentArrow;

    private void Awake()
    {
        aimProvider = GetComponentInParent<IAimProvider>();
        equipment = GetComponentInParent<EquipmentController>();

        drawEffect = GetComponent<BowDrawEffect>();
        audioHandler = GetComponent<BowAudioHandler>();
        arrowVisualEffect = GetComponent<BowArrowVisualEffect>();
        arrowVisualEffect?.SetArrowVisibility(false);
    }

    private void Update()
    {
        if (isPulling)
        {
            pullTime += Time.deltaTime;
            currentPullForce = Mathf.Clamp01(pullTime / maxPullTime);

            drawEffect?.UpdateDraw(currentPullForce);
            arrowVisualEffect?.UpdateDraw(currentPullForce);

            if (currentPullForce >= minShootThreshold)
                audioHandler?.StartLoop();
        }
    }

    public override void OnFire2Down()
    {
        isHoldingRightClick = true;

        if (!isPulling && !isCooldown)
            TryStartPull();
    }

    public override void OnFire2Up()
    {
        isHoldingRightClick = false;

        if (isPulling)
            CancelPull();
    }

    public override void OnFire1Down()
    {
        if (isPulling && currentPullForce >= minShootThreshold)
        {
            Shoot(currentPullForce);
        }
    }

    private void TryStartPull()
    {
        if (equipment == null || requiredArrowData == null)
            return;

        var inventory = equipment.GetPlayerInventory();
        var stack = inventory?.FindItem(requiredArrowData); // ✅ New method!

        if (stack == null || stack.quantity <= 0)
        {
            Debug.Log("[Bow] No required arrows available.");
            return;
        }

        currentArrow = (ArrowItemData)stack.data; // ✅ Now we reference the real stack
        StartPull();
    }

    private void StartPull()
    {
        isPulling = true;
        pullTime = 0f;
        currentPullForce = 0f;

        equipment?.SetForceSlowWalk(true);

        audioHandler?.PlayDrawStart();
        arrowVisualEffect?.SetArrowVisibility(true);
    }

    private void CancelPull()
    {
        isPulling = false;
        pullTime = 0f;
        currentPullForce = 0f;

        equipment?.SetForceSlowWalk(false);

        drawEffect?.StopDraw();
        audioHandler?.StopLoop();
        arrowVisualEffect?.SetArrowVisibility(false);
        arrowVisualEffect?.UpdateDraw(0f);
    }

    private void Shoot(float normalizedPower)
    {
        isPulling = false;
        pullTime = 0f;
        currentPullForce = 0f;

        equipment?.SetForceSlowWalk(false);

        drawEffect?.StopDraw();
        audioHandler?.PlayRelease();
        audioHandler?.StopLoop();
        arrowVisualEffect?.SetArrowVisibility(false);
        arrowVisualEffect?.UpdateDraw(0f);

        if (currentArrow == null || currentArrow.projectilePrefab == null)
        {
            Debug.LogWarning("[Bow] Arrow missing or invalid.");
            return;
        }

        Vector3 targetPoint = aimProvider.GetAimHitPoint();
        Vector3 direction = (targetPoint - arrowSpawnPoint.position).normalized;

        var arrowInstance = Instantiate(
            currentArrow.projectilePrefab,
            arrowSpawnPoint.position,
            Quaternion.LookRotation(direction)
        );

        arrowInstance.Launch(direction, baseShootForce * normalizedPower);

        equipment.GetPlayerInventory()?.TryConsumeItem(currentArrow);

        currentArrow = null;

        StartCooldown();
    }

    private void StartCooldown()
    {
        isCooldown = true;
        Invoke(nameof(EndCooldown), shotCooldown);
    }

    private void EndCooldown()
    {
        isCooldown = false;

        if (isHoldingRightClick)
        {
            TryStartPull();
        }
    }

    public override void OnFire1Hold() { }
    public override void OnFire1Up() { }
    public override void OnUtilityDown() { }
    public override void OnUtilityUp() { }
    public override void OnReloadDown() { }
}
