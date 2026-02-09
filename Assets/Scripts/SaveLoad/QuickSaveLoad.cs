using UnityEngine;

public class QuickSaveLoad : MonoBehaviour
{
    bool introBlocked;

    void OnEnable()
    {
        ASCENTA.Events.EventBus.Subscribe<ASCENTA.Events.IntroStartEvent>(HandleIntroStart);
        ASCENTA.Events.EventBus.Subscribe<ASCENTA.Events.IntroUpdateEvent>(HandleIntroUpdate);
    }

    void OnDisable()
    {
        ASCENTA.Events.EventBus.Unsubscribe<ASCENTA.Events.IntroStartEvent>(HandleIntroStart);
        ASCENTA.Events.EventBus.Unsubscribe<ASCENTA.Events.IntroUpdateEvent>(HandleIntroUpdate);
    }

    public void OnQuicksave()
    {
        if (introBlocked)
        {
            return;
        }

        if (DataPersistenceManager.Instance == null)
        {
            return;
        }

        DataPersistenceManager.Instance.SaveGame();
    }

    public void OnQuickload()
    {
        if (introBlocked)
        {
            return;
        }

        if (DataPersistenceManager.Instance == null)
        {
            return;
        }

        DataPersistenceManager.Instance.LoadGame();
    }

    void HandleIntroStart(ASCENTA.Events.IntroStartEvent eventData)
    {
        if (eventData.WillPlay)
        {
            introBlocked = true;
        }
    }

    void HandleIntroUpdate(ASCENTA.Events.IntroUpdateEvent _)
    {
        introBlocked = false;
    }
}
