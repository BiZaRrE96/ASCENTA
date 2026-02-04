using ASCENTA.Events;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public sealed class UndoVignetteEffect : EventBusListener<OnUndoBeganEvent>
{
    [Header("Volume")]
    [SerializeField] Volume targetVolume;

    [Header("Undo Vignette")]
    [SerializeField, Min(0f)] float targetWeight = 0.8f;
    [SerializeField, Min(0f)] float holdTime = 0.25f;
    [SerializeField] AnimationCurve weightCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    float restingWeight;
    float baseWeight;
    float remainingTime;
    float effectDuration;
    float elapsedTime;

    protected override void Awake()
    {
        base.Awake();
        CacheVolume();
    }

    void CacheVolume()
    {
        if (targetVolume == null)
        {
            targetVolume = FindFirstObjectByType<Volume>();
        }

        if (targetVolume != null)
        {
            baseWeight = targetVolume.weight;
            restingWeight = baseWeight;
        }
    }

    void OnValidate()
    {
        holdTime = Mathf.Max(0f, holdTime);
        if (weightCurve == null || weightCurve.length == 0)
        {
            weightCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        }
    }

    void Update()
    {
        if (targetVolume == null || remainingTime <= 0f)
        {
            return;
        }

        float dt = Time.deltaTime;
        elapsedTime += dt;
        if (effectDuration <= 0f)
        {
            remainingTime = 0f;
        }
        else
        {
            remainingTime = Mathf.Max(0f, effectDuration - elapsedTime);
        }

        float normalized = effectDuration <= 0f ? 1f : Mathf.Clamp01(elapsedTime / effectDuration);
        float curveValue = Mathf.Clamp01(weightCurve.Evaluate(normalized));
        targetVolume.weight = Mathf.Lerp(restingWeight, targetWeight, curveValue);

        if (remainingTime <= 0f)
        {
            targetVolume.weight = restingWeight;
        }
    }

    protected override void OnEvent(OnUndoBeganEvent eventData)
    {
        if (targetVolume == null)
        {
            CacheVolume();
            if (targetVolume == null)
            {
                return;
            }
        }

        restingWeight = baseWeight;
        effectDuration = holdTime;
        elapsedTime = 0f;
        remainingTime = effectDuration;

        targetVolume.weight = restingWeight;

        if (effectDuration <= 0f)
        {
            targetVolume.weight = targetWeight;
            return;
        }

        targetVolume.weight = restingWeight;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (targetVolume != null)
        {
            targetVolume.weight = baseWeight;
        }

        remainingTime = 0f;
        elapsedTime = 0f;
    }

    protected override void OnSubscribeFailed()
    {
        Debug.LogWarning($"{nameof(UndoVignetteEffect)} failed to subscribe to {typeof(OnUndoBeganEvent).Name}; the event may already be registered.");
    }
}
