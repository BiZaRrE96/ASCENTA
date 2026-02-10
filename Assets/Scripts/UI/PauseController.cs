using ASCENTA.Events;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PauseController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] MovementController movementController;
    [SerializeField] SettingsWorker settingsWorker;
    [SerializeField] SceneReferenceSO menuScene;

    [Header("UI")]
    [SerializeField] GameObject pauseRoot;
    [SerializeField] GameObject settingsRoot;

    float cachedTimeScale = 1f;
    float cachedFixedDeltaTime = 0.02f;
    bool hasCachedTime;
    bool isPaused;

    void Awake()
    {
        if (movementController == null)
        {
            movementController = FindFirstObjectByType<MovementController>(FindObjectsInactive.Include);
        }

        if (settingsWorker == null)
        {
            settingsWorker = FindFirstObjectByType<SettingsWorker>(FindObjectsInactive.Include);
        }
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<RequestPauseEvent>(HandlePauseRequested);

        if (isPaused)
        {
            ForceResetTime();
            RestoreInput();
            isPaused = false;
        }

        HideSettings();
        hasCachedTime = false;
    }

    void OnEnable()
    {
        EventBus.Subscribe<RequestPauseEvent>(HandlePauseRequested);
    }

    public void TogglePause()
    {
        if (isPaused)
        {
            Resume();
        }
        else
        {
            Pause();
        }
    }

    public void Pause()
    {
        if (isPaused)
        {
            return;
        }

        CacheTime();
        ApplyPauseTime();
        BlockInput();
        ShowPauseUI();
        isPaused = true;
    }

    public void Resume()
    {
        if (!isPaused)
        {
            return;
        }

        RestoreTime();
        RestoreInput();
        HidePauseUI();
        isPaused = false;
        hasCachedTime = false;
    }

    public void ShowSettings()
    {
        if (settingsRoot != null)
        {
            settingsRoot.SetActive(true);
        }

        if (settingsWorker != null && !settingsWorker.enabled)
        {
            settingsWorker.enabled = true;
        }
    }

    public void HideSettings()
    {
        if (settingsRoot != null)
        {
            settingsRoot.SetActive(false);
        }

        if (settingsWorker != null && settingsWorker.enabled)
        {
            settingsWorker.enabled = false;
        }
    }

    public void BackToMenu()
    {
        ForceResetTime();
        RestoreInput();
        HidePauseUI();
        isPaused = false;
        hasCachedTime = false;

        SceneLoad loader = SceneLoad.Instance ?? FindFirstObjectByType<SceneLoad>(FindObjectsInactive.Include);
        if (loader == null)
        {
            Debug.LogWarning("PauseController could not find a SceneLoad instance.", this);
            return;
        }

        if (menuScene == null || !menuScene.IsValid)
        {
            Debug.LogWarning("PauseController has no valid menu SceneReferenceSO assigned.", this);
            return;
        }

        loader.LoadScene(menuScene);
    }

    void HandlePauseRequested(RequestPauseEvent eventData)
    {
        if (eventData.ShouldPause)
        {
            Pause();
        }
        else
        {
            Resume();
        }
    }

    void CacheTime()
    {
        cachedTimeScale = Time.timeScale;
        cachedFixedDeltaTime = Time.fixedDeltaTime;
        hasCachedTime = true;
    }

    void ApplyPauseTime()
    {
        Time.timeScale = 0f;
        Time.fixedDeltaTime = 0f;
    }

    void RestoreTime()
    {
        if (!hasCachedTime)
        {
            ForceResetTime();
            return;
        }

        Time.timeScale = cachedTimeScale;
        Time.fixedDeltaTime = cachedFixedDeltaTime;
    }

    void ForceResetTime()
    {
        if (TimeController.Instance != null)
        {
            TimeController.Instance.ForceResetTime();
        }
        else
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
        }
    }

    void BlockInput()
    {
        if (movementController == null)
        {
            return;
        }

        movementController.SetPlayerInputAllowed(false);
    }

    void RestoreInput()
    {
        if (movementController == null)
        {
            return;
        }

        movementController.SetPlayerInputAllowed(true);
    }

    void ShowPauseUI()
    {
        if (pauseRoot != null)
        {
            pauseRoot.SetActive(true);
        }

        HideSettings();
    }

    void HidePauseUI()
    {
        if (pauseRoot != null)
        {
            pauseRoot.SetActive(false);
        }

        HideSettings();
    }
}
