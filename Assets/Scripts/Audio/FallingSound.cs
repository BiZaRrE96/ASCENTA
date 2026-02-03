using ASCENTA.Events;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class FallingSound : EventBusListener<GroundedChangedEvent>
{
    [Header("FMOD")]
    [SerializeField] EventReference fallingEvent;
    [SerializeField] string intensityParameter = "Intensity";
    [SerializeField] bool attachToGameObject = true;

    [Header("Timing")]
    [SerializeField, Min(0f)] float startDelay = 1f;
    [SerializeField, Min(0.1f)] float timeToMaxIntensity = 2f;
    [SerializeField] AnimationCurve timeToMaxIntensityCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("Velocity")]
    [SerializeField, Min(0.1f)] float downwardTerminalVelocity = 20f;
    [SerializeField, Min(0.1f)] float upwardTerminalVelocity = 10f;

    [SerializeField] Rigidbody rb;

    EventInstance fallingInstance;
    bool isGrounded = true;
    float ungroundedTime;
    bool warnedMissingEvent;
    bool instanceValid;

    protected override void Awake()
    {
        base.Awake();

        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }
    }

    void Update()
    {
        if (isGrounded)
        {
            return;
        }

        ungroundedTime += Time.deltaTime;

        if (!instanceValid && ungroundedTime >= startDelay)
        {
            TryStartInstance();
        }

        if (instanceValid)
        {
            UpdateIntensity();
        }
    }

    protected override void OnEvent(GroundedChangedEvent eventData)
    {
        isGrounded = eventData.IsGrounded;

        if (isGrounded)
        {
            ungroundedTime = 0f;
            StopInstance();
        }
    }

    void TryStartInstance()
    {
        if (fallingEvent.IsNull)
        {
            if (!warnedMissingEvent)
            {
                Debug.LogWarning($"{nameof(FallingSound)} has no falling event assigned.");
                warnedMissingEvent = true;
            }
            return;
        }

        fallingInstance = RuntimeManager.CreateInstance(fallingEvent);

        if (attachToGameObject)
        {
            RuntimeManager.AttachInstanceToGameObject(fallingInstance, transform, rb);
        }

        fallingInstance.start();
        instanceValid = true;
    }

    void UpdateIntensity()
    {
        float verticalVelocity = rb != null ? rb.linearVelocity.y : 0f;
        float speed = Mathf.Abs(verticalVelocity);
        float terminal = verticalVelocity >= 0f ? upwardTerminalVelocity : downwardTerminalVelocity;
        float velocityFactor = Mathf.InverseLerp(0f, terminal, speed);
        float normalizedTime = Mathf.Clamp01(ungroundedTime / timeToMaxIntensity);
        float intensityCap = Mathf.Clamp01(timeToMaxIntensityCurve.Evaluate(normalizedTime));
        float intensity = Mathf.Clamp01(Mathf.Min(velocityFactor, intensityCap));

        fallingInstance.setParameterByName(intensityParameter, intensity);
    }

    void StopInstance()
    {
        if (!instanceValid)
        {
            return;
        }

        fallingInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        fallingInstance.release();
        instanceValid = false;
    }

    new void OnDisable()
    {
        StopInstance();
        base.OnDisable();
    }

    new void OnDestroy()
    {
        StopInstance();
        base.OnDestroy();
    }
}
