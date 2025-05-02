using UnityEngine;

public class RecoilHandler : MonoBehaviour {
    [Header("Recoil Settings")]
    [SerializeField] private RecoilPattern pattern;
    [Range(0f,100f)] public float randomSeed = 0f;

    // rotation
    private Vector3 originalEuler;
    private Vector3 rotOffset      = Vector3.zero;
    private Vector3 rotVelocity    = Vector3.zero;

    // position
    private Vector3 originalPos;
    private Vector3 posOffset      = Vector3.zero;
    private Vector3 posVelocity    = Vector3.zero;

    private void Awake() {
        originalEuler = transform.localEulerAngles;
        originalPos   = transform.localPosition;
        if (randomSeed <= 0f) randomSeed = Random.Range(0f, 100f);
    }

    public void ApplyRecoil() {
        // --- rotation kick ---
        float vOff = RandomPattern(pattern.verticalMin,   pattern.verticalMax);
        float hOff = RandomPattern(pattern.horizontalMin, pattern.horizontalMax);
        float rOff = RandomPattern(pattern.rollMin,       pattern.rollMax);
        rotOffset += new Vector3(-vOff, hOff, rOff);

        // --- positional kick (back along local Z) ---
        float zOff = RandomPattern(pattern.kickbackMin, pattern.kickbackMax);
        posOffset += new Vector3(0f, 0f, -zOff);
    }

    private void Update() {
        // recover rotation
        rotOffset = Vector3.SmoothDamp(rotOffset, Vector3.zero, ref rotVelocity, pattern.recoveryDuration);
        transform.localEulerAngles = originalEuler + rotOffset;

        // recover position
        posOffset = Vector3.SmoothDamp(posOffset, Vector3.zero, ref posVelocity, pattern.recoveryDuration);
        transform.localPosition = originalPos + posOffset;
    }

    private float RandomPattern(float min, float max) {
        float noise = Mathf.PerlinNoise(Time.time * 10f + randomSeed, randomSeed);
        return Mathf.Lerp(min, max, noise);
    }
}