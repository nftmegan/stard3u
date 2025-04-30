using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public class TimeManager : MonoBehaviour
{
    [Header("Sun (Directional Light)")]
    [SerializeField] private Light sun;

    [Header("Clock")]
    [Tooltip("Real-time seconds for a whole 24-hour game day (timeScale = 1).")]
    public float realSecondsPerDay = 600f;
    [Range(0.1f, 30f)] public float timeScale = 1f;

    [Header("Start Time")]
    [Tooltip("At what time (in hours) the game day starts (0-24).")]
    [Range(0f, 24f)]
    public float startHour = 6f;  // Default = 6:00 AM

    [Header("Lighting Curves")]
    public AnimationCurve sunIntensityCurve;
    public AnimationCurve ambientIntensityCurve;
    public Gradient ambientColorGradient;

    [Header("Optional")]
    [SerializeField] private ReflectionProbe globalReflectionProbe;

    [Header("Stars")]
    [SerializeField] private Material starsMaterial;       // Stars material
    public AnimationCurve starsVisibilityCurve;             // ⭐ new curve to control star alpha

    // ───────── runtime read-outs ─────────
    public float CurrentTime01 { get; private set; }
    public int Hours   => Mathf.FloorToInt(CurrentTime01 * 24f);
    public int Minutes => Mathf.FloorToInt((CurrentTime01 * 24f % 1f) * 60f);

    public float curveT;
    public float currentSunIntensity;

    void Reset()
    {
        sun = FindAnyObjectByType<Light>();
        if (sun && sun.type != LightType.Directional) sun = null;
    }

    void Awake()
    {
        if (!sun) Debug.LogError("TimeManager: Please assign a Directional Light as Sun");
        RenderSettings.ambientMode = AmbientMode.Flat;

        // Initialize the day start
        CurrentTime01 = Mathf.Clamp01(startHour / 24f);
    }

    void Update()
    {
        float inc = Time.deltaTime / realSecondsPerDay * timeScale;
        CurrentTime01 = (CurrentTime01 + inc) % 1f;

        curveT = CurrentTime01;
        currentSunIntensity = sunIntensityCurve.Evaluate(curveT);

        UpdateSun();
        UpdateAmbient();
        UpdateStars(); // ⭐ new call for stars fading

        if (Time.frameCount % 120 == 0)
        {
            DynamicGI.UpdateEnvironment();
            if (globalReflectionProbe && globalReflectionProbe.enabled)
                globalReflectionProbe.RenderProbe();
        }
    }

    void UpdateSun()
    {
        if (!sun) return;
        float angle = CurrentTime01 * 360f - 90f;
        sun.transform.rotation = Quaternion.Euler(angle, 180f, 0f);
        sun.intensity = currentSunIntensity;
    }

    void UpdateAmbient()
    {
        RenderSettings.ambientIntensity = ambientIntensityCurve.Evaluate(CurrentTime01);
        RenderSettings.ambientLight = ambientColorGradient.Evaluate(CurrentTime01);
    }

    void UpdateStars()
    {
        if (!starsMaterial || starsVisibilityCurve == null) return;

        float starAlpha = Mathf.Clamp01(starsVisibilityCurve.Evaluate(CurrentTime01));

        Color baseColor = starsMaterial.GetColor("_BaseColor");
        baseColor.a = starAlpha;
        starsMaterial.SetColor("_BaseColor", baseColor);
    }

    void OnGUI()
    {
        const int boxW = 200, boxH = 100;
        GUI.Box(new Rect(10, 10, boxW, boxH), "Time / Debug");

        GUI.Label(new Rect(22, 32, 140, 20), $"Clock    : {Hours:00}:{Minutes:00}");
        GUI.Label(new Rect(22, 50, 140, 20), $"Curve t  : {curveT:F3}");
        GUI.Label(new Rect(22, 68, 140, 20), $"Sun Int. : {currentSunIntensity:F2}");

        GUI.Label(new Rect(22, 86, 15, 20), "×");
        timeScale = GUI.HorizontalSlider(new Rect(37, 91, 145, 15), timeScale, 0.1f, 30f);
    }
}
