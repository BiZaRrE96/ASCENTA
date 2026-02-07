using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class DataPersistenceManager : MonoBehaviour
{

    // Singleton setup
    public static DataPersistenceManager Instance { get; private set; }

    [Header("File Storage Config")]
    [SerializeField] string fileName;

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
    }

    public void NewGame()
    {
        currentData = new GameData();
    }

    public void LoadGame()
    {
        EnsureInitialized();

        currentData = dataHandler.Load();
        if (currentData == null)
        {
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

        if (currentData == null)
        {
            NewGame();
        }

        foreach (IDataPersistence dataPersistence in dataPersistenceObjects.OrderBy(item => item.loadPriority))
        {
            dataPersistence.SaveData(ref currentData);
        }

        dataHandler.Save(currentData);
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

        if (dataPersistenceObjects == null || dataPersistenceObjects.Count == 0)
        {
            dataPersistenceObjects = FindAllDataPersistenceObjects();
        }
    }

    List<IDataPersistence> FindAllDataPersistenceObjects()
    {
        return new List<IDataPersistence>(FindObjectsByType<IDataPersistence>(FindObjectsInactive.Include, FindObjectsSortMode.None));
    }
}
