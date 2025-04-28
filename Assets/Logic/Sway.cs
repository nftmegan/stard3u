using UnityEngine;

public class WeaponSway : MonoBehaviour
{
    [Header("Sway Settings")]
    public float swayAmount = 0.05f;
    public float swaySmooth = 8f;
    public float maxSway = 0.06f;

    [Header("Rotation Settings")]
    public float rotationAmount = 4f;
    public float rotationSmooth = 12f;

    private Vector3 initialLocalPos;
    private Quaternion initialLocalRot;

    private Vector3 targetSwayPos;
    private Quaternion targetSwayRot;

    private Quaternion lastLocalRotation;

    private void Start()
    {
        initialLocalPos = transform.localPosition;
        initialLocalRot = transform.localRotation;
        lastLocalRotation = transform.localRotation;
    }

    private void Update()
    {
        Quaternion currentLocalRot = transform.localRotation;
        Quaternion deltaRotation = Quaternion.Inverse(lastLocalRotation) * currentLocalRot;
        deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);

        Vector3 deltaEuler = axis * angle;
        lastLocalRotation = currentLocalRot;

        if (deltaEuler.sqrMagnitude < 0.0001f)
        {
            // Smoothly return to center
            targetSwayPos = Vector3.Lerp(targetSwayPos, Vector3.zero, Time.deltaTime * swaySmooth);
            targetSwayRot = Quaternion.Slerp(targetSwayRot, Quaternion.identity, Time.deltaTime * rotationSmooth);
        }
        else
        {
            // --- Position Sway ---
            Vector3 sway = new Vector3(-deltaEuler.y, -deltaEuler.x, 0f) * swayAmount;
            sway = Vector3.ClampMagnitude(sway, maxSway);
            targetSwayPos = Vector3.Lerp(targetSwayPos, sway, Time.deltaTime * swaySmooth);

            // --- Rotation Sway ---
            Quaternion swayRot = Quaternion.Euler(deltaEuler.x * rotationAmount, -deltaEuler.y * rotationAmount, 0f);
            targetSwayRot = Quaternion.Slerp(targetSwayRot, swayRot, Time.deltaTime * rotationSmooth);
        }

        // Apply final sway
        transform.localPosition = Vector3.Lerp(transform.localPosition, initialLocalPos + targetSwayPos, Time.deltaTime * swaySmooth);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, initialLocalRot * targetSwayRot, Time.deltaTime * rotationSmooth);
    }
}
