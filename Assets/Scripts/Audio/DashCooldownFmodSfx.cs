using ASCENTA.Events;
using FMODUnity;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class DashCooldownFmodSfx : EventBusListener<OnDashCooldownFinishedEvent>
{
    [Header("FMOD")]
    [SerializeField] EventReference dashCooldownEvent;
    [SerializeField] bool attachToGameObject = true;

    bool warnedMissingEvent;

    protected override void OnEvent(OnDashCooldownFinishedEvent eventData)
    {
        if (dashCooldownEvent.IsNull)
        {
            if (!warnedMissingEvent)
            {
                Debug.LogWarning($"{nameof(DashCooldownFmodSfx)} has no dash cooldown event assigned.");
                warnedMissingEvent = true;
            }
            return;
        }

        if (attachToGameObject)
        {
            RuntimeManager.PlayOneShotAttached(dashCooldownEvent, gameObject);
            return;
        }

        RuntimeManager.PlayOneShot(dashCooldownEvent, transform.position);
    }
}
