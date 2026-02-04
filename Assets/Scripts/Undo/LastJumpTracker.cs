
using ASCENTA.Events;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class LastJumpTracker : EventBusListener<PreJumpCalculationEvent>
{
    [Header("References")]
    [SerializeField] Transform player;
    [SerializeField] Transform facingTransform;
    [SerializeField] Rigidbody rb;

    bool hasJumpData;
    Vector3 lastPosition;
    Quaternion lastRotation = Quaternion.identity;
    Vector3 lastVelocity;

    public bool HasJumpData => hasJumpData;
    public Vector3 LastPosition => lastPosition;
    public Quaternion LastRotation => lastRotation;
    public Vector3 LastVelocity => lastVelocity;

    protected override void Awake()
    {
        base.Awake();

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

    protected override void OnEvent(PreJumpCalculationEvent eventData)
    {
        RecordJumpPose();
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
        lastVelocity = rb != null ? rb.linearVelocity : Vector3.zero;
        hasJumpData = true;
    }
}
