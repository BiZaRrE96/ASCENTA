using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SceneLoad : MonoBehaviour
{
    public static SceneLoad Instance { get; private set; }

    public event Action<string> SceneLoaded;

    [SerializeField, Tooltip("Keep this loader alive across scene changes.")]
    bool keepAlive = true;

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

    void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void LoadScene(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("SceneLoad.LoadScene called with an empty scene name.", this);
            return;
        }

        SceneManager.LoadScene(sceneName, mode);
    }

    public void LoadScene(int buildIndex, LoadSceneMode mode = LoadSceneMode.Single)
    {
        if (buildIndex < 0)
        {
            Debug.LogWarning("SceneLoad.LoadScene called with an invalid build index.", this);
            return;
        }

        SceneManager.LoadScene(buildIndex, mode);
    }

    public void LoadScene(SceneReferenceSO sceneReference, LoadSceneMode mode = LoadSceneMode.Single)
    {
        if (sceneReference == null || !sceneReference.IsValid)
        {
            Debug.LogWarning("SceneLoad.LoadScene called with an invalid SceneReferenceSO.", this);
            return;
        }

        if (sceneReference.BuildIndex >= 0)
        {
            LoadScene(sceneReference.BuildIndex, mode);
            return;
        }

        LoadScene(sceneReference.SceneName, mode);
    }

    void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneLoaded?.Invoke(scene.name);
    }
}
