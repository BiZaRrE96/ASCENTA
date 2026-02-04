using ASCENTA.Events;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class TutorialTimeStopTrigger : MonoBehaviour
{
    [SerializeField, Min(0.01f), Tooltip("Time scale that will be applied when the player enters this trigger.")]
    float slowMotionScale = 0.3f;

    [SerializeField, Tooltip("If true, the trigger only fires once. Otherwise it can fire every time the player re-enters after the grace period.")]
    bool singleUse = true;

    [SerializeField, Min(0f), Tooltip("Seconds after the trigger fires before the trigger can fire again.")]
    float gracePeriod = 1f;

    [SerializeField] GameObject tutorialObject;

    Collider triggerCollider;
    float nextAllowedTriggerTime;
    bool hasTriggered;
    bool isEffectActive;
    Collider activePlayerCollider;


    void Awake()
    {
        triggerCollider = GetComponent<Collider>();
        triggerCollider.isTrigger = true;
        gracePeriod = Mathf.Max(gracePeriod, 0f);
    }

    void OnEnable()
    {
        EventBus.Subscribe<OnDashEvent>(ResumeTimeOnDash);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<OnDashEvent>(ResumeTimeOnDash);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!CanTrigger())
        {
            return;
        }

        if (!IsPlayerCollider(other))
        {
            return;
        }

        if (TimeController.Instance == null)
        {
            Debug.LogWarning("TutorialTimeStopTrigger cannot find a TimeController instance.", this);
            return;
        }

        TimeController.Instance.EnterSlowMotion(slowMotionScale);
        showTutorial();
        isEffectActive = true;
        activePlayerCollider = other;
        nextAllowedTriggerTime = Time.time + gracePeriod;
    }

    void OnTriggerExit(Collider other)
    {
        if (!isEffectActive || other != activePlayerCollider)
        {
            return;
        }

        isEffectActive = false;
        activePlayerCollider = null;
        ResumeTimeCleanup();
    }

    void ResumeTimeCleanup()
    {
        if (TimeController.Instance == null)
        {
            return;
        }

        TimeController.Instance.ResumeNormalTime();
        hideTutorial();
        nextAllowedTriggerTime = Time.time + gracePeriod;
    }

    bool CanTrigger()
    {
        if (singleUse && hasTriggered)
        {
            return false;
        }

        return Time.time >= nextAllowedTriggerTime;
    }

    bool IsPlayerCollider(Collider other)
    {
        if (other.TryGetComponent<MovementController>(out _))
        {
            return true;
        }

        if (other.attachedRigidbody != null && other.attachedRigidbody.TryGetComponent<MovementController>(out _))
        {
            return true;
        }

        return other.GetComponentInParent<MovementController>() != null;
    }

    void ResumeTimeOnDash(OnDashEvent _)
    {
        if (!isEffectActive)
        {
            return;
        }

        isEffectActive = false;
        activePlayerCollider = null;
        ResumeTimeCleanup();

        if (singleUse)
        {
            hasTriggered = true;
        }
    }

    void showTutorial()
    {
        if (tutorialObject)
        {
            tutorialObject.SetActive(true);
        }
    }

    void hideTutorial()
    {
        if (tutorialObject)
        {
            tutorialObject.SetActive(false);
        }
    }
}
