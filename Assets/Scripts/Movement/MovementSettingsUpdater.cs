using ASCENTA.Events;
using UnityEngine;

public sealed class MovementSettingsUpdater : EventBusListener<SettingsChangedEvent>
{
    [SerializeField] bool applyOnEnable = true;
    [SerializeField] float fallbackSensitivity = 0.15f;

    protected override void OnEnable()
    {
        base.OnEnable();

        if (applyOnEnable)
        {
            ApplyCurrentSettings();
        }
    }

    protected override void OnEvent(SettingsChangedEvent eventData)
    {
        float sensitivity = eventData.Config != null
            ? eventData.Config.mouseSensitivity
            : fallbackSensitivity;

        ApplySensitivity(sensitivity);
    }

    void ApplyCurrentSettings()
    {
        if (ConfigWorker.Instance != null)
        {
            ConfigWorker.Instance.EnsureInitialized();
        }

        float sensitivity = fallbackSensitivity;
        if (ConfigWorker.Instance != null && ConfigWorker.Instance.CurrentConfig != null)
        {
            sensitivity = ConfigWorker.Instance.CurrentConfig.mouseSensitivity;
        }

        ApplySensitivity(sensitivity);
    }

    void ApplySensitivity(float sensitivity)
    {
        MovementController[] controllers = FindObjectsByType<MovementController>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (MovementController controller in controllers)
        {
            if (controller == null)
            {
                continue;
            }

            controller.SetLookSensitivity(sensitivity);
        }
    }
}
