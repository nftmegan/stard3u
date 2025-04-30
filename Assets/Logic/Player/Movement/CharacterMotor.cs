using UnityEngine;

public class CharacterMotor : MonoBehaviour
{
    private Vector3 moveDirection;
    private Rigidbody rb;

    [Header("Movement Settings")]
    public float walkingSpeed = 2.5f;
    public float runningSpeed = 5f;
    public float acceleration = 2.5f;

    [Header("Directional Speed Multipliers")]
    public float forwardMultiplier = 1f;
    public float backwardMultiplier = 0.5f;
    public float strafeMultiplier = 0.75f;

    [Header("Air Control Settings")]
    [Range(0f, 1f)]
    public float airControlFactor = 0.1f;
    public float airDragFactor = 0.1f;

    [Header("Ground Drag Settings")]
    public float groundDragFactor = 5f;

    [Header("Jump Settings")]
    public float jumpForce = 6.5f;
    public float gravityMultiplier = 2f;
    public float groundCheckDistance = 0.2f;

    [Header("Debug")]
    public bool isGrounded;
    private bool isRunning;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        CheckGrounded();
        ApplyExtraGravity();
        ApplyAirDrag();
        Move();
    }

    public void SetMoveDirection(Vector3 direction, bool running)
    {
        moveDirection = direction;
        isRunning = running;
    }

    public void Move()
    {
        // Determine direction relative to character's forward
        Vector3 localMoveDir = transform.InverseTransformDirection(moveDirection.normalized);
        float directionMultiplier;

        if (localMoveDir.z > 0.7f) // Mostly forward
            directionMultiplier = forwardMultiplier;
        else if (localMoveDir.z < -0.3f) // Mostly backward
            directionMultiplier = backwardMultiplier;
        else // Mostly sideways
            directionMultiplier = strafeMultiplier;

        float baseSpeed = isRunning ? runningSpeed : walkingSpeed;
        float targetSpeed = baseSpeed * directionMultiplier;
        Vector3 desiredVelocity = moveDirection * targetSpeed;

        if (isGrounded)
        {
            if (moveDirection != Vector3.zero)
            {
                Vector3 velocityChange = desiredVelocity - rb.linearVelocity;
                velocityChange.y = 0f;

                Vector3 clampedForce = Vector3.ClampMagnitude(velocityChange, acceleration);
                rb.AddForce(clampedForce, ForceMode.VelocityChange);
            }
            else
            {
                // Apply manual ground drag
                Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                Vector3 groundDrag = -horizontalVelocity * groundDragFactor;
                rb.AddForce(groundDrag, ForceMode.Acceleration);
            }
        }
        else
        {
            Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            Vector3 airVelocityChange = desiredVelocity - horizontalVelocity;
            airVelocityChange.y = 0f;

            Vector3 clampedAirForce = Vector3.ClampMagnitude(airVelocityChange, acceleration * airControlFactor);
            rb.AddForce(clampedAirForce, ForceMode.VelocityChange);
        }
    }

    public void Jump()
    {
        if (!isGrounded) return;

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
    }

    public void Rotate(float x)
    {
        transform.rotation = Quaternion.Euler(0f, x, 0f);
    }

    private void CheckGrounded()
    {
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        isGrounded = Physics.Raycast(origin, Vector3.down, groundCheckDistance + 0.1f);
    }

    private void ApplyExtraGravity()
    {
        if (!isGrounded)
        {
            rb.AddForce(Physics.gravity * gravityMultiplier, ForceMode.Acceleration);
        }
    }

    private void ApplyAirDrag()
    {
        if (!isGrounded)
        {
            Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            Vector3 drag = -horizontalVelocity * airDragFactor;
            rb.AddForce(drag, ForceMode.Acceleration);
        }
    }

    public bool IsGrounded => isGrounded;
    public Vector3 CurrentVelocity => rb.linearVelocity;
}
