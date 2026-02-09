using System.Collections;
using ASCENTA.Events;
using UnityEngine;

public sealed class SaveLoadUI : MonoBehaviour
{
    [SerializeField] SceneReferenceSO inGameScene;

    SceneLoad sceneLoad;
    PendingAction pendingAction;

    enum PendingAction
    {
        None,
        LoadGame,
        NewGame
    }

    void OnEnable()
    {
        EventBus.Subscribe<LoadGameRequestedEvent>(HandleLoadRequested);
        EventBus.Subscribe<NewGameRequestedEvent>(HandleNewGameRequested);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<LoadGameRequestedEvent>(HandleLoadRequested);
        EventBus.Unsubscribe<NewGameRequestedEvent>(HandleNewGameRequested);

        if (sceneLoad != null)
        {
            sceneLoad.SceneLoaded -= HandleSceneLoaded;
        }
    }

    void EnsureSceneLoad()
    {
        if (sceneLoad != null)
        {
            return;
        }

        sceneLoad = SceneLoad.Instance ?? FindFirstObjectByType<SceneLoad>(FindObjectsInactive.Include);
        if (sceneLoad != null)
        {
            sceneLoad.SceneLoaded += HandleSceneLoaded;
        }
        else
        {
            Debug.LogWarning("SaveLoadUI could not find a SceneLoad instance.", this);
        }
    }

    void HandleLoadRequested(LoadGameRequestedEvent _)
    {
        pendingAction = PendingAction.LoadGame;
        Debug.Log("Load requested");
        RequestSceneLoad();
    }

    void HandleNewGameRequested(NewGameRequestedEvent _)
    {
        pendingAction = PendingAction.NewGame;
        Debug.Log("New game requested");
        RequestSceneLoad();
    }

    void RequestSceneLoad()
    {
        EnsureSceneLoad();

        if (sceneLoad == null)
        {
            return;
        }

        if (inGameScene == null || !inGameScene.IsValid)
        {
            Debug.LogWarning("SaveLoadUI has no valid InGame SceneReferenceSO assigned.", this);
            return;
        }

        sceneLoad.LoadScene(inGameScene);
    }

    void HandleSceneLoaded(string sceneName)
    {
        if (!IsTargetScene(sceneName))
        {
            return;
        }

        switch (pendingAction)
        {
            case PendingAction.NewGame:
                StartCoroutine(CallNewGame());
                break;
            case PendingAction.LoadGame:
                StartCoroutine(CallLoadGame());
                break;
        }

        pendingAction = PendingAction.None;
    }

    bool IsTargetScene(string sceneName)
    {
        if (inGameScene == null || !inGameScene.IsValid)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(inGameScene.SceneName))
        {
            return string.Equals(sceneName, inGameScene.SceneName);
        }

        return false;
    }

    void ClearSaveData()
    {
        var manager = DataPersistenceManager.Instance;
        if (manager == null)
        {
            Debug.LogWarning("SaveLoadUI could not find DataPersistenceManager to clear save data.", this);
            return;
        }

        manager.ClearSaveData();
    }

    IEnumerator CallNewGame()
    {
        yield return new WaitForFixedUpdate();
        var manager = DataPersistenceManager.Instance;
        if (manager == null)
        {
            Debug.LogWarning("SaveLoadUI could not find DataPersistenceManager to clear save data.", this);
            yield break;
        }
        manager.NewGame();
        EventBus.Publish(new GameStartedEvent());
    }

    IEnumerator CallLoadGame()
    {
        yield return new WaitForFixedUpdate();
        var manager = DataPersistenceManager.Instance;
        if (manager == null)
        {
            Debug.LogWarning("SaveLoadUI could not find DataPersistenceManager to clear save data.", this);
            yield break;
        }
        manager.LoadGame();
        EventBus.Publish(new GameStartedEvent());
    }
}
