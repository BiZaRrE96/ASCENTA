using ASCENTA.Events;
using UnityEngine;

public class SaveLoadHelper : MonoBehaviour
{
    public void RequestNewGame()
    {
        EventBus.Publish(new NewGameRequestedEvent());
        return;
    }

    public void RequestLoadGame()
    {
        EventBus.Publish(new LoadGameRequestedEvent());
        return ;
    }
}