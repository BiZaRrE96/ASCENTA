using ASCENTA.Events;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class LandedSFX : EventBusListener<GroundedChangedEvent>
{
    [Header("FMOD")]
    [SerializeField] EventReference landedEvent;
    [SerializeField] string intensityParameter = "Land_Intensity";
    [SerializeField] bool attachToGameObject = true;

    [Header("Intensity")]
    [SerializeField, Min(0.1f)] float terminalVelocity = 20f;
    [SerializeField] bool useVerticalSpeed = true;

    [Header("References")]
    [SerializeField] Rigidbody rb;

    bool warnedMissingEvent;
    readonly float[] recentSpeeds = new float[20];
    int recentSpeedCount;
    int recentSpeedIndex;
    bool isAirborne;

    protected override void Awake()
    {
        base.Awake();

        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }
    }

    protected override void OnEvent(GroundedChangedEvent eventData)
    {
        if (!eventData.IsGrounded)
        {
            StartAirborneTracking();
            return;
        }

        if (landedEvent.IsNull)
        {
            if (!warnedMissingEvent)
            {
                Debug.LogWarning($"{nameof(LandedSFX)} has no landed event assigned.");
                warnedMissingEvent = true;
            }
            return;
        }

        float intensity = CalculateIntensity(eventData);
        PlayWithIntensity(intensity);
        ClearRecentSpeeds();
        isAirborne = false;
    }

    float CalculateIntensity(GroundedChangedEvent eventData)
    {
        if (terminalVelocity <= Mathf.Epsilon)
        {
            return 1f;
        }

        float speed = GetMaxRecentSpeed(eventData);

        return Mathf.Clamp01(speed / terminalVelocity);
    }

    void PlayWithIntensity(float intensity)
    {
        EventInstance instance = RuntimeManager.CreateInstance(landedEvent);
        instance.setParameterByName(intensityParameter, intensity);

        if (attachToGameObject)
        {
            RuntimeManager.AttachInstanceToGameObject(instance, transform, rb);
        }
        else
        {
            instance.set3DAttributes(RuntimeUtils.To3DAttributes(transform.position));
        }

        instance.start();
        instance.release();
    }

    void Update()
    {
        if (!isAirborne || rb == null)
        {
            return;
        }

        float speed = CalculateSpeed(rb.linearVelocity, Vector3.up);
        RecordRecentSpeed(speed);
    }

    void StartAirborneTracking()
    {
        ClearRecentSpeeds();
        isAirborne = true;
    }

    void ClearRecentSpeeds()
    {
        recentSpeedCount = 0;
        recentSpeedIndex = 0;
    }

    void RecordRecentSpeed(float speed)
    {
        recentSpeeds[recentSpeedIndex] = speed;
        recentSpeedIndex = (recentSpeedIndex + 1) % recentSpeeds.Length;
        if (recentSpeedCount < recentSpeeds.Length)
        {
            recentSpeedCount++;
        }
    }

    float GetMaxRecentSpeed(GroundedChangedEvent eventData)
    {
        if (recentSpeedCount <= 0)
        {
            Vector3 fallbackVelocity = rb != null ? rb.linearVelocity : Vector3.zero;
            Vector3 normal = eventData.GroundNormal.sqrMagnitude > Mathf.Epsilon ? eventData.GroundNormal : Vector3.up;
            return CalculateSpeed(fallbackVelocity, normal);
        }

        float maxSpeed = 0f;
        for (int i = 0; i < recentSpeedCount; i++)
        {
            if (recentSpeeds[i] > maxSpeed)
            {
                maxSpeed = recentSpeeds[i];
            }
        }

        return maxSpeed;
    }

    float CalculateSpeed(Vector3 velocity, Vector3 groundNormal)
    {
        if (useVerticalSpeed)
        {
            Vector3 normal = groundNormal.sqrMagnitude > Mathf.Epsilon ? groundNormal : Vector3.up;
            return Mathf.Abs(Vector3.Dot(velocity, -normal.normalized));
        }

        return velocity.magnitude;
    }
}
