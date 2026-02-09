using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class ServiceRegistry : MonoBehaviour
{
    public static ServiceRegistry Instance { get; private set; }

    public event Action InitialServicesLoaded;

    [Header("Bootstrap")]
    [SerializeField, Tooltip("Load the initial services on Start.")]
    bool loadInitialServicesOnStart = true;

    [SerializeField, Tooltip("Keep registry and instantiated services across scenes.")]
    bool keepAlive = true;

    [Header("Services")]
    [SerializeField, Tooltip("Service prefabs to instantiate before the first scene loads.")]
    List<GameObject> initialServicePrefabs = new List<GameObject>();

    [SerializeField, Tooltip("Optional parent for instantiated services.")]
    Transform serviceParent;

    readonly List<GameObject> loadedServiceObjects = new List<GameObject>();
    readonly List<MonoBehaviour> loadedServiceComponents = new List<MonoBehaviour>();

    bool isLoadingInitialServices;

    public bool InitialServicesReady { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (keepAlive)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void Start()
    {
        if (loadInitialServicesOnStart)
        {
            StartCoroutine(LoadInitialServices());
        }
    }

    public IEnumerator LoadInitialServices()
    {
        if (InitialServicesReady || isLoadingInitialServices)
        {
            yield break;
        }

        isLoadingInitialServices = true;

        for (int i = 0; i < initialServicePrefabs.Count; i++)
        {
            GameObject prefab = initialServicePrefabs[i];
            if (prefab == null)
            {
                Debug.LogWarning("ServiceRegistry has a null service prefab reference.", this);
                continue;
            }

            yield return LoadServiceRoutine(prefab);
        }

        InitialServicesReady = true;
        isLoadingInitialServices = false;
        InitialServicesLoaded?.Invoke();
    }

    public Coroutine LoadServiceAsync(GameObject servicePrefab)
    {
        if (servicePrefab == null)
        {
            Debug.LogWarning("ServiceRegistry.LoadServiceAsync called with a null prefab.", this);
            return null;
        }

        return StartCoroutine(LoadServiceRoutine(servicePrefab));
    }

    IEnumerator LoadServiceRoutine(GameObject servicePrefab)
    {
        GameObject instance = Instantiate(servicePrefab, serviceParent);
        instance.name = servicePrefab.name;

        if (keepAlive)
        {
            DontDestroyOnLoad(instance);
        }

        loadedServiceObjects.Add(instance);

        MonoBehaviour[] monos = instance.GetComponentsInChildren<MonoBehaviour>(true);
        bool foundService = false;
        for (int i = 0; i < monos.Length; i++)
        {
            MonoBehaviour mono = monos[i];
            if (mono is IService service)
            {
                foundService = true;
                loadedServiceComponents.Add(mono);
                IEnumerator init = service.InitializeService();
                if (init != null)
                {
                    yield return init;
                }
            }
        }

        if (!foundService)
        {
            Debug.LogWarning($"ServiceRegistry instantiated '{servicePrefab.name}' but found no IService components.", instance);
        }
    }

    public IReadOnlyList<GameObject> GetLoadedServiceObjects() => loadedServiceObjects;
    public IReadOnlyList<MonoBehaviour> GetLoadedServiceComponents() => loadedServiceComponents;
}
