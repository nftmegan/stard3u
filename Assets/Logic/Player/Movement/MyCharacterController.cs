using UnityEngine;
using KinematicCharacterController;
using System.Collections.Generic;

// Interface ICharacterController from KCC remains
public class MyCharacterController : MonoBehaviour, ICharacterController
{
    public KinematicCharacterMotor Motor;

    [Header("Movement Settings")]
    public float walkSpeed = 2.5f;
    public float runSpeed = 5f;
    public float slowSpeed = 1.5f;
    public float crouchSpeed = 1.2f;
    public float acceleration = 10f;
    public float airControlMultiplier = 0.3f;
    public float maxAirSpeed = 5f;

    [Header("Jumping")]
    public float jumpForce = 6f;
    public float groundGraceTime = 0.15f;

    [Header("Crouch")]
    public float standingHeight = 2f;
    public float crouchedHeight = 1f;
    public float capsuleRadius = 0.5f;
    [Tooltip("Speed at which capsule height changes")]
    public float crouchTransitionSpeed = 10f;

    [Header("Head")]
    [Tooltip("Speed at which the visual head follows capsule height")]
    public float headFollowSpeed = 15f;

    [Header("Gravity")]
    public Vector3 gravity = new Vector3(0, -25f, 0);

    [Header("Debug")]
    public bool isCrouching;
    public float smoothedHeight { get; private set; }

    // Public read-only properties for state if needed externally
    public Vector3 Velocity { get; private set; }
    public bool IsSprinting { get; private set; }
    public bool IsSlowWalking { get; private set; }


    // Internal state variables
    private Vector2 _moveInputAxis; // Store raw axis input
    private Vector3 _lookDirection = Vector3.forward;
    private bool _jumpRequested;
    private bool _jumpConsumed;
    private float _timeSinceLastGrounded = Mathf.Infinity;
    private float _timeSinceJumpRequested = Mathf.Infinity;
    private bool _isCrouchHeld; // Changed from _shouldBeCrouching
    private bool _forceSlowWalk; // Keep this for external control
    private float _currentSpeed;
    private List<Collider> _ignored = new(); // For KCC collision filtering

    private float _visualHeadHeight;

    private void Awake()
    {
        Motor.CharacterController = this;
        smoothedHeight = standingHeight;
        _visualHeadHeight = standingHeight; // Initialize visual height
    }

    // --- NEW Public Methods for Input ---

    public void SetMoveInput(Vector2 moveAxis)
    {
        _moveInputAxis = moveAxis;
    }

    public void SetLookDirection(Vector3 worldLookDirection) // Called by PlayerManager
    {
        if (worldLookDirection.sqrMagnitude > 0.01f)
            _lookDirection = worldLookDirection.normalized;
    }

    public void OnJumpPressed() // Called by PlayerManager on Jump event
    {
        _jumpRequested = true;
        _timeSinceJumpRequested = 0f;
    }

    public void SetSprint(bool value) // Called by PlayerManager on Sprint event start/cancel
    {
        IsSprinting = value;
    }

    public void SetCrouch(bool value) // Called by PlayerManager on Crouch event start/cancel
    {
        _isCrouchHeld = value;
    }

     public void SetSlowWalk(bool value) // Called by PlayerManager on SlowWalk event start/cancel
    {
        IsSlowWalking = value;
    }

    public void ForceSlowWalk(bool value) // Keep this for external systems (e.g., aiming)
    {
         _forceSlowWalk = value;
    }

    // --- KCC Interface Implementation ---

    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        _timeSinceLastGrounded += deltaTime;
        _timeSinceJumpRequested += deltaTime;

        // Calculate current target speed based on state
        if (_isCrouchHeld)
        {
            _currentSpeed = crouchSpeed;
        }
        else
        {
            _currentSpeed = (_forceSlowWalk || IsSlowWalking) ? slowSpeed : (IsSprinting ? runSpeed : walkSpeed);
        }

        // Transform move input axis to world space direction based on look direction
        Vector3 inputDirection = new Vector3(_moveInputAxis.x, 0f, _moveInputAxis.y).normalized;
        Vector3 right = Vector3.Cross(Motor.CharacterUp, _lookDirection).normalized; // Ensure normalization
        Vector3 forward = Vector3.Cross(right, Motor.CharacterUp).normalized;      // Ensure normalization
        Vector3 moveDirectionWorld = (right * inputDirection.x + forward * inputDirection.z);
        // Normalize moveDirectionWorld only if input is non-zero to avoid normalizing zero vector
        if (moveDirectionWorld.sqrMagnitude > 0.01f)
            moveDirectionWorld.Normalize();


        if (Motor.GroundingStatus.IsStableOnGround)
        {
            _timeSinceLastGrounded = 0f;
            _jumpConsumed = false; // Reset jump flag when grounded

            // Calculate target velocity on ground
            Vector3 targetVelocity = moveDirectionWorld * _currentSpeed;

            // Smoothly interpolate towards the target velocity
            // Using Lerp for acceleration feel
            currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, 1f - Mathf.Exp(-acceleration * deltaTime));

            // Handle jumping
            if (_jumpRequested && _timeSinceJumpRequested <= groundGraceTime)
            {
                // Calculate jump direction before applying force
                Vector3 jumpDirection = Motor.CharacterUp;
                if (Motor.GroundingStatus.GroundNormal.sqrMagnitude > 0f)
                {
                    // Optional: Jump slightly away from slope normal
                    jumpDirection = (jumpDirection + Motor.GroundingStatus.GroundNormal * 0.1f).normalized;
                }

                // Apply jump force
                Motor.ForceUnground(0.1f); // Slightly unground to ensure liftoff
                currentVelocity += jumpDirection * jumpForce - Vector3.Project(currentVelocity, Motor.CharacterUp); // Add jump vel, remove existing vertical

                _jumpConsumed = true;
                _jumpRequested = false;
            }
        }
        else // In Air
        {
            // Air control: Add horizontal acceleration
            Vector3 horizontalVelocity = Vector3.ProjectOnPlane(currentVelocity, Motor.CharacterUp);
            Vector3 airAcceleration = moveDirectionWorld * (acceleration * airControlMultiplier); // Accel based on input

            // Calculate new horizontal velocity limited by max air speed
            Vector3 newHorizontalVelocity = horizontalVelocity + airAcceleration * deltaTime;
            if (newHorizontalVelocity.magnitude > maxAirSpeed)
            {
                newHorizontalVelocity = newHorizontalVelocity.normalized * maxAirSpeed;
            }

            // Apply horizontal change and gravity
            currentVelocity = newHorizontalVelocity + Vector3.Project(currentVelocity, Motor.CharacterUp); // Keep existing vertical
            currentVelocity += gravity * deltaTime; // Apply gravity

            // Coyote time jump
            if (!_jumpConsumed && _jumpRequested && _timeSinceLastGrounded <= groundGraceTime)
            {
                 // Calculate jump direction
                 Vector3 jumpDirection = Motor.CharacterUp;
                 // Apply jump force
                 currentVelocity += jumpDirection * jumpForce - Vector3.Project(currentVelocity, Motor.CharacterUp);

                 _jumpConsumed = true;
                 _jumpRequested = false;
            }
        }

         // Reset jump request if it wasn't consumed quickly enough
         if (_timeSinceJumpRequested > groundGraceTime * 2f) // Allow slightly more time than grace period
         {
             _jumpRequested = false;
         }

        Velocity = currentVelocity; // Update stored Velocity every frame
    }

    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        // KCC usually handles rotation based on velocity or look direction.
        // If you need custom rotation logic, implement it here.
        // For FPS, usually the root object (this transform) rotates only around Y based on PlayerLook.
        // Let PlayerLook handle Y rotation of the parent, KCC handles movement.
        // So, often empty for FPS unless specific needs arise.
    }

    public void BeforeCharacterUpdate(float deltaTime)
    {
        // Called before Motor velocity/rotation updates. Good place for input processing
        // IF you weren't using the event-driven approach. Now largely handled by events.
    }

    public void AfterCharacterUpdate(float deltaTime)
    {
        // Update capsule height based on crouch state
        float targetHeight = _isCrouchHeld ? crouchedHeight : standingHeight;
        // Use SmoothDamp for smoother visual transition (optional, Lerp is also fine)
        // smoothedHeight = Mathf.SmoothDamp(smoothedHeight, targetHeight, ref _capsuleResizeVelocity, 0.1f, crouchTransitionSpeed, deltaTime);
        smoothedHeight = Mathf.Lerp(smoothedHeight, targetHeight, 1f - Mathf.Exp(-crouchTransitionSpeed * deltaTime)); // Exponential lerp is often nice

        // Prevent resizing if obstructed (KCC doesn't do this automatically for capsule height changes)
        // TODO: Add obstruction check before setting Motor capsule dimensions if needed

        float center = smoothedHeight * 0.5f;
        Motor.SetCapsuleDimensions(capsuleRadius, smoothedHeight, center);
        isCrouching = Mathf.Abs(smoothedHeight - crouchedHeight) < 0.05f; // Update debug flag

        // Update visual head height smoothly following capsule top
        // Offset slightly below the absolute top for better feel
        float targetVisualHeight = smoothedHeight - 0.1f;
        _visualHeadHeight = Mathf.Lerp(_visualHeadHeight, targetVisualHeight, 1f - Mathf.Exp(-headFollowSpeed * deltaTime));
    }

    // --- Helper Methods ---
    public Vector3 GetSmoothedHeadWorldPosition()
    {
        // Use the smoothed visual head height
        return transform.position + Vector3.up * _visualHeadHeight;
    }

    public Vector3 GetVelocity() => Velocity; // Getter remains useful

    // --- KCC Collision Callbacks ---
    public void PostGroundingUpdate(float deltaTime) { }
    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport stabilityReport) { }
    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport stabilityReport) { }
    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 position, Quaternion rotation, ref HitStabilityReport report) { }
    public void OnDiscreteCollisionDetected(Collider hitCollider) { }
    public bool IsColliderValidForCollisions(Collider coll) => !_ignored.Contains(coll); // Basic ignore list
    public void AddIgnoredCollider(Collider coll) { if (!_ignored.Contains(coll)) _ignored.Add(coll); }
    public void RemoveIgnoredCollider(Collider coll) { _ignored.Remove(coll); }
}