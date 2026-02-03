using UnityEngine;

/// <summary>
/// Centralizes slow motion control and guarantees a single controller instance.
/// </summary>
public sealed class TimeController : MonoBehaviour
{
    [SerializeField, Min(0.01f), Tooltip("Default time scale used when a slow motion effect is requested.")]
    float slowMotionTimeScale = 0.3f;

    const float MinTimeScale = 0.01f;

    static TimeController instance;
    float originalTimeScale = 1f;
    float originalFixedDeltaTime = 0.02f;
    bool isSlowMotionActive;

    public static TimeController Instance => instance;
    public float SlowMotionTimeScale => slowMotionTimeScale;
    public bool IsSlowMotionActive => isSlowMotionActive;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        originalTimeScale = Time.timeScale;
        originalFixedDeltaTime = Mathf.Max(Time.fixedDeltaTime, MinTimeScale);
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
}
