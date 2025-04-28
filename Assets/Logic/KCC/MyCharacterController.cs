using UnityEngine;
using KinematicCharacterController;
using System.Collections.Generic;

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

    public Vector3 Velocity { get; private set; } // ✅ NEW FIELD

    private Vector3 _moveInput;
    private Vector3 _lookDirection = Vector3.forward;
    private bool _jumpRequested;
    private bool _jumpConsumed;
    private float _timeSinceLastGrounded = Mathf.Infinity;
    private float _timeSinceJumpRequested = Mathf.Infinity;
    private bool _shouldBeCrouching;
    private bool _forceSlowWalk;
    private float _currentSpeed;
    private List<Collider> _ignored = new();

    private float _visualHeadHeight;

    private void Awake()
    {
        Motor.CharacterController = this;
        smoothedHeight = standingHeight;
        _visualHeadHeight = standingHeight;
    }

    public void SetMovementInput(Vector3 moveInput, bool run, bool slow, bool crouch, bool jump)
    {
        _moveInput = moveInput;
        _shouldBeCrouching = crouch;

        if (jump)
        {
            _jumpRequested = true;
            _timeSinceJumpRequested = 0f;
        }

        if (_shouldBeCrouching)
        {
            _currentSpeed = crouchSpeed;
        }
        else
        {
            _currentSpeed = (_forceSlowWalk || slow) ? slowSpeed : (run ? runSpeed : walkSpeed);
        }
    }

    public void SetLookDirection(Vector3 lookDirection)
    {
        if (lookDirection.sqrMagnitude > 0.01f)
            _lookDirection = lookDirection.normalized;
    }

    public void HandleInput(IPlayerInput input, Vector3 worldLookDirection)
    {
        SetLookDirection(worldLookDirection);
        Vector2 moveAxis = input.MoveAxis;
        Vector3 moveInput = new Vector3(moveAxis.x, 0f, moveAxis.y).normalized;
        SetMovementInput(moveInput, input.SprintHeld, input.SlowWalkHeld, input.CrouchHeld, input.JumpDown);
    }

    public void ForceSlowWalk(bool value) => _forceSlowWalk = value;

    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        _timeSinceLastGrounded += deltaTime;
        _timeSinceJumpRequested += deltaTime;

        Vector3 inputDir = _moveInput.normalized;
        Vector3 right = Vector3.Cross(Motor.CharacterUp, _lookDirection);
        Vector3 forward = Vector3.Cross(right, Motor.CharacterUp);
        Vector3 moveWorld = (right * inputDir.x + forward * inputDir.z).normalized;

        if (Motor.GroundingStatus.IsStableOnGround)
        {
            _timeSinceLastGrounded = 0f;
            _jumpConsumed = false;

            Vector3 targetVelocity = moveWorld * _currentSpeed;
            currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, 1 - Mathf.Exp(-acceleration * deltaTime));

            if (_jumpRequested && _timeSinceJumpRequested <= groundGraceTime)
            {
                Motor.ForceUnground();
                currentVelocity.y = jumpForce;
                _jumpConsumed = true;
                _jumpRequested = false;
            }
        }
        else
        {
            Vector3 horizontalVel = Vector3.ProjectOnPlane(currentVelocity, Motor.CharacterUp);
            Vector3 airAccel = moveWorld * (acceleration * airControlMultiplier * deltaTime);
            Vector3 newHorizontalVel = horizontalVel + airAccel;

            if (newHorizontalVel.magnitude > maxAirSpeed)
                newHorizontalVel = newHorizontalVel.normalized * maxAirSpeed;

            Vector3 velDelta = newHorizontalVel - horizontalVel;
            currentVelocity += velDelta;
            currentVelocity += gravity * deltaTime;

            if (!_jumpConsumed && _jumpRequested && _timeSinceLastGrounded <= groundGraceTime)
            {
                Motor.ForceUnground();
                currentVelocity.y = jumpForce;
                _jumpConsumed = true;
                _jumpRequested = false;
            }
        }

        Velocity = currentVelocity; // ✅ Update stored Velocity every frame
    }

    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime) { }

    public void BeforeCharacterUpdate(float deltaTime) { }

    public void AfterCharacterUpdate(float deltaTime)
    {
        float targetHeight = _shouldBeCrouching ? crouchedHeight : standingHeight;
        smoothedHeight = Mathf.Lerp(smoothedHeight, targetHeight, deltaTime * crouchTransitionSpeed);
        float center = smoothedHeight * 0.5f;
        Motor.SetCapsuleDimensions(capsuleRadius, smoothedHeight, center);
        isCrouching = Mathf.Abs(smoothedHeight - crouchedHeight) < 0.05f;

        float targetVisualHeight = smoothedHeight - 0.1f;
        _visualHeadHeight = Mathf.Lerp(_visualHeadHeight, targetVisualHeight, deltaTime * headFollowSpeed);
    }

    public Vector3 GetSmoothedHeadWorldPosition()
    {
        return transform.position + Vector3.up * _visualHeadHeight;
    }

    public Vector3 GetVelocity() => Velocity; // ✅ Getter for outside systems (Viewmodel Animator, etc.)

    public void PostGroundingUpdate(float deltaTime) { }

    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport stabilityReport) { }
    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport stabilityReport) { }
    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 position, Quaternion rotation, ref HitStabilityReport report) { }
    public void OnDiscreteCollisionDetected(Collider hitCollider) { }
    public bool IsColliderValidForCollisions(Collider coll) => !_ignored.Contains(coll);
}
