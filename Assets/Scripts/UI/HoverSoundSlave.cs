using FMODUnity;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class HoverSoundSlave : ButtonHoverSlave
{
    [Header("FMOD")]
    [SerializeField] EventReference buttonEnterEvent;
    [SerializeField] bool attachToGameObject = true;

    bool warnedMissingEvent;

    public override void OnHoverStart()
    {
        if (!IsEnabled)
        {
            return;
        }

        if (buttonEnterEvent.IsNull)
        {
            if (!warnedMissingEvent)
            {
                Debug.LogWarning($"{nameof(HoverSoundSlave)} has no button enter event assigned.");
                warnedMissingEvent = true;
            }
            return;
        }

        if (attachToGameObject)
        {
            RuntimeManager.PlayOneShotAttached(buttonEnterEvent, gameObject);
            return;
        }

        RuntimeManager.PlayOneShot(buttonEnterEvent, transform.position);
    }
}
