using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System.Threading.Tasks;
using ASCENTA.Events;
using UnityEngine;


public class DataPersistenceManager : MonoBehaviour
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

    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        EnsureInitialized();
        HasSaveData = dataHandler.HasSaveData();
        if (HasSaveData)
        {
            StartCoroutine(LoadOnStartAsync());
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
