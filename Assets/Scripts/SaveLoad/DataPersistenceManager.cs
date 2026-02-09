using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System.Threading.Tasks;
using ASCENTA.Events;
using UnityEngine;


public class DataPersistenceManager : MonoBehaviour, IService
{

    // Singleton setup
    public static DataPersistenceManager Instance { get; private set; }

    [Header("File Storage Config")]
    [SerializeField] string fileName;

    public bool HasSaveData { get; private set; }
    public bool HasLoadedData => currentData != null;
    public GameData CurrentData => currentData;

    GameData currentData;
    List<IDataPersistence> dataPersistenceObjects;
    FileDataHandler dataHandler;
    bool serviceInitialized;

    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        WarnIfNotUnderServiceRegistry();
    }

    private void Start()
    {
        if (!serviceInitialized)
        {
            StartCoroutine(InitializeService());
        }
    }

    public IEnumerator InitializeService()
    {
        if (serviceInitialized)
        {
            yield break;
        }

        serviceInitialized = true;
        EnsureInitialized();
        HasSaveData = dataHandler.HasSaveData();
        if (HasSaveData)
        {
            yield return LoadOnStartAsync();
        }
    }

    IEnumerator LoadOnStartAsync()
    {
        Task<GameData> loadTask = dataHandler.LoadAsync();
        while (!loadTask.IsCompleted)
        {
            yield return null;
        }

        if (loadTask.IsFaulted)
        {
            Debug.LogWarning($"Async load failed: {loadTask.Exception}");
            currentData = null;
        }
        else
        {
            currentData = loadTask.Result;
        }

        EventBus.Publish(new GameDataLoadedEvent(HasSaveData));
    }

    public void NewGame()
    {
        currentData = new GameData();
        EventBus.Publish(new GameDataLoadedEvent(HasSaveData));
    }

    public void LoadGame()
    {
        EnsureInitialized();
        RefreshDataPersistenceObjects();
        Debug.Log($"DataPersistenceManager.LoadGame affecting {dataPersistenceObjects.Count} IDataPersistence objects.");

        HasSaveData = dataHandler.HasSaveData();
        currentData = dataHandler.Load();
        if (currentData == null)
        {
            Debug.LogWarning("Save data was missing!");
            NewGame();
        }

        foreach (IDataPersistence dataPersistence in dataPersistenceObjects)
        {
            dataPersistence.BeforeLoadData();
        }

        foreach (IDataPersistence dataPersistence in dataPersistenceObjects.OrderBy(item => item.loadPriority))
        {
            dataPersistence.LoadData(currentData);
        }

        foreach (IDataPersistence dataPersistence in dataPersistenceObjects)
        {
            dataPersistence.LoadDataComplete();
        }
    }

    public void SaveGame()
    {
        EnsureInitialized();
        RefreshDataPersistenceObjects();
        Debug.Log($"DataPersistenceManager.SaveGame affecting {dataPersistenceObjects.Count} IDataPersistence objects.");

        if (currentData == null)
        {
            NewGame();
        }

        foreach (IDataPersistence dataPersistence in dataPersistenceObjects.OrderBy(item => item.loadPriority))
        {
            dataPersistence.SaveData(ref currentData);
        }

        dataHandler.Save(currentData);
        HasSaveData = dataHandler.HasSaveData();
    }


    [ContextMenu("Save Game")]
    void SaveGameContextMenu()
    {
        SaveGame();
    }

    [ContextMenu("Load Game")]
    void LoadGameContextMenu()
    {
        LoadGame();
    }

    void EnsureInitialized()
    {
        if (dataHandler == null)
        {
            dataHandler = new FileDataHandler(Application.persistentDataPath, fileName);
        }

        HasSaveData = dataHandler.HasSaveData();

        if (dataPersistenceObjects == null)
        {
            dataPersistenceObjects = new List<IDataPersistence>();
        }
    }

    void WarnIfNotUnderServiceRegistry()
    {
        if (ServiceRegistry.Instance == null)
        {
            return;
        }

        if (!transform.IsChildOf(ServiceRegistry.Instance.transform))
        {
            Debug.LogWarning("DataPersistenceManager is not parented under ServiceRegistry. Consider spawning it via ServiceRegistry.", this);
        }
    }

    public bool CheckHasSaveData()
    {
        EnsureInitialized();
        return HasSaveData;
    }

    public bool ClearSaveData()
    {
        EnsureInitialized();
        bool deleted = dataHandler.Delete();
        HasSaveData = dataHandler.HasSaveData();
        if (deleted)
        {
            currentData = null;
        }

        return deleted;
    }

    List<IDataPersistence> FindAllDataPersistenceObjects()
    {
        return new List<IDataPersistence>(FindObjectsByType<IDataPersistence>(FindObjectsInactive.Include, FindObjectsSortMode.None));
    }

    void RefreshDataPersistenceObjects()
    {
        dataPersistenceObjects = FindAllDataPersistenceObjects();
    }
}
