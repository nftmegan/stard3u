using UnityEngine;
using System.Collections;

public class RecoilHandler : MonoBehaviour {
    [Header("Recoil Settings")]
    [SerializeField] private RecoilPattern pattern;
    [Tooltip("Random seed for procedural variation")]
    [Range(0f, 100f)] public float randomSeed = 0f;

    private Vector3 originalLocalEuler;
    private Vector3 currentOffset = Vector3.zero;
    private Vector3 offsetVelocity = Vector3.zero;

    private void Awake() {
        originalLocalEuler = transform.localEulerAngles;
        if (randomSeed <= 0f) randomSeed = Random.Range(0f, 100f);
    }

    /// <summary>
    /// Call this method when the weapon is fired to add recoil.
    /// Supports stacking for full-auto fire.
    /// </summary>
    public void ApplyRecoil() {
        float vOff = RandomRangePattern(pattern.verticalMin, pattern.verticalMax);
        float hOff = RandomRangePattern(pattern.horizontalMin, pattern.horizontalMax);
        currentOffset += new Vector3(-vOff, hOff, 0f);
    }

    private void Update() {
        // Recover smoothly back to zero offset
        currentOffset = Vector3.SmoothDamp(currentOffset, Vector3.zero, ref offsetVelocity, pattern.recoveryDuration);
        // Apply to transform
        transform.localEulerAngles = originalLocalEuler + currentOffset;
    }

    // Generates a value between min and max based on Perlin noise for variation
    private float RandomRangePattern(float min, float max) {
        float noise = Mathf.PerlinNoise(Time.time * 10f + randomSeed, randomSeed);
        return Mathf.Lerp(min, max, noise);
    }
}
