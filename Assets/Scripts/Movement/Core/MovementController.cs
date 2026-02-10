using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class MovementController : MonoBehaviour
{
    [SerializeField] Rigidbody rb;
    [SerializeField] Transform look;
    [SerializeField] Transform forward;
    [SerializeField] Groundcheck groundcheck;
    [SerializeField] MovementStateController movementStateController;

    //Configs
    [SerializeField] float maxLookYDeg = 179f;
    [SerializeField] float lookSensitivity = 0.15f;

    [SerializeField] bool lockForwardToLook;
    [SerializeField] bool moveWithPlatformDelta = true;

    [Header("Movement Properties")]
    [SerializeField] float baseMaxSpeed = 6f;
    [SerializeField] float baseAcceleration = 12f;
    [SerializeField] float baseTurnAcceleration = 18f;
    [SerializeField] float baseDamping = 10f;

    [Header("Traction")]
    [SerializeField, Tooltip("Surface traction multiplies acceleration and turning responsiveness.")]
    float defaultTraction = 1f;
    [SerializeField, Tooltip("Surface damping multiplies braking authority when no input is supplied.")]
    float defaultSurfaceDamping = 1f;
    [SerializeField, Min(0f)]
    float tractionLerpSpeed = 10f;
    [SerializeField, Min(0f)]
    float dampingLerpSpeed = 10f;
    [SerializeField, Range(0f, 1f)]
    float airControlMultiplier = 0.3f;

    [Header("Snap")]
    [SerializeField] float snapMoveDuration = 0.25f;
    [SerializeField] float snapMaxDuration = 2f;
    [SerializeField] float snapCompletionDistance = 0.05f;
    [SerializeField] float snapCompletionAngle = 2f;
    [SerializeField] AnimationCurve snapCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Header("Dash State")]
    [SerializeField, Tooltip("If enabled, gravity is temporarily disabled while MovementState is Dashing.")]
    bool ignoreGravityDuringDash = true;

    public event Action OnSnapCompleted;

    bool externalInputAllowed = true;
    bool snapInputLocked;
    bool pendingSnapShouldRestoreInput;
    Coroutine snapCoroutine;

    float currentTraction;
    float currentSurfaceDamping;
    bool defaultUseGravity = true;

    public float CurrentTraction => currentTraction;
    public float CurrentSurfaceDamping => currentSurfaceDamping;
    public bool IsGrounded => groundcheck != null && groundcheck.IsGrounded;
    public Vector3 GroundNormal => groundcheck != null ? groundcheck.GroundNormal : Vector3.up;

    private Vector3 moveIntent;
    private Vector2 moveInput;

    public float Velocity => rb.linearVelocity.magnitude;

    float lookPitch;
    float lookYaw;
    int fixedUpdateStep;
    Vector3 pendingVelocityDelta;
    bool hasPendingVelocityDelta;

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
        if (movementStateController == null)
        {
            movementStateController = GetComponent<MovementStateController>();
        }

        if (look != null)
        {
            Vector3 initialEuler = look.localEulerAngles;
            lookPitch = NormalizeSignedAngle(initialEuler.x);
            lookYaw = NormalizeSignedAngle(initialEuler.y);
            look.localEulerAngles = new Vector3(lookPitch, lookYaw, 0f);
        }

        currentTraction = defaultTraction;
        currentSurfaceDamping = defaultSurfaceDamping;

        if (rb != null)
        {
            defaultUseGravity = rb.useGravity;
        }
    }

    void OnDisable()
    {
    }

    void OnEnable()
    {
    }

    void OnLook(InputValue value)
    {
        if (!IsLookInputAllowed())
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
        if (!IsMovementInputAllowed())
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

        fixedUpdateStep++;

        float deltaTime = Time.fixedDeltaTime;
        bool grounded = groundcheck != null && groundcheck.IsGrounded;
        Vector3 planeNormal = grounded ? groundcheck.GroundNormal : Vector3.up;
        Collider groundCollider = grounded ? groundcheck.GroundCollider : null;
        MovingPlatform movingPlatform = groundcheck != null ? groundcheck.CurrentMovingPlatform : null;
        bool groundedOnMovingPlatform = groundcheck != null && groundcheck.IsGroundedOnMovingPlatform;
        UpdateGravityState();

        Vector3 platformDelta = Vector3.zero;
        if (moveWithPlatformDelta && groundedOnMovingPlatform && movingPlatform != null)
        {
            Vector3? platformDeltaResult = movingPlatform.FixedUpdateByPlayer();
            if (platformDeltaResult.HasValue)
            {
                platformDelta = platformDeltaResult.Value;
            }
        }

        Vector3 velocityChange = Vector3.zero;
        float targetTraction = defaultTraction;
        float targetDamping = defaultSurfaceDamping;
        if (grounded && groundCollider != null)
        {
            GroundSurface surface = groundCollider.GetComponent<GroundSurface>() ?? groundCollider.GetComponentInParent<GroundSurface>();
            if (surface != null)
            {
                targetTraction = surface.traction;
                targetDamping = surface.damping;
            }
        }

        currentTraction = Mathf.MoveTowards(currentTraction, targetTraction, tractionLerpSpeed * deltaTime);
        currentSurfaceDamping = Mathf.MoveTowards(currentSurfaceDamping, targetDamping, dampingLerpSpeed * deltaTime);

        Vector3 platformVelocity = Vector3.zero;
        if (platformDelta.sqrMagnitude > Mathf.Epsilon)
        {
            float fixedDt = Mathf.Max(Time.fixedDeltaTime, Mathf.Epsilon);
            platformVelocity = platformDelta / fixedDt;
        }

        Vector3 lateralVelocity = Vector3.ProjectOnPlane(rb.linearVelocity, planeNormal);
        Vector3 lateralVelocityWithoutPlatform = lateralVelocity - Vector3.ProjectOnPlane(platformVelocity, planeNormal);

        Vector3 inputDirection = Vector3.zero;
        if (moveIntent.sqrMagnitude > Mathf.Epsilon)
        {
            inputDirection = Vector3.ProjectOnPlane(moveIntent, planeNormal);
            if (inputDirection.sqrMagnitude > Mathf.Epsilon)
            {
                inputDirection = inputDirection.normalized;
            }
        }

        bool hasInput = inputDirection.sqrMagnitude > Mathf.Epsilon;

        if (hasInput)
        {
            // Traction controls how quickly we accelerate and redirect toward the intended velocity.
            Vector3 desiredVelocity = inputDirection * baseMaxSpeed;
            Vector3 deltaVelocity = desiredVelocity - lateralVelocityWithoutPlatform;

            float tractionModifier = currentTraction;
            if (!grounded)
            {
                tractionModifier *= airControlMultiplier;
            }

            float accelLimit = baseAcceleration * tractionModifier;
            float turnLimit = baseTurnAcceleration * tractionModifier;

            Vector3 forwardComponent = inputDirection.sqrMagnitude > Mathf.Epsilon
                ? Vector3.Project(deltaVelocity, inputDirection)
                : deltaVelocity;
            Vector3 orthogonalComponent = deltaVelocity - forwardComponent;

            velocityChange += Vector3.ClampMagnitude(forwardComponent, accelLimit * deltaTime);
            velocityChange += Vector3.ClampMagnitude(orthogonalComponent, turnLimit * deltaTime);
        }
        else if (grounded)
        {
            // Damping/braking slows the player when no input is provided.
            float brakingLimit = baseDamping * currentSurfaceDamping;
            Vector3 braking = -Vector3.ClampMagnitude(lateralVelocityWithoutPlatform, brakingLimit * deltaTime);
            velocityChange += braking;
        }

        if (velocityChange.sqrMagnitude > Mathf.Epsilon)
        {
            QueueVelocityDelta(velocityChange);
        }

        if (platformDelta.sqrMagnitude > Mathf.Epsilon)
        {
            MoveWithPlatformDelta(platformDelta);
        }

        ApplyQueuedVelocityDelta();

    }

    void UpdateMoveIntent()
    {
        if (!IsMovementInputAllowed())
        {
            moveInput = Vector2.zero;
            moveIntent = Vector3.zero;
            return;
        }

        moveIntent = ConvertMoveInputToWorld(moveInput);
    }

    public Vector3 ConvertMoveInputToWorld(Vector2 input)
    {
        Vector3 forwardDir = forward != null ? forward.forward : transform.forward;
        Vector3 rightDir = forward != null ? forward.right : transform.right;

        Vector3 desired = forwardDir * input.y + rightDir * input.x;
        if (desired.sqrMagnitude > 1f)
        {
            desired.Normalize();
        }

        return desired;
    }

    void UpdateGravityState()
    {
        if (rb == null)
        {
            return;
        }

        bool shouldIgnoreGravity = ignoreGravityDuringDash && CurrentMovementState == MovementState.Dashing;
        if (shouldIgnoreGravity)
        {
            if (rb.useGravity)
            {
                rb.useGravity = false;
            }
        }
        else if (rb.useGravity != defaultUseGravity)
        {
            rb.useGravity = defaultUseGravity;
        }
    }

    static float NormalizeSignedAngle(float angle)
    {
        if (angle > 180f)
        {
            angle -= 360f;
        }

        return angle;
    }

    public MovementState CurrentMovementState => movementStateController != null
        ? movementStateController.CurrentState
        : MovementState.Default;

    public float LookSensitivity => lookSensitivity;

    public void SetLookSensitivity(float sensitivity)
    {
        lookSensitivity = sensitivity;
    }

    public void SetPlayerInputAllowed(bool allowed)
    {
        externalInputAllowed = allowed;
    }

    public void SetMovementState(MovementState targetState, float stateLockIn)
    {
        if (movementStateController == null)
        {
            return;
        }

        movementStateController.SetState(targetState, stateLockIn);
    }

    public void TemporarilySetMovementState(MovementState targetState, float duration)
    {
        if (movementStateController == null)
        {
            return;
        }

        movementStateController.TemporarilySetState(targetState, duration);
    }

    public void SnapTo(Vector3 targetPosition, float snapTime, Quaternion? rotationTarget = null)
    {
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

        snapCoroutine = StartCoroutine(SnapRoutine(targetPosition, snapTime, rotationTarget));
    }

    public void SnapTo(Transform target, float snapTime, bool shouldRotate)
    {
        if (target == null)
        {
            return;
        }

        Quaternion? rotationTarget = shouldRotate ? target.rotation : (Quaternion?)null;
        SnapTo(target.position, snapTime, rotationTarget);
    }

    IEnumerator SnapRoutine(Vector3 targetPosition, float snapTime, Quaternion? rotationTarget)
    {
        float duration = Mathf.Max(0.001f, snapTime > 0f ? snapTime : snapMoveDuration);
        duration = Mathf.Min(duration, snapMaxDuration);
        float elapsed = 0f;
        Vector3 startPosition = GetCurrentPosition();
        Quaternion startLookRotation = GetCurrentLookRotation();

        while (elapsed < duration)
        {
            Vector3 currentPosition = GetCurrentPosition();
            bool positionReached = Vector3.SqrMagnitude(currentPosition - targetPosition) <= snapCompletionDistance * snapCompletionDistance;
            bool rotationReached = !rotationTarget.HasValue || Quaternion.Angle(transform.rotation, rotationTarget.Value) <= snapCompletionAngle;

            if (positionReached && rotationReached)
            {
                break;
            }

            float normalizedTime = Mathf.Clamp01(elapsed / duration);
            float curveTime = snapCurve != null ? snapCurve.Evaluate(normalizedTime) : normalizedTime;
            Vector3 nextPosition = Vector3.LerpUnclamped(startPosition, targetPosition, curveTime);
            MoveToPosition(nextPosition);

            if (rotationTarget.HasValue)
            {
                Quaternion nextRotation = Quaternion.SlerpUnclamped(startLookRotation, rotationTarget.Value, curveTime);
                ApplyLookRotation(nextRotation);
            }

            yield return new WaitForFixedUpdate();
            elapsed += Time.fixedDeltaTime;
        }

        MoveToPosition(targetPosition);
        if (rotationTarget.HasValue)
        {
            ApplyLookRotation(rotationTarget.Value);
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

    Quaternion GetCurrentLookRotation()
    {
        if (look != null)
        {
            return look.rotation;
        }

        return transform.rotation;
    }

    void ApplyLookRotation(Quaternion worldTargetRotation)
    {
        if (look == null)
        {
            return;
        }

        Quaternion localTarget = look.parent != null
            ? Quaternion.Inverse(look.parent.rotation) * worldTargetRotation
            : worldTargetRotation;

        Vector3 targetEuler = localTarget.eulerAngles;
        float targetPitch = NormalizeSignedAngle(targetEuler.x);
        float targetYaw = NormalizeSignedAngle(targetEuler.y);
        float pitchDelta = targetPitch - lookPitch;
        float yawDelta = targetYaw - lookYaw;
        rotateLook(pitchDelta, yawDelta);
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

    public bool IsMovementInputAllowed()
    {
        return externalInputAllowed && !snapInputLocked && CurrentMovementState != MovementState.Cutscene;
    }

    bool IsLookInputAllowed()
    {
        return externalInputAllowed && !snapInputLocked;
    }

    public void MoveWithPlatformDelta(Vector3 movementDelta)
    {
        if (rb == null || movementDelta.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        QueueVelocityDelta(movementDelta);
    }

    void QueueVelocityDelta(Vector3 delta)
    {
        if (delta.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        pendingVelocityDelta += delta;
        hasPendingVelocityDelta = true;
    }

    void ApplyQueuedVelocityDelta()
    {
        if (!hasPendingVelocityDelta || rb == null)
        {
            return;
        }

        Vector3 delta = pendingVelocityDelta;
        pendingVelocityDelta = Vector3.zero;
        hasPendingVelocityDelta = false;
        rb.AddForce(delta, ForceMode.VelocityChange);
    }

}
