
using ASCENTA.Events;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class LastJumpTracker : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Transform player;
    [SerializeField] Transform facingTransform;
    [SerializeField] Rigidbody rb;

    [Header("Timing")]
    [SerializeField, Min(0f)] float eventMergeWindow = 0.15f;

    bool hasUngroundedPose;
    bool hasPreJumpVelocity;
    bool pendingVelocityFromUngrounded;
    Vector3 pendingVelocity;
    float pendingVelocityTime;
    float lastUngroundedTime = -999f;
    float lastPreJumpTime = -999f;
    Vector3 lastPosition;
    Quaternion lastRotation = Quaternion.identity;
    Vector3 lastVelocity;

    public bool HasJumpData => hasUngroundedPose && hasPreJumpVelocity;
    public Vector3 LastPosition => lastPosition;
    public Quaternion LastRotation => lastRotation;
    public Vector3 LastVelocity => lastVelocity;

    void Awake()
    {
        if (player == null)
        {
            MovementController movementController = FindObjectOfType<MovementController>();
            if (movementController != null)
            {
                player = movementController.transform;
            }
        }

        if (facingTransform == null)
        {
            facingTransform = player;
        }
        
        if (rb == null && player != null)
        {
            rb = player.GetComponent<Rigidbody>();
        }
    }

    void OnEnable()
    {
        EventBus.Subscribe<GroundedChangedEvent>(HandleGroundedChanged);
        EventBus.Subscribe<PreJumpCalculationEvent>(HandlePreJumpCalculation);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<GroundedChangedEvent>(HandleGroundedChanged);
        EventBus.Unsubscribe<PreJumpCalculationEvent>(HandlePreJumpCalculation);
    }

    void HandleGroundedChanged(GroundedChangedEvent eventData)
    {
        if (eventData.IsGrounded)
        {
            return;
        }

        float now = Time.unscaledTime;
        if (now - lastPreJumpTime <= eventMergeWindow)
        {
            return;
        }

        RecordUngroundedPose();
        QueueUngroundedVelocity(now);
    }

    void HandlePreJumpCalculation(PreJumpCalculationEvent eventData)
    {
        float now = Time.unscaledTime;
        lastPreJumpTime = now;

        if (now - lastUngroundedTime <= eventMergeWindow)
        {
            ApplyVelocityFromPreJump();
            return;
        }

        RecordJumpPose();
    }

    void RecordUngroundedPose()
    {
        if (player == null)
        {
            return;
        }

        Vector3 position = player.position;
        Quaternion rotation = facingTransform != null ? facingTransform.rotation : player.rotation;
        lastPosition = position;
        lastRotation = rotation;
        hasUngroundedPose = true;
        lastUngroundedTime = Time.unscaledTime;
    }

    void RecordJumpPose()
    {
        if (player == null)
        {
            return;
        }

        Vector3 position = player.position;
        Quaternion rotation = facingTransform != null ? facingTransform.rotation : player.rotation;
        lastPosition = position;
        lastRotation = rotation;
        hasUngroundedPose = true;
        ApplyVelocityFromPreJump();
    }

    void ApplyVelocityFromPreJump()
    {
        lastVelocity = rb != null ? rb.linearVelocity : Vector3.zero;
        hasPreJumpVelocity = true;
        pendingVelocityFromUngrounded = false;
    }

    void QueueUngroundedVelocity(float now)
    {
        pendingVelocity = rb != null ? rb.linearVelocity : Vector3.zero;
        pendingVelocityTime = now;
        pendingVelocityFromUngrounded = true;
        lastUngroundedTime = now;
    }

    void Update()
    {
        if (!pendingVelocityFromUngrounded)
        {
            return;
        }

        float now = Time.unscaledTime;
        if (now - pendingVelocityTime < eventMergeWindow)
        {
            return;
        }

        lastVelocity = pendingVelocity;
        hasPreJumpVelocity = true;
        pendingVelocityFromUngrounded = false;
    }
}
