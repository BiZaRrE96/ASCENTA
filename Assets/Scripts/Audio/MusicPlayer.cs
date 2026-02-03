using FMOD.Studio;
using FMODUnity;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class MusicPlayer : MonoBehaviour
{
    [Header("FMOD")]
    [SerializeField] EventReference musicEvent;
    [SerializeField] string velocityIntensityParameter = "Velocity_Intensity";
    [SerializeField] bool attachToGameObject = true;
    [SerializeField] bool allowFadeoutOnStop = true;

    [Header("Velocity")]
    [SerializeField, Min(0f)] float thresholdSpeed = 6f;
    [SerializeField, Min(0.1f)] float maxSpeed = 20f;
    [SerializeField] bool useHorizontalSpeed = true;

    [Header("Intensity")]
    [SerializeField, Range(0f, 10f)] float initialIntensity = 0f;
    [SerializeField, Min(0f)] float baseIncreaseRate = 0.05f;
    [SerializeField, Min(0f)] float extraIncreaseRate = 0.2f;
    [SerializeField, Min(0f)] float decreaseRate = 0.03f;
    [SerializeField, Min(0f)] float decreaseDelay = 1f;

    [Header("References")]
    [SerializeField] MovementController movementController;
    [SerializeField] Rigidbody rb;

    EventInstance musicInstance;
    bool warnedMissingEvent;
    bool instanceValid;
    float intensity;
    float belowThresholdTime;

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

        intensity = Mathf.Clamp(initialIntensity, 0f, 10f);
    }

    void OnEnable()
    {
        intensity = Mathf.Clamp(initialIntensity, 0f, 10f);
        belowThresholdTime = 0f;
        TryStartInstance();
    }

    void Update()
    {
        if (!instanceValid)
        {
            return;
        }

        float speed = GetSpeed();
        if (speed >= thresholdSpeed)
        {
            belowThresholdTime = 0f;

            float speedFactor = maxSpeed <= thresholdSpeed
                ? 1f
                : Mathf.InverseLerp(thresholdSpeed, maxSpeed, speed);
            float increaseRate = baseIncreaseRate + (extraIncreaseRate * speedFactor);

            intensity = Mathf.Clamp(intensity + (increaseRate * Time.deltaTime), 0f, 10f);
        }
        else
        {
            belowThresholdTime += Time.deltaTime;
            if (belowThresholdTime >= decreaseDelay)
            {
                intensity = Mathf.Clamp(intensity - (decreaseRate * Time.deltaTime), 0f, 10f);
            }
        }

        musicInstance.setParameterByName(velocityIntensityParameter, intensity);
    }

    float GetSpeed()
    {
        if (rb == null)
        {
            return movementController != null ? movementController.Velocity : 0f;
        }

        Vector3 velocity = rb.linearVelocity;
        if (useHorizontalSpeed)
        {
            velocity = Vector3.ProjectOnPlane(velocity, Vector3.up);
        }

        return velocity.magnitude;
    }

    void TryStartInstance()
    {
        if (musicEvent.IsNull)
        {
            if (!warnedMissingEvent)
            {
                Debug.LogWarning($"{nameof(MusicPlayer)} has no music event assigned.");
                warnedMissingEvent = true;
            }
            return;
        }

        if (instanceValid)
        {
            return;
        }

        musicInstance = RuntimeManager.CreateInstance(musicEvent);

        if (attachToGameObject)
        {
            RuntimeManager.AttachInstanceToGameObject(musicInstance, transform, rb);
        }

        musicInstance.start();
        musicInstance.setParameterByName(velocityIntensityParameter, intensity);
        instanceValid = true;
    }

    void StopInstance()
    {
        if (!instanceValid)
        {
            return;
        }

        musicInstance.stop(allowFadeoutOnStop ? FMOD.Studio.STOP_MODE.ALLOWFADEOUT : FMOD.Studio.STOP_MODE.IMMEDIATE);
        musicInstance.release();
        instanceValid = false;
    }

    void OnDisable()
    {
        StopInstance();
    }

    void OnDestroy()
    {
        StopInstance();
    }
}
