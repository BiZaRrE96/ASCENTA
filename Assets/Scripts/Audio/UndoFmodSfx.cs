using ASCENTA.Events;
using FMODUnity;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class UndoFmodSfx : EventBusListener<OnUndoBeganEvent>
{
    [Header("FMOD")]
    [SerializeField] EventReference undoEvent;
    [SerializeField] bool attachToGameObject = true;

    bool warnedMissingEvent;

    protected override void OnEvent(OnUndoBeganEvent eventData)
    {
        if (undoEvent.IsNull)
        {
            if (!warnedMissingEvent)
            {
                Debug.LogWarning($"{nameof(UndoFmodSfx)} has no undo event assigned.");
                warnedMissingEvent = true;
            }
            return;
        }

        if (attachToGameObject)
        {
            RuntimeManager.PlayOneShotAttached(undoEvent, gameObject);
            return;
        }

        RuntimeManager.PlayOneShot(undoEvent, transform.position);
    }
}
