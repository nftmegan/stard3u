using UnityEngine;

[RequireComponent(typeof(Transform))]
public class HeadBob : MonoBehaviour
{
    [Header("Walking Bob")]
    public float walkBobSpeed = 14f;
    public float walkBobAmount = 0.05f;

    [Header("Idle Breathing")]
    public float idleBobSpeed = 1.5f;
    public float idleBobAmount = 0.02f;

    [Header("Settings")]
    public float blendSpeed = 5f;
    public float movementThreshold = 0.1f;

    [Header("References")]
    public MyCharacterController characterController;

    private Vector3 initialLocalPos;
    private float bobTimer = 0f;

    void Start()
    {
        initialLocalPos = transform.localPosition;

        if (characterController == null)
        {
            characterController = GetComponentInParent<MyCharacterController>();
            if (characterController == null)
            {
                Debug.LogWarning("HeadBob couldn't find MyCharacterController in parent.");
            }
        }
    }

    void Update()
    {
        if (characterController == null || characterController.Motor == null) return;

        Vector3 flatVelocity = new Vector3(characterController.Motor.Velocity.x, 0f, characterController.Motor.Velocity.z);
        float movementMagnitude = flatVelocity.magnitude;

        bobTimer += Time.deltaTime;

        if (movementMagnitude > movementThreshold)
        {
            // Walking bob
            float bobX = Mathf.Sin(bobTimer * walkBobSpeed) * walkBobAmount;
            float bobY = Mathf.Cos(bobTimer * walkBobSpeed * 2f) * walkBobAmount;
            Vector3 targetPos = initialLocalPos + new Vector3(bobX, bobY, 0f);
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * blendSpeed);
        }
        else
        {
            // Idle breathing
            float bobX = Mathf.Cos(bobTimer * idleBobSpeed) * idleBobAmount;
            float bobY = Mathf.Sin(bobTimer * idleBobSpeed) * idleBobAmount;
            Vector3 targetPos = initialLocalPos + new Vector3(bobX, bobY, 0f);
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * blendSpeed);
        }
    }
}
