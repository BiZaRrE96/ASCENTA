using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Should load first no matter what
[DefaultExecutionOrder(-1000)]
public class Bootstrap : MonoBehaviour
{
    [Header("Scene Load")]
    [SerializeField] SceneReferenceSO nextScene;
    [SerializeField] bool loadAdditive;
    [SerializeField] bool keepBootstrapSceneLoaded = true;

    [Header("References")]
    [SerializeField] ConfigWorker configWorker;

    static bool hasBootstrapped;

    void Awake()
    {
        if (hasBootstrapped)
        {
            Destroy(gameObject);
            return;
        }

        hasBootstrapped = true;

        if (configWorker == null)
        {
            configWorker = FindFirstObjectByType<ConfigWorker>(FindObjectsInactive.Include);
        }

        if (keepBootstrapSceneLoaded)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    IEnumerator Start()
    {
        if (configWorker != null)
        {
            configWorker.EnsureInitialized();
        }
        else
        {
            Debug.LogWarning("Bootstrap could not find a ConfigWorker in the bootstrap scene.", this);
        }

        yield return null;

        LoadNextScene();
    }

    void LoadNextScene()
    {
        if (nextScene != null)
        {
            if (nextScene.BuildIndex >= 0)
            {
                SceneManager.LoadScene(nextScene.BuildIndex, loadAdditive ? LoadSceneMode.Additive : LoadSceneMode.Single);
                return;
            }

            if (!string.IsNullOrWhiteSpace(nextScene.SceneName))
            {
                SceneManager.LoadScene(nextScene.SceneName, loadAdditive ? LoadSceneMode.Additive : LoadSceneMode.Single);
                return;
            }
        }

        if (nextScene == null)
        {
            Debug.LogWarning("Bootstrap has no next scene configured. Assign a SceneReferenceSO.", this);
            return;
        }

        Debug.LogWarning("Bootstrap has an invalid SceneReferenceSO (not in build settings).", this);
    }
}
