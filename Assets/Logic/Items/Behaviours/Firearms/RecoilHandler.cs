// RecoilHandler.cs
using UnityEngine;

public class RecoilHandler : MonoBehaviour
{
    // REMOVED: [Header("Recoil Settings")]
    // REMOVED: [SerializeField] private RecoilPattern pattern;
    private RecoilPattern pattern; // Keep the variable, but don't serialize it

    [Range(0f, 100f)] public float randomSeed = 0f; // Keep this if you like the Perlin randomness

    // rotation
    private Vector3 originalEuler;
    private Vector3 rotOffset = Vector3.zero;
    private Vector3 rotVelocity = Vector3.zero;

    // position
    private Vector3 originalPos;
    private Vector3 posOffset = Vector3.zero;
    private Vector3 posVelocity = Vector3.zero;

    private void Awake()
    {
        originalEuler = transform.localEulerAngles;
        originalPos = transform.localPosition;
        if (randomSeed <= 0f) randomSeed = Random.Range(0f, 100f);

        // Initialize with a default pattern to prevent null reference errors
        // before the first SetRecoilPattern call from FirearmBehavior.
        pattern = new RecoilPattern();
    }

    /// <summary>
    /// Called by FirearmBehavior to update the recoil parameters this handler uses.
    /// </summary>
    public void SetRecoilPattern(RecoilPattern newPattern)
    {
        // If RecoilPattern is a struct, direct assignment is a copy:
        // this.pattern = newPattern;

        // If RecoilPattern is a class, make a distinct copy if you want to be
        // absolutely sure this handler doesn't accidentally modify the instance
        // held by FirearmBehavior (though it shouldn't). Direct assignment is usually fine here.
        this.pattern = newPattern;

        // Optional: You could validate the pattern here (e.g., ensure min <= max)
    }


    public void ApplyRecoil()
    {
        // Check if pattern is valid (it should be after Awake/SetRecoilPattern)
        if (pattern == null)
        {
             Debug.LogError("[RecoilHandler] Pattern is null, cannot apply recoil!", this);
             return;
        }

        // --- rotation kick ---
        float vOff = RandomPattern(pattern.verticalMin, pattern.verticalMax);
        float hOff = RandomPattern(pattern.horizontalMin, pattern.horizontalMax);
        float rOff = RandomPattern(pattern.rollMin, pattern.rollMax);
        rotOffset += new Vector3(-vOff, hOff, rOff); // Apply immediately to offset

        // --- positional kick (back along local Z) ---
        float zOff = RandomPattern(pattern.kickbackMin, pattern.kickbackMax);
        posOffset += new Vector3(0f, 0f, -zOff); // Apply immediately to offset
    }

    private void Update()
    {
        // Check pattern for recovery duration
         if (pattern == null) return;

        // Use the recovery duration from the CURRENT pattern
        float effectiveRecoveryDuration = Mathf.Max(0.01f, pattern.recoveryDuration); // Prevent zero/negative duration

        // recover rotation
        rotOffset = Vector3.SmoothDamp(rotOffset, Vector3.zero, ref rotVelocity, effectiveRecoveryDuration);
        transform.localEulerAngles = originalEuler + rotOffset;

        // recover position
        posOffset = Vector3.SmoothDamp(posOffset, Vector3.zero, ref posVelocity, effectiveRecoveryDuration);
        transform.localPosition = originalPos + posOffset;
    }

    // Keep RandomPattern or use simple Random.Range if Perlin is not desired
    private float RandomPattern(float min, float max)
    {
         // Simple random:
         // return Random.Range(min, max);

         // Perlin noise (can feel more structured/less erratic):
        float noise = Mathf.PerlinNoise(Time.time * 10f + randomSeed, randomSeed + Time.frameCount * 0.01f); // Add slight variation per frame?
        return Mathf.Lerp(min, max, noise);
    }
}