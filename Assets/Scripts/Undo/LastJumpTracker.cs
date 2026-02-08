
using ASCENTA.Events;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class LastJumpTracker : MonoBehaviour
{
    public readonly struct LastJumpPosition
    {
        public LastJumpPosition(Vector3 position, Quaternion rotation, Vector3 velocity, float time)
        {
            Position = position;
            Rotation = rotation;
            Velocity = velocity;
            Time = time;
        }

        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public Vector3 Velocity { get; }
        public float Time { get; }
    }

    [Header("References")]
    [SerializeField] Transform player;
    [SerializeField] Transform facingTransform;
    [SerializeField] Rigidbody rb;

    [Header("Timing")]
    [SerializeField, Min(0f)] float eventMergeWindow = 0.15f;
    [SerializeField, Min(0f)] float minTimeAfterLanding = 0.15f;

    bool hasUngroundedPose;
    bool hasPreJumpVelocity;
    bool pendingVelocityFromUngrounded;
    Vector3 pendingVelocity;
    float pendingVelocityTime;
    float lastUngroundedTime = -999f;
    float lastPreJumpTime = -999f;
    float lastGroundedTime = -999f;
    bool recordingEnabled = true;

    readonly System.Collections.Generic.Stack<LastJumpPosition> jumpStack = new System.Collections.Generic.Stack<LastJumpPosition>();
    Vector3 lastPosition;
    Quaternion lastRotation = Quaternion.identity;
    Vector3 lastVelocity;
    float lastTime;

    public bool HasJumpData => jumpStack.Count > 0;
    public int JumpCount => jumpStack.Count;

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
        if (!recordingEnabled)
        {
            return;
        }

        if (eventData.IsGrounded)
        {
            lastGroundedTime = Time.unscaledTime;
            return;
        }

        float now = Time.unscaledTime;
        if (now - lastGroundedTime < minTimeAfterLanding)
        {
            return;
        }
        if (now - lastPreJumpTime <= eventMergeWindow)
        {
            return;
        }

        RecordUngroundedPose();
        QueueUngroundedVelocity(now);
    }

    void HandlePreJumpCalculation(PreJumpCalculationEvent eventData)
    {
        if (!recordingEnabled)
        {
            return;
        }

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
        lastTime = GetCurrentScaledTime();
        hasUngroundedPose = true;
        lastUngroundedTime = Time.unscaledTime;
        TryFinalizeJump();
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
        lastTime = GetCurrentScaledTime();
        hasUngroundedPose = true;
        ApplyVelocityFromPreJump();
        TryFinalizeJump();
    }

    void ApplyVelocityFromPreJump()
    {
        lastVelocity = rb != null ? rb.linearVelocity : Vector3.zero;
        hasPreJumpVelocity = true;
        pendingVelocityFromUngrounded = false;
        TryFinalizeJump();
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
        TryFinalizeJump();
    }

    void TryFinalizeJump()
    {
        if (!hasUngroundedPose || !hasPreJumpVelocity)
        {
            return;
        }

        jumpStack.Push(new LastJumpPosition(lastPosition, lastRotation, lastVelocity, lastTime));
        hasUngroundedPose = false;
        hasPreJumpVelocity = false;
    }

    public bool TryPop(out LastJumpPosition jump)
    {
        if (jumpStack.Count > 0)
        {
            jump = jumpStack.Pop();
            return true;
        }

        jump = default;
        return false;
    }

    public void PauseRecording()
    {
        recordingEnabled = false;
        pendingVelocityFromUngrounded = false;
    }

    public void ResumeRecording()
    {
        recordingEnabled = true;
    }

    float GetCurrentScaledTime()
    {
        return TimeController.Instance != null ? TimeController.Instance.GetRealTime() : Time.time;
    }
}
