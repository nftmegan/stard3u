using UnityEngine;

public class PickaxeSwingAnimator : MonoBehaviour
{
    [Header("Swing Targets")]
    [SerializeField] private Transform pickaxeTransform;

    [Header("Swing Settings")]
    [SerializeField] private Vector3 swingRotation = new Vector3(60f, 0f, 0f);
    [SerializeField] private float swingDuration = 0.3f;
    [SerializeField] private float returnDuration = 0.2f;
    [SerializeField] private AnimationCurve swingCurve;

    private Quaternion originalRotation;
    private bool isSwinging = false;
    private float swingTimer = 0f;
    private bool returning = false;

    private void Start()
    {
        originalRotation = pickaxeTransform.localRotation;
        if (swingCurve == null || swingCurve.length == 0)
        {
            swingCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // default smooth curve
        }
    }

    public void TriggerSwing()
    {
        if (!isSwinging)
        {
            isSwinging = true;
            swingTimer = 0f;
            returning = false;
        }
    }

    private void Update()
    {
        if (!isSwinging) return;

        swingTimer += Time.deltaTime;
        float duration = returning ? returnDuration : swingDuration;
        float t = Mathf.Clamp01(swingTimer / duration);
        float curveT = swingCurve.Evaluate(t);

        if (!returning)
        {
            // Swing forward
            Quaternion targetRotation = originalRotation * Quaternion.Euler(swingRotation);
            pickaxeTransform.localRotation = Quaternion.Slerp(originalRotation, targetRotation, curveT);
        }
        else
        {
            // Return to original rotation
            Quaternion targetRotation = originalRotation * Quaternion.Euler(swingRotation);
            pickaxeTransform.localRotation = Quaternion.Slerp(targetRotation, originalRotation, curveT);
        }

        if (swingTimer >= duration)
        {
            if (!returning)
            {
                // Start returning to idle
                returning = true;
                swingTimer = 0f;
            }
            else
            {
                // Done
                isSwinging = false;
                pickaxeTransform.localRotation = originalRotation;
            }
        }
    }
}