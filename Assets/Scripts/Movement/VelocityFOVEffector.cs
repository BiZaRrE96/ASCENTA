using UnityEngine;

public sealed class VelocityFOVEffector : FOVEffector
{
    [Header("Velocity Source")]
    [SerializeField] private Rigidbody rb;

    [Header("Mapping")]
    [SerializeField, Min(0f)] private float minSpeed = 0f;
    [SerializeField, Min(0.01f)] private float maxSpeed = 10f;
    [SerializeField] private float maxFovIncrease = 15f;
    [SerializeField] private AnimationCurve response = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    protected override void Awake()
    {
        base.Awake();
        if (rb == null) rb = GetComponent<Rigidbody>();
    }

    protected override float GetStrength01()
    {
        if (rb == null) return 0f;

        float speed = rb.linearVelocity.magnitude;
        float t = Mathf.InverseLerp(minSpeed, maxSpeed, speed);
        return Mathf.Clamp01(response.Evaluate(t));
    }

    protected override float GetDeltaFovDegrees() => maxFovIncrease;
}
