using System.Collections;
using ASCENTA.Events;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

[DisallowMultipleComponent]
public sealed class ButtonHoverMaster : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Scale")]
    [SerializeField] Transform target;
    [SerializeField] float hoverScaleMultiplier = 1.08f;
    [SerializeField] float scaleLerpSpeed = 12f;

    [Header("Timing")]
    [SerializeField, Min(0f)] float clickHoverDelay = 0.1f;
    [SerializeField, Min(0f)] float clickInvokeDelay = 0.1f;
    [SerializeField] bool lockOtherButtons = true;
    [SerializeField] UnityEvent onDelayedClick;

    bool isHovered;
    bool isEnabled = true;
    Vector3 baseScale;
    Vector3 targetScale;
    float nextHoverAllowedTime;
    Coroutine delayedClickRoutine;

    void Awake()
    {
        if (target == null)
        {
            target = transform;
        }

        baseScale = target.localScale;
        targetScale = baseScale;
    }

    void OnEnable()
    {
        if (isEnabled)
        {
            BroadcastMessage("OnButtonEnabled", SendMessageOptions.DontRequireReceiver);
        }
    }

    void OnDisable()
    {
        if (delayedClickRoutine != null)
        {
            StopCoroutine(delayedClickRoutine);
            delayedClickRoutine = null;
        }
        BroadcastMessage("OnButtonDisabled", SendMessageOptions.DontRequireReceiver);
    }

    void Update()
    {
        if (target == null)
        {
            return;
        }

        float dt = Time.unscaledDeltaTime;
        float speed = Mathf.Max(0f, scaleLerpSpeed);
        target.localScale = Vector3.Lerp(target.localScale, targetScale, 1f - Mathf.Exp(-speed * dt));
    }

    public void Enable()
    {
        if (isEnabled)
        {
            return;
        }

        isEnabled = true;
        BroadcastMessage("OnButtonEnabled", SendMessageOptions.DontRequireReceiver);

        if (isHovered)
        {
            ApplyHoverScale(true);
        }
        else
        {
            ApplyHoverScale(false);
        }
    }

    public void Disable()
    {
        if (!isEnabled)
        {
            return;
        }

        isEnabled = false;
        isHovered = false;
        targetScale = baseScale;
        BroadcastMessage("OnButtonDisabled", SendMessageOptions.DontRequireReceiver);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isEnabled)
        {
            return;
        }

        if (Time.unscaledTime < nextHoverAllowedTime)
        {
            return;
        }

        if (isHovered)
        {
            return;
        }

        isHovered = true;
        ApplyHoverScale(true);
        BroadcastMessage("OnHoverStart", SendMessageOptions.DontRequireReceiver);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isEnabled)
        {
            return;
        }

        if (!isHovered)
        {
            return;
        }

        isHovered = false;
        ApplyHoverScale(false);
        BroadcastMessage("OnHoverEnd", SendMessageOptions.DontRequireReceiver);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isEnabled)
        {
            return;
        }

        nextHoverAllowedTime = Time.unscaledTime + clickHoverDelay;
        BroadcastMessage("OnClick", SendMessageOptions.DontRequireReceiver);
        if (lockOtherButtons)
        {
            EventBus.Publish(new UIButtonLockEvent(true));
        }
        StartDelayedClick();
    }

    void ApplyHoverScale(bool hovered)
    {
        if (target == null)
        {
            return;
        }

        float multiplier = Mathf.Max(0.01f, hoverScaleMultiplier);
        targetScale = hovered ? baseScale * multiplier : baseScale;
    }

    void StartDelayedClick()
    {
        if (delayedClickRoutine != null)
        {
            StopCoroutine(delayedClickRoutine);
            delayedClickRoutine = null;
        }

        if (clickInvokeDelay <= 0f)
        {
            onDelayedClick?.Invoke();
            return;
        }

        delayedClickRoutine = StartCoroutine(DelayedClickRoutine());
    }

    IEnumerator DelayedClickRoutine()
    {
        float delay = Mathf.Max(0f, clickInvokeDelay);
        yield return new WaitForSecondsRealtime(delay);
        delayedClickRoutine = null;
        onDelayedClick?.Invoke();
    }
}
