
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(MovementController))]
public sealed class UndoProcess : MonoBehaviour
{
    [Header("References")]
    [SerializeField] MovementController movementController;
    [SerializeField] Rigidbody rb;
    [SerializeField] LastJumpTracker lastJumpTracker;

    [Header("Snap")]
    [SerializeField] bool rotateToTarget = true;
    [SerializeField, Min(0f)] float snapTime = 0.25f;
    [SerializeField, Min(0f)] float postSnapUpOffset = 0.05f;
    [SerializeField] float snapReverseMult = 1.2f;
    [SerializeField] LayerMask groundMask = ~0;
    [SerializeField, Min(0f)] float groundCheckDistance = 1.2f;
    [SerializeField, Range(1, 3)] int maxNudgeAttempts = 3;

    bool undoInProgress;
    bool cachedDetectCollisions = true;

    void Awake()
    {
        if (movementController == null)
        {
            movementController = GetComponent<MovementController>();
        }

        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        if (lastJumpTracker == null)
        {
            lastJumpTracker = FindObjectOfType<LastJumpTracker>();
        }
    }

    void OnEnable()
    {
        if (movementController != null)
        {
            movementController.OnSnapCompleted += HandleSnapCompleted;
        }
    }

    void OnDisable()
    {
        if (movementController != null)
        {
            movementController.OnSnapCompleted -= HandleSnapCompleted;
        }

        if (undoInProgress)
        {
            RestoreState();
            undoInProgress = false;
        }
    }

    void OnReload(InputValue value)
    {
        if (!value.isPressed)
        {
            return;
        }

        BeginUndo();
    }

    void BeginUndo()
    {
        if (undoInProgress || movementController == null || lastJumpTracker == null)
        {
            return;
        }

        if (!lastJumpTracker.HasJumpData)
        {
            return;
        }

        undoInProgress = true;
        movementController.SetPlayerInputAllowed(false);
        lastJumpTracker.PauseRecording();

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            cachedDetectCollisions = rb.detectCollisions;
            rb.detectCollisions = false;
        }

        if (!lastJumpTracker.TryPop(out LastJumpTracker.LastJumpPosition jump))
        {
            RestoreState();
            undoInProgress = false;
            return;
        }

        Quaternion? rotationTarget = rotateToTarget ? jump.Rotation : (Quaternion?)null;
        Vector3 nudge = -jump.Velocity * Time.unscaledDeltaTime * snapReverseMult;
        Vector3 targetPosition = jump.Position + nudge;
        targetPosition = ResolveTargetWithGroundCheck(targetPosition, nudge, maxNudgeAttempts);

        if (postSnapUpOffset > 0f)
        {
            Vector3 up = transform.up;
            Vector3 offset = up * postSnapUpOffset;
            targetPosition += offset;
        }
        movementController.SnapTo(targetPosition, snapTime, rotationTarget);
    }

    void HandleSnapCompleted()
    {
        if (!undoInProgress)
        {
            return;
        }

        RestoreState();
        undoInProgress = false;
    }

    void RestoreState()
    {
        if (rb != null)
        {
            rb.detectCollisions = cachedDetectCollisions;
        }

        if (movementController != null)
        {
            movementController.SetPlayerInputAllowed(true);
        }

        if (lastJumpTracker != null)
        {
            lastJumpTracker.ResumeRecording();
        }
    }

    void FixedUpdate()
    {
        if (!undoInProgress || rb == null)
        {
            return;
        }

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    Vector3 ResolveTargetWithGroundCheck(Vector3 targetPosition, Vector3 nudge, int attempts)
    {
        int maxAttempts = Mathf.Clamp(attempts, 1, 3);
        Vector3 candidate = targetPosition;
        for (int i = 0; i < maxAttempts; i++)
        {
            if (HasGroundBelow(candidate))
            {
                return candidate;
            }

            candidate += nudge;
        }

        return candidate;
    }

    bool HasGroundBelow(Vector3 position)
    {
        float distance = Mathf.Max(0f, groundCheckDistance);
        return Physics.Raycast(position, Vector3.down, distance, groundMask, QueryTriggerInteraction.Ignore);
    }
}
