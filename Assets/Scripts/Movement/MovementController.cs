using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class MovementController : MonoBehaviour
{
    public enum MovementState
    {
        Default,
        Airborne
    }

    [SerializeField] Rigidbody rb;
    [SerializeField] Transform look;
    [SerializeField] Transform forward;
    [SerializeField] Groundcheck groundcheck;

    //Configs
    [SerializeField] float maxLookYDeg = 179f;
    [SerializeField] float lookSensitivity = 0.15f;

    [SerializeField] bool lockForwardToLook;

    [SerializeField] float accel = 12f;
    [SerializeField] float maxAccel = 20f;
    [SerializeField] float ungroundedMult = 0.6f;

    [SerializeField] float deccelBonus = 0.3f;
    [SerializeField] float intentDeadzone = 0.01f;

    [Header("State")]
    [SerializeField] float airborneUngroundedMult = 0.1f;

    [Header("Snap")]
    [SerializeField] float snapMoveDuration = 0.25f;
    [SerializeField] float snapMaxDuration = 2f;
    [SerializeField] float snapCompletionDistance = 0.05f;
    [SerializeField] float snapCompletionAngle = 2f;

    public event Action OnSnapCompleted;

    bool externalInputAllowed = true;
    bool snapInputLocked;
    bool pendingSnapShouldRestoreInput;
    Coroutine snapCoroutine;
    Coroutine temporaryMovementStateCoroutine;

    MovementState movementState = MovementState.Default;
    float movementStateLockUntil;

    private Vector3 moveIntent;
    private Vector2 moveInput;

    float lookPitch;
    float lookYaw;

    void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        if (look == null)
        {
            look = transform;
        }

        if (forward == null)
        {
            forward = transform;
        }

        if (groundcheck == null)
        {
            groundcheck = GetComponentInChildren<Groundcheck>();
        }

        if (look != null)
        {
            Vector3 initialEuler = look.localEulerAngles;
            lookPitch = NormalizeSignedAngle(initialEuler.x);
            lookYaw = NormalizeSignedAngle(initialEuler.y);
            look.localEulerAngles = new Vector3(lookPitch, lookYaw, 0f);
        }
    }

    void OnLook(InputValue value)
    {
        if (!IsPlayerInputAllowed())
        {
            return;
        }

        Vector2 input = value.Get<Vector2>();
        if (input.sqrMagnitude <= 0f || look == null)
        {
            return;
        }

        float yaw = input.x * lookSensitivity;
        float pitch = -input.y * lookSensitivity;

        rotateLook(pitch, yaw);
    }


    void rotateLook(float pitchDelta, float yawDelta)
    {
        if (look == null)
        {
            return;
        }

        float halfLookLimit = maxLookYDeg * 0.5f;
        lookPitch = Mathf.Clamp(lookPitch + pitchDelta, -halfLookLimit, halfLookLimit);
        lookYaw = NormalizeSignedAngle(lookYaw + yawDelta);
        look.localRotation = Quaternion.Euler(lookPitch, lookYaw, 0f);

        if (lockForwardToLook)
        {
            SyncForwardToLook();
        }
    }

    void SyncForwardToLook()
    {
        if (forward == null || look == null)
        {
            return;
        }

        Vector3 forwardEuler = forward.eulerAngles;
        forwardEuler.y = look.eulerAngles.y;
        forward.eulerAngles = forwardEuler;
    }

    void OnMove(InputValue value)
    {
        if (!IsPlayerInputAllowed())
        {
            moveInput = Vector2.zero;
            UpdateMoveIntent();
            return;
        }

        moveInput = value.Get<Vector2>();

        UpdateMoveIntent();
    }

    void Update()
    {
        UpdateMoveIntent();
    }

    void FixedUpdate()
    {
        if (rb == null)
        {
            return;
        }

        UpdateAutoMovementState();

        if (moveIntent.sqrMagnitude <= 0f)
        {
            return;
        }

        Vector3 velocity = rb.linearVelocity;
        float currentSpeed = Vector3.Dot(velocity, moveIntent);
        float maxAllowedAccel = (maxAccel - currentSpeed) / Time.fixedDeltaTime;
        float appliedAccel = Mathf.Clamp(maxAllowedAccel, 0f, accel);
        if (groundcheck != null && !groundcheck.IsGrounded)
        {
            float mult = movementState == MovementState.Airborne ? airborneUngroundedMult : ungroundedMult;
            appliedAccel *= mult;
        }

        Vector3 adjustedIntent = movementState == MovementState.Airborne ? moveIntent : ApplyDecelAdjust(moveIntent, velocity);

        rb.AddForce(adjustedIntent * appliedAccel, ForceMode.Acceleration);
    }

    void UpdateMoveIntent()
    {
        Vector3 forwardDir = forward != null ? forward.forward : transform.forward;
        Vector3 rightDir = forward != null ? forward.right : transform.right;

        Vector3 desired = forwardDir * moveInput.y + rightDir * moveInput.x;
        if (desired.sqrMagnitude > 1f)
        {
            desired.Normalize();
        }

        moveIntent = desired;
    }

    Vector3 ApplyDecelAdjust(Vector3 intent, Vector3 velocity)
    {
        Vector3 forwardDir = forward != null ? forward.forward : transform.forward;
        Vector3 rightDir = forward != null ? forward.right : transform.right;

        float intentForward = Vector3.Dot(intent, forwardDir);
        float intentRight = Vector3.Dot(intent, rightDir);
        float velForward = Vector3.Dot(velocity, forwardDir);
        float velRight = Vector3.Dot(velocity, rightDir);

        bool forwardIdle = IsNearZero(intentForward);
        bool rightIdle = IsNearZero(intentRight);

        if (forwardIdle && Mathf.Abs(velForward) > 0f)
        {
            intentForward = -Mathf.Sign(velForward) * deccelBonus;
        }
        else if (intentForward * velForward < 0f)
        {
            intentForward *= 1f + deccelBonus;
        }

        if (rightIdle && Mathf.Abs(velRight) > 0f)
        {
            intentRight = -Mathf.Sign(velRight) * deccelBonus;
        }
        else if (intentRight * velRight < 0f)
        {
            intentRight *= 1f + deccelBonus;
        }

        return forwardDir * intentForward + rightDir * intentRight;
    }

    static float NormalizeSignedAngle(float angle)
    {
        if (angle > 180f)
        {
            angle -= 360f;
        }

        return angle;
    }

    bool IsNearZero(float value)
    {
        return Mathf.Abs(value) <= intentDeadzone;
    }

    void UpdateAutoMovementState()
    {
        if (groundcheck == null)
        {
            return;
        }

        if (!IsMovementStateLocked() && groundcheck.IsGrounded && movementState == MovementState.Airborne)
        {
            movementState = MovementState.Default;
        }
    }

    bool IsMovementStateLocked()
    {
        return Time.time < movementStateLockUntil;
    }

    public MovementState CurrentMovementState => movementState;

    public void SetPlayerInputAllowed(bool allowed)
    {
        externalInputAllowed = allowed;
    }

    public void SetMovementState(MovementState targetState, float stateLockIn)
    {
        if (IsMovementStateLocked() && movementState != targetState)
        {
            return;
        }

        movementState = targetState;
        float clampedDuration = Mathf.Max(0f, stateLockIn);
        movementStateLockUntil = clampedDuration > 0f ? Time.time + clampedDuration : 0f;
    }

    public void TemporarilySetMovementState(MovementState targetState, float duration)
    {
        if (duration <= 0f)
        {
            SetMovementState(targetState, duration);
            return;
        }

        if (temporaryMovementStateCoroutine != null)
        {
            StopCoroutine(temporaryMovementStateCoroutine);
            temporaryMovementStateCoroutine = null;
        }

        temporaryMovementStateCoroutine = StartCoroutine(TemporaryMovementStateRoutine(targetState, duration));
    }

    IEnumerator TemporaryMovementStateRoutine(MovementState targetState, float duration)
    {
        MovementState previousState = movementState;
        float previousLockUntil = movementStateLockUntil;

        SetMovementState(targetState, duration);

        yield return new WaitForSeconds(duration);

        if (movementState == targetState)
        {
            movementState = previousState;
            movementStateLockUntil = previousLockUntil;
        }

        temporaryMovementStateCoroutine = null;
    }

    public void SnapTo(Transform target, bool rotate, float rotationSpeed)
    {
        if (target == null)
        {
            return;
        }

        bool shouldRestoreInput = externalInputAllowed;
        if (snapCoroutine != null)
        {
            StopCoroutine(snapCoroutine);
            ResetSnapState(true);
        }

        if (shouldRestoreInput)
        {
            snapInputLocked = true;
        }

        pendingSnapShouldRestoreInput = shouldRestoreInput;
        ClearMovementIntent();

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        snapCoroutine = StartCoroutine(SnapRoutine(target, rotate, rotationSpeed));
    }

    IEnumerator SnapRoutine(Transform target, bool rotate, float rotationSpeed)
    {
        float duration = Mathf.Max(0.001f, snapMoveDuration);
        duration = Mathf.Min(duration, snapMaxDuration);
        float moveSpeed = Vector3.Distance(GetCurrentPosition(), target.position) / duration;
        float elapsed = 0f;

        while (elapsed < snapMaxDuration)
        {
            Vector3 currentPosition = GetCurrentPosition();
            Vector3 targetPosition = target.position;
            bool positionReached = Vector3.SqrMagnitude(currentPosition - targetPosition) <= snapCompletionDistance * snapCompletionDistance;
            bool rotationReached = !rotate || Quaternion.Angle(transform.rotation, target.rotation) <= snapCompletionAngle;

            if (positionReached && rotationReached)
            {
                break;
            }

            if (moveSpeed > 0f)
            {
                Vector3 nextPosition = Vector3.MoveTowards(currentPosition, targetPosition, moveSpeed * Time.fixedDeltaTime);
                MoveToPosition(nextPosition);
            }
            else
            {
                MoveToPosition(targetPosition);
            }

            if (rotate)
            {
                ApplyRotationTowards(target.rotation, rotationSpeed);
            }

            yield return new WaitForFixedUpdate();
            elapsed += Time.fixedDeltaTime;
        }

        MoveToPosition(target.position);
        if (rotate)
        {
            MoveToRotation(target.rotation);
        }

        CompleteSnap();
    }

    Vector3 GetCurrentPosition()
    {
        return rb != null ? rb.position : transform.position;
    }

    void MoveToPosition(Vector3 position)
    {
        if (rb != null)
        {
            rb.MovePosition(position);
        }
        else
        {
            transform.position = position;
        }
    }

    void MoveToRotation(Quaternion rotation)
    {
        if (rb != null)
        {
            rb.MoveRotation(rotation);
        }
        else
        {
            transform.rotation = rotation;
        }
    }

    void ApplyRotationTowards(Quaternion targetRotation, float rotationSpeed)
    {
        if (rotationSpeed <= 0f)
        {
            MoveToRotation(targetRotation);
            return;
        }

        Quaternion nextRotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        MoveToRotation(nextRotation);
    }

    void ClearMovementIntent()
    {
        moveInput = Vector2.zero;
        moveIntent = Vector3.zero;
    }

    void ResetSnapState(bool restoreInput)
    {
        if (restoreInput && pendingSnapShouldRestoreInput)
        {
            snapInputLocked = false;
        }

        pendingSnapShouldRestoreInput = false;
        snapCoroutine = null;
    }

    void CompleteSnap()
    {
        ResetSnapState(true);
        OnSnapCompleted?.Invoke();
    }

    bool IsPlayerInputAllowed()
    {
        return externalInputAllowed && !snapInputLocked;
    }
}
