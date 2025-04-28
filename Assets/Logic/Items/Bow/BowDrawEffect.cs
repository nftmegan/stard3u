using UnityEngine;

public class BowDrawEffect : MonoBehaviour
{
    [Header("Target to Affect")]
    [Tooltip("The transform that will be moved by draw effects (usually the bow model root). If null, this GameObject will be affected.")]
    [SerializeField] private Transform target;

    [Header("Draw Positions")]
    [SerializeField] private Vector3 restLocalPosition = new Vector3(0f, 0f, 0f);
    [SerializeField] private Vector3 drawnLocalPosition = new Vector3(0.05f, 0.05f, -0.2f);

    [Header("Draw Rotations")]
    [SerializeField] private Vector3 restLocalEulerAngles = new Vector3(0f, 0f, 0f);
    [SerializeField] private Vector3 drawnLocalEulerAngles = new Vector3(-5f, 3f, 0f); // just an example

    [Header("Shake Settings")]
    [SerializeField] private float shakeIntensity = 0.005f;
    [SerializeField] private float shakeSpeed = 30f;

    [Header("Return Settings")]
    [SerializeField] private float returnSpeed = 5f;

    private float targetPullAmount = 0f;
    private float currentPullAmount = 0f;
    private bool isPulling = false;

    private void Awake()
    {
        if (target == null)
            target = transform;

        target.localPosition = restLocalPosition;
        target.localRotation = Quaternion.Euler(restLocalEulerAngles);
    }

    /// <summary>Call this every frame while pulling to update effect (value should be 0–1).</summary>
    public void UpdateDraw(float normalizedPull)
    {
        isPulling = true;
        targetPullAmount = Mathf.Clamp01(normalizedPull);
    }

    /// <summary>Call when pulling stops (either fire or cancel).</summary>
    public void StopDraw()
    {
        isPulling = false;
    }

    private void Update()
    {
        if (isPulling)
        {
            currentPullAmount = Mathf.Lerp(currentPullAmount, targetPullAmount, Time.deltaTime * 12f);
        }
        else
        {
            currentPullAmount = Mathf.MoveTowards(currentPullAmount, 0f, Time.deltaTime * returnSpeed);
        }

        // Interpolate position
        Vector3 basePosition = Vector3.Lerp(restLocalPosition, drawnLocalPosition, currentPullAmount);

        if (isPulling && currentPullAmount > 0.1f)
        {
            float shakePower = shakeIntensity * currentPullAmount;
            basePosition += new Vector3(
                (Mathf.PerlinNoise(Time.time * shakeSpeed, 0f) - 0.5f),
                (Mathf.PerlinNoise(0f, Time.time * shakeSpeed) - 0.5f),
                0f
            ) * shakePower;
        }

        // Interpolate rotation
        Quaternion baseRotation = Quaternion.Lerp(
            Quaternion.Euler(restLocalEulerAngles),
            Quaternion.Euler(drawnLocalEulerAngles),
            currentPullAmount
        );

        target.localPosition = basePosition;
        target.localRotation = baseRotation;
    }
}
