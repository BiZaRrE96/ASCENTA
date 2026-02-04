using ASCENTA.Events;
using UnityEngine;
using UnityEngine.InputSystem;

public class JumpController : MonoBehaviour
{
    // helps define many jump types that might be implemented in the future
    // 2 types of time : Queuetime and Coyotetime

    // Queuetime : howmany seconds a single input will be attempted
    // Coyotetime : howmany seconds after an action becomes invalid can it still be called

    // Basic / groundJump : max 1 until landing/grounded again

    [Header("References")]
    [SerializeField] Rigidbody rb;
    [SerializeField] Groundcheck groundcheck;

    [Header("Jump")]
    [SerializeField] float jumpSpeed = 6f;
    [SerializeField] float queueTime = 0.12f;
    [SerializeField] float coyoteTime = 0.12f;

    float lastGroundedTime = -999f;
    float queuedTime = -999f;
    bool jumpQueued;
    bool jumpedSinceGrounded;
    bool shouldTryJump;

    void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        if (groundcheck == null)
        {
            groundcheck = GetComponentInChildren<Groundcheck>();
        }
    }

    void OnEnable()
    {
        if (groundcheck != null)
        {
            groundcheck.OnLanded += HandleLanded;
            groundcheck.OnUngrounded += HandleUngrounded;
        }
    }

    void OnDisable()
    {
        if (groundcheck != null)
        {
            groundcheck.OnLanded -= HandleLanded;
            groundcheck.OnUngrounded -= HandleUngrounded;
        }
    }

    void FixedUpdate()
    {
        if (groundcheck != null && groundcheck.IsGrounded)
        {
            lastGroundedTime = Time.time;
        }

        if (jumpQueued && Time.time - queuedTime > Mathf.Max(0f, queueTime))
        {
            jumpQueued = false;
            shouldTryJump = false;
        }

        if (jumpQueued)
        {
            TryJump();
        }
    }

    void OnJump(InputValue value)
    {
        if (!value.isPressed)
        {
            return;
        }

        if (!TryJump())
        {
            QueueJumpAttempt();
        }
    }

    void HandleLanded()
    {
        jumpedSinceGrounded = false;
        if (shouldTryJump)
        {
            TryJump();
        }
    }

    void HandleUngrounded()
    {
        lastGroundedTime = Time.time;
    }

    bool TryJump()
    {
        if (!CanJump())
        {
            return false;
        }

        jumpQueued = false;
        bool jumped = GroundJump();
        if (jumped)
        {
            queuedTime = -999f;
            shouldTryJump = false;
        }

        return jumped;
    }

    bool CanJump()
    {
        if (jumpedSinceGrounded)
        {
            return false;
        }

        if (groundcheck != null && groundcheck.IsGrounded)
        {
            return true;
        }

        return Time.time - lastGroundedTime <= Mathf.Max(0f, coyoteTime);
    }

    bool GroundJump()
    {
        if (rb == null)
        {
            return false;
        }

        EventBus.Publish(new PreJumpCalculationEvent());

        Vector3 up = transform.up;
        Vector3 velocity = rb.linearVelocity;
        float vertical = Vector3.Dot(velocity, up);
        if (vertical < 0f)
        {
            velocity -= up * vertical;
        }

        velocity = Vector3.ProjectOnPlane(velocity, up) + up * jumpSpeed;
        rb.linearVelocity = velocity;

        jumpedSinceGrounded = true;
        return true;
    }

    void QueueJumpAttempt()
    {
        jumpQueued = true;
        queuedTime = Time.time;
        shouldTryJump = true;
    }
}
