using System.Collections;
using ASCENTA.Events;
using UnityEngine;

/// <summary>
/// Centralizes slow motion control and guarantees a single controller instance.
/// </summary>
public sealed class TimeController : MonoBehaviour, IService
{
    [SerializeField, Min(0.01f), Tooltip("Default time scale used when a slow motion effect is requested.")]
    float slowMotionTimeScale = 0.3f;

    const float MinTimeScale = 0.01f;

    static TimeController instance;
    float originalTimeScale = 1f;
    float originalFixedDeltaTime = 0.02f;
    bool isSlowMotionActive;
    float realTime;
    bool serviceInitialized;

    public static TimeController Instance => instance;
    public float SlowMotionTimeScale => slowMotionTimeScale;
    public bool IsSlowMotionActive => isSlowMotionActive;
    public float GetRealTime() => realTime;
    public static float GetBackwardsScale(float targetTime, float currentTime, float reversalPeriod)
    {
        if (reversalPeriod <= Mathf.Epsilon)
        {
            return 0f;
        }

        if (currentTime <= targetTime)
        {
            return 0f;
        }

        float fixedStep = Mathf.Max(Time.fixedDeltaTime, Mathf.Epsilon);
        int stepCount = Mathf.Max(1, Mathf.CeilToInt(reversalPeriod / fixedStep));
        float delta = targetTime - currentTime;
        return delta / stepCount;
    }

    void HandleUndoBegan(OnUndoBeganEvent eventData)
    {
        float reversalFixedDelta = GetBackwardsScale(eventData.Jump.Time, realTime, eventData.SnapTime);
        RaiseReversalEvent(true, reversalFixedDelta);
    }

    void HandleUndoCompleted(OnUndoEvent _)
    {
        RaiseReversalEvent(false, 0f);
    }

    void RaiseReversalEvent(bool isReversing, float reversalFixedDelta)
    {
        EventBus.Publish(new ReversalEvent(isReversing, reversalFixedDelta));
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        WarnIfNotUnderServiceRegistry();

        originalTimeScale = Time.timeScale;
        originalFixedDeltaTime = Mathf.Max(Time.fixedDeltaTime, MinTimeScale);
        realTime = 0f;
    }

    public IEnumerator InitializeService()
    {
        if (serviceInitialized)
        {
            yield break;
        }

        serviceInitialized = true;
        yield break;
    }

    void OnEnable()
    {
        EventBus.Subscribe<OnUndoBeganEvent>(HandleUndoBegan);
        EventBus.Subscribe<OnUndoEvent>(HandleUndoCompleted);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<OnUndoBeganEvent>(HandleUndoBegan);
        EventBus.Unsubscribe<OnUndoEvent>(HandleUndoCompleted);
    }

    void Update()
    {
        realTime += Time.deltaTime;
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    /// <summary>
    /// Applies slow motion using the default scale or an overridden value.
    /// </summary>
    public void EnterSlowMotion(float? customScale = null)
    {
        float targetScale = customScale ?? slowMotionTimeScale;
        float maxScale = Mathf.Max(originalTimeScale, MinTimeScale);
        targetScale = Mathf.Clamp(targetScale, MinTimeScale, maxScale);

        if (Mathf.Approximately(Time.timeScale, targetScale))
        {
            return;
        }

        Time.timeScale = targetScale;
        Time.fixedDeltaTime = originalFixedDeltaTime * targetScale;
        isSlowMotionActive = true;
    }

    /// <summary>
    /// Restores time to its original scale.
    /// </summary>
    public void ResumeNormalTime()
    {
        if (!isSlowMotionActive)
        {
            return;
        }

        Time.timeScale = originalTimeScale;
        Time.fixedDeltaTime = originalFixedDeltaTime;
        isSlowMotionActive = false;
    }

    /// <summary>
    /// Immediately resets time regardless of the current state.
    /// </summary>
    public void ForceResetTime()
    {
        Time.timeScale = originalTimeScale;
        Time.fixedDeltaTime = originalFixedDeltaTime;
        isSlowMotionActive = false;
    }

    void WarnIfNotUnderServiceRegistry()
    {
        if (ServiceRegistry.Instance == null)
        {
            return;
        }

        if (!transform.IsChildOf(ServiceRegistry.Instance.transform))
        {
            Debug.LogWarning("TimeController is not parented under ServiceRegistry. Consider spawning it via ServiceRegistry.", this);
        }
    }
}
