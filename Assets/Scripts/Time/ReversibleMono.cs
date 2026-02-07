using ASCENTA.Events;
using UnityEngine;

/// <summary>
/// Base class that tracks a reversible fixed delta time driven by TimeController.
/// </summary>
public abstract class ReversibleMono : MonoBehaviour
{
    float fixedDeltaTime;

    protected float FixedDeltaTime => fixedDeltaTime;

    protected virtual void Awake()
    {
        fixedDeltaTime = Time.fixedDeltaTime;
    }

    protected virtual void OnEnable()
    {
        EventBus.Subscribe<ReversalEvent>(HandleReversalEvent);
    }

    protected virtual void OnDisable()
    {
        EventBus.Unsubscribe<ReversalEvent>(HandleReversalEvent);
    }

    void HandleReversalEvent(ReversalEvent data)
    {
        fixedDeltaTime = data.IsReversing ? data.ReversalFixedDelta : Time.fixedDeltaTime;
        OnReversalChanged(data.IsReversing, data.ReversalFixedDelta);
    }

    protected virtual void OnReversalChanged(bool isReversing, float reversalFixedDelta)
    {
    }
}
