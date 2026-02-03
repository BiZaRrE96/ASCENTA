using ASCENTA.Events;
using FMODUnity;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class DashFmodSfx : EventBusListener<OnDashEvent>
{
    [Header("FMOD")]
    [SerializeField] EventReference dashEvent;
    [SerializeField] bool attachToGameObject = true;

    bool warnedMissingEvent;

    protected override void OnEvent(OnDashEvent eventData)
    {
        if (dashEvent.IsNull)
        {
            if (!warnedMissingEvent)
            {
                Debug.LogWarning($"{nameof(DashFmodSfx)} has no dash event assigned.");
                warnedMissingEvent = true;
            }
            return;
        }

        if (attachToGameObject)
        {
            RuntimeManager.PlayOneShotAttached(dashEvent, gameObject);
            return;
        }

        RuntimeManager.PlayOneShot(dashEvent, transform.position);
    }
}
