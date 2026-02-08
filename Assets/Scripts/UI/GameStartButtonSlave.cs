using FMODUnity;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class GameStartButtonSlave : ButtonHoverSlave
{
    [Header("FMOD")]
    [SerializeField] EventReference buttonPressEvent;
    [SerializeField] bool attachToGameObject = true;

    bool warnedMissingEvent;

    public override void OnClick()
    {
        if (!IsEnabled)
        {
            return;
        }

        if (buttonPressEvent.IsNull)
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
            RuntimeManager.PlayOneShotAttached(buttonPressEvent, gameObject);
            return;
        }

        RuntimeManager.PlayOneShot(buttonPressEvent, transform.position);
    }
}
