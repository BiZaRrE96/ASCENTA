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
    }

    float CalculateIntensity(GroundedChangedEvent eventData)
    {
        if (terminalVelocity <= Mathf.Epsilon)
        {
            return 1f;
        }

        Vector3 velocity = rb != null ? rb.linearVelocity : Vector3.zero;
        float speed;

        if (useVerticalSpeed)
        {
            Vector3 normal = eventData.GroundNormal.sqrMagnitude > Mathf.Epsilon ? eventData.GroundNormal : Vector3.up;
            speed = Mathf.Abs(Vector3.Dot(velocity, -normal.normalized));
        }
        else
        {
            speed = velocity.magnitude;
        }

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
}
