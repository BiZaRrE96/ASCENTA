
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
    [SerializeField, Min(0f)] float postSnapUpOffset = 0.15f;
    [SerializeField] float snapReverseMult = 1.2f;

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

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            cachedDetectCollisions = rb.detectCollisions;
            rb.detectCollisions = false;
        }

        Quaternion? rotationTarget = rotateToTarget ? lastJumpTracker.LastRotation : (Quaternion?)null;
        Vector3 nudge = -lastJumpTracker.LastVelocity * Time.unscaledDeltaTime * snapReverseMult;
        Vector3 targetPosition = lastJumpTracker.LastPosition + nudge;
        movementController.SnapTo(targetPosition, snapTime, rotationTarget);
    }

    void HandleSnapCompleted()
    {
        if (!undoInProgress)
        {
            return;
        }

        if (postSnapUpOffset > 0f)
        {
            Vector3 up = transform.up;
            Vector3 offset = up * postSnapUpOffset;
            if (rb != null)
            {
                rb.MovePosition(rb.position + offset);
            }
            else
            {
                transform.position += offset;
            }
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
}
