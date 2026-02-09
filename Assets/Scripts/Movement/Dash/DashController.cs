using ASCENTA.Events;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(MovementController))]
public class DashController : MonoBehaviour
{
    [SerializeField] MovementController movementController;

    [Header("Dash Settings")]
    [SerializeField, Min(0f)] float dashStrength = 16f;
    [SerializeField, Min(0.05f), Tooltip("Maximum time between taps that still counts toward a double-tap.")] float doubleTapWindow = 0.35f;
    [SerializeField, Range(0f, 1f), Tooltip("Minimum dot product between tap directions to treat them as the same direction.")] float directionMatchThreshold = 0.75f;

    [Header("Dash Timing")]
    [SerializeField, Min(0f)] float dashDuration = 0.25f;
    [SerializeField, Min(0f)] float dashCooldown = 0.6f;

    Rigidbody rb;
    float nextDashAllowedTime = -Mathf.Infinity;
    float cooldownReadyTime = -Mathf.Infinity;
    bool cooldownActive;

    const int RequiredTapCount = 2;
    float lastTapTime = -Mathf.Infinity;
    Vector2 lastTapDirection = Vector2.zero;
    int tapCount;

    void Awake()
    {
        if (movementController == null)
        {
            movementController = GetComponent<MovementController>();
        }

        if (movementController == null)
        {
            enabled = false;
            return;
        }

        rb = movementController.GetComponent<Rigidbody>();
        if (rb == null)
        {
            enabled = false;
        }
    }

    void OnMoveTap(InputValue value)
    {
        if (movementController == null
            || movementController.CurrentMovementState == MovementState.Cutscene
            || !movementController.IsMovementInputAllowed())
        {
            return;
        }

        Vector2 rawInput = value.Get<Vector2>();
        if (rawInput.sqrMagnitude < 0.001f)
        {
            return;
        }

        Vector2 normalizedDirection = rawInput.normalized;
        float currentTime = Time.time;
        bool withinWindow = tapCount > 0 && currentTime - lastTapTime <= doubleTapWindow;
        bool directionMatches = withinWindow && Vector2.Dot(normalizedDirection, lastTapDirection) >= directionMatchThreshold;

        tapCount = directionMatches ? tapCount + 1 : 1;
        lastTapDirection = normalizedDirection;
        lastTapTime = currentTime;

        if (tapCount >= RequiredTapCount)
        {
            if (ExecuteDash(normalizedDirection))
            {
                tapCount = 0;
                lastTapTime = -Mathf.Infinity;
            }
            else
            {
                tapCount = 1;
            }
        }
    }

    void Update()
    {
        if (!cooldownActive || Time.time < cooldownReadyTime)
        {
            return;
        }

        cooldownActive = false;
        EventBus.Publish(new OnDashCooldownFinishedEvent(cooldownReadyTime, dashCooldown));
    }

    bool ExecuteDash(Vector2 inputDirection)
    {
        if (movementController == null
            || rb == null
            || movementController.CurrentMovementState == MovementState.Cutscene
            || Time.time < nextDashAllowedTime)
        {
            return false;
        }

        if (!movementController.IsMovementInputAllowed())
        {
            return false;
        }

        Vector3 worldDirection = movementController.ConvertMoveInputToWorld(inputDirection);
        if (worldDirection.sqrMagnitude < 0.01f)
        {
            return false;
        }

        Vector3 groundNormal = movementController.GroundNormal;
        Vector3 projected = Vector3.ProjectOnPlane(worldDirection, groundNormal);
        Vector3 dashDirection = projected.sqrMagnitude >= 0.01f ? projected.normalized : worldDirection.normalized;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.AddForce(dashDirection * dashStrength, ForceMode.VelocityChange);

        movementController.TemporarilySetMovementState(MovementState.Dashing, dashDuration);
        nextDashAllowedTime = Time.time + dashCooldown;
        cooldownReadyTime = nextDashAllowedTime;
        cooldownActive = dashCooldown > 0f;
        EventBus.Publish(new OnDashEvent(dashDirection, dashStrength, dashDuration));

        if (!cooldownActive)
        {
            EventBus.Publish(new OnDashCooldownFinishedEvent(cooldownReadyTime, dashCooldown));
        }
        return true;
    }
}
