using ASCENTA.Events;
using UnityEngine;

public sealed class DashFOVEffector : FOVEffector
{
    [Header("Dash Timing")]
    [SerializeField, Min(0f)] float minDuration = 0.2f;
    [SerializeField, Min(0f)] float durationScale = 1f;
    [SerializeField] float additionalDuration = 0f;

    [Header("Magnitude")]
    [SerializeField, Min(0f)] float maxFovIncrease = 12f;
    [SerializeField] AnimationCurve response = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
    [SerializeField] bool useUnscaledTime = true;

    float effectDuration;
    float elapsedTime;
    bool isActive;

    bool subscribed;

    protected override void OnEnable()
    {
        base.OnEnable();
        if (!subscribed)
        {
            subscribed = EventBus.Subscribe<OnDashEvent>(HandleDash);
            if (!subscribed)
            {
                Debug.LogWarning($"{nameof(DashFOVEffector)} could not subscribe to {nameof(OnDashEvent)}.");
            }
        }
    }

    void OnDisable()
    {
        if (subscribed)
        {
            EventBus.Unsubscribe<OnDashEvent>(HandleDash);
            subscribed = false;
        }
    }

    void HandleDash(OnDashEvent e)
    {
        effectDuration = Mathf.Max(minDuration, e.Duration * durationScale + additionalDuration);
        elapsedTime = 0f;
        isActive = true;
    }

    protected override float GetStrength01()
    {
        if (!isActive || effectDuration <= 0f)
        {
            return 0f;
        }

        float delta = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        elapsedTime += delta;

        float normalized = Mathf.Clamp01(elapsedTime / effectDuration);
        float value = Mathf.Clamp01(response.Evaluate(normalized));

        if (elapsedTime >= effectDuration)
        {
            isActive = false;
        }

        return value;
    }

    protected override float GetDeltaFovDegrees() => maxFovIncrease;
}
