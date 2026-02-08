using ASCENTA.Events;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CheckSaveFileButton : MonoBehaviour
{
    [SerializeField] ButtonHoverMaster button;

    void Awake()
    {
        if (button == null)
        {
            button = GetComponent<ButtonHoverMaster>();
        }

        if (button == null)
        {
            Debug.LogWarning($"{nameof(CheckSaveFileButton)} could not find a {nameof(ButtonHoverMaster)}.", this);
            return;
        }

        button.Disable();
    }

    void OnEnable()
    {
        EventBus.Subscribe<GameDataLoadedEvent>(HandleGameDataLoaded);
        TryApplyImmediateState();
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<GameDataLoadedEvent>(HandleGameDataLoaded);
    }

    void TryApplyImmediateState()
    {
        var manager = DataPersistenceManager.Instance
            ?? FindFirstObjectByType<DataPersistenceManager>(FindObjectsInactive.Include);
        if (manager == null || !manager.HasLoadedData)
        {
            return;
        }

        ApplyState(manager.HasSaveData);
    }

    void HandleGameDataLoaded(GameDataLoadedEvent eventData)
    {
        ApplyState(eventData.hasSaveData);
    }

    void ApplyState(bool hasSaveData)
    {
        if (button == null)
        {
            return;
        }

        if (hasSaveData)
        {
            button.Enable();
        }
        else
        {
            button.Disable();
        }
    }
}
