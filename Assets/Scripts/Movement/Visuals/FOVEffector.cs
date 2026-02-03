using UnityEngine;

public abstract class FOVEffector : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private CameraFOVEffector target;

    [Header("Blending")]
    [SerializeField, Range(0f, 1f)] private float weight = 1f;
    [SerializeField] private bool active = true;
    [SerializeField, Min(0f)] private float blendInTime = 0.10f;
    [SerializeField, Min(0f)] private float blendOutTime = 0.15f;

    private float blend;        // 0..1
    private float blendVel;

    /// <summary>CameraFOVEffector this effector contributes to.</summary>
    public CameraFOVEffector Target
    {
        get => target;
        set
        {
            if (target == value) return;
            if (target != null) target.Unregister(this);
            target = value;
            if (target != null) target.Register(this);
        }
    }

    /// <summary>0..1 multiplier on this effector.</summary>
    public float Weight
    {
        get => weight;
        set => weight = Mathf.Clamp01(value);
    }

    /// <summary>Turns the effector on/off (will blend smoothly).</summary>
    public bool Active
    {
        get => active;
        set => active = value;
    }

    protected virtual void Awake()
    {
        // Optional convenience: auto-find a CameraFOVEffector if not assigned.
        // If you have multiple cameras, you should assign Target explicitly in the inspector.
        if (target == null)
            target = FindFirstObjectByType<CameraFOVEffector>();
    }

    protected virtual void OnEnable()
    {
        if (target != null) target.Register(this);
    }

    protected virtual void OnDestroy()
    {
        if (target != null) target.Unregister(this);
    }

    /// <summary>
    /// Called by CameraFOVEffector every frame. Returns a blended delta FOV (degrees).
    /// </summary>
    public float EvaluateDeltaFov(float dt)
    {
        // Note: even if this component is disabled, CameraFOVEffector may still call this
        // (because it keeps a reference). That’s intentional to allow smooth blend-out.
        bool shouldBeOn = enabled && gameObject.activeInHierarchy && active;

        float targetBlend = shouldBeOn ? 1f : 0f;
        float smoothTime = (targetBlend > blend) ? blendInTime : blendOutTime;
        smoothTime = Mathf.Max(0.0001f, smoothTime);

        blend = Mathf.SmoothDamp(blend, targetBlend, ref blendVel, smoothTime, Mathf.Infinity, dt);

        float strength = blend * weight * Mathf.Clamp01(GetStrength01());
        if (strength <= 0.0001f) return 0f;

        return GetDeltaFovDegrees() * strength;
    }

    /// <summary>
    /// 0..1 runtime strength for this effector (e.g., speed normalized).
    /// Default is 1.
    /// </summary>
    protected virtual float GetStrength01() => 1f;

    /// <summary>
    /// The delta FOV (in degrees) at full strength (before weight/blend).
    /// Can be positive or negative.
    /// </summary>
    protected abstract float GetDeltaFovDegrees();
}
