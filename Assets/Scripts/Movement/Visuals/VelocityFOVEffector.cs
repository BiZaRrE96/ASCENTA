using FMOD.Studio;
using FMODUnity;
using UnityEngine;

public sealed class VelocityFOVEffector : FOVEffector
{
    [Header("FMOD")]
    [SerializeField] private EventReference windyEvent;
    [SerializeField] private string velocityIntensityParameter = "Velocity_Intensity";
    [SerializeField] private bool attachToGameObject = true;
    [SerializeField, Min(0f)] private float intensityIncreaseRate = 6f;
    [SerializeField, Min(0f)] private float intensityDecreaseRate = 4f;

    [Header("Velocity Source")]
    [SerializeField] private Rigidbody rb;

    [Header("Mapping")]
    [SerializeField, Min(0f)] private float minSpeed = 0f;
    [SerializeField, Min(0.01f)] private float maxSpeed = 10f;
    [SerializeField] private float maxFovIncrease = 15f;
    [SerializeField] private AnimationCurve response = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private EventInstance windyInstance;
    private bool warnedMissingEvent;
    private bool instanceValid;
    private float intensity;

    protected override void Awake()
    {
        base.Awake();
        if (rb == null) rb = GetComponent<Rigidbody>();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        intensity = 0f;
        TryStartInstance();
    }

    private void Update()
    {
        if (!instanceValid)
        {
            return;
        }

        float target = (enabled && gameObject.activeInHierarchy && Active) ? GetStrength01() : 0f;
        target = Mathf.Clamp01(target);

        float rate = target > intensity ? intensityIncreaseRate : intensityDecreaseRate;
        float lerpT = Mathf.Clamp01(rate * Time.deltaTime);
        intensity = Mathf.Lerp(intensity, target, lerpT);

        windyInstance.setParameterByName(velocityIntensityParameter, intensity);
    }

    protected override float GetStrength01()
    {
        if (rb == null) return 0f;

        float speed = rb.linearVelocity.magnitude;
        float t = Mathf.InverseLerp(minSpeed, maxSpeed, speed);
        return Mathf.Clamp01(response.Evaluate(t));
    }

    protected override float GetDeltaFovDegrees() => maxFovIncrease;

    private void TryStartInstance()
    {
        if (windyEvent.IsNull)
        {
            if (!warnedMissingEvent)
            {
                Debug.LogWarning($"{nameof(VelocityFOVEffector)} has no windy event assigned.");
                warnedMissingEvent = true;
            }
            return;
        }

        if (instanceValid)
        {
            return;
        }

        windyInstance = RuntimeManager.CreateInstance(windyEvent);

        if (attachToGameObject)
        {
            RuntimeManager.AttachInstanceToGameObject(windyInstance, transform, rb);
        }

        windyInstance.start();
        windyInstance.setParameterByName(velocityIntensityParameter, intensity);
        instanceValid = true;
    }

    private void StopInstance()
    {
        if (!instanceValid)
        {
            return;
        }

        windyInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        windyInstance.release();
        instanceValid = false;
    }

    private void OnDisable()
    {
        StopInstance();
    }

    protected override void OnDestroy()
    {
        StopInstance();
        base.OnDestroy();
    }
}
