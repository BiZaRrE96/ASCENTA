using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class CameraFOVEffector : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Camera targetCamera;

    [Header("Base FOV")]
    [SerializeField] private bool useInitialCameraFovAsBase = true;
    [SerializeField] private float baseFov = 60f;

    [Header("Clamp")]
    [SerializeField] private float minFov = 30f;
    [SerializeField] private float maxFov = 110f;

    [Header("Smoothing")]
    [SerializeField, Min(0f)] private float cameraSmoothTime = 0.08f;
    [SerializeField] private bool useUnscaledTime = false;

    private readonly List<FOVEffector> effectors = new();
    private float fovVel;

    public float BaseFov
    {
        get => baseFov;
        set => baseFov = value;
    }

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = GetComponent<Camera>();

        if (useInitialCameraFovAsBase)
            baseFov = targetCamera.fieldOfView;
    }

    public void Register(FOVEffector effector)
    {
        if (effector == null) return;
        if (!effectors.Contains(effector))
            effectors.Add(effector);
    }

    public void Unregister(FOVEffector effector)
    {
        if (effector == null) return;
        effectors.Remove(effector);
    }

    private void LateUpdate()
    {
        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        // Sum contributions (delta FOV in degrees)
        float delta = 0f;

        for (int i = effectors.Count - 1; i >= 0; i--)
        {
            var e = effectors[i];
            if (e == null)
            {
                effectors.RemoveAt(i);
                continue;
            }

            delta += e.EvaluateDeltaFov(dt);
        }

        float targetFov = Mathf.Clamp(baseFov + delta, minFov, maxFov);

        // Smooth the camera itself (on top of effector blending)
        targetCamera.fieldOfView = Mathf.SmoothDamp(
            targetCamera.fieldOfView,
            targetFov,
            ref fovVel,
            Mathf.Max(0.0001f, cameraSmoothTime),
            Mathf.Infinity,
            dt
        );
    }
}
