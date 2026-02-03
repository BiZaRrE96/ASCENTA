using ASCENTA.Events;
using FMODUnity;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class JumppadSFX : EventBusListener<OnJumpPadBoostEvent>
{
    [Header("FMOD")]
    [SerializeField] EventReference jumpPadEvent;

    bool warnedMissingEvent;

    protected override void OnEvent(OnJumpPadBoostEvent eventData)
    {
        if (jumpPadEvent.IsNull)
        {
            if (!warnedMissingEvent)
            {
                Debug.LogWarning($"{nameof(JumppadSFX)} has no jump pad event assigned.");
                warnedMissingEvent = true;
            }
            return;
        }

        RuntimeManager.PlayOneShot(jumpPadEvent, eventData.ContactPoint);
    }
}
