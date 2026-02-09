using UnityEngine;
using ASCENTA.Events;

[DisallowMultipleComponent]
public class NewgameCutscene : EventBusListener<GameStartedEvent>
{
    [SerializeField] GameObject startPanel;
    [SerializeField] Transform startPoint;
    [SerializeField] Transform playerTransform;
    [SerializeField] MovementController movementController;
    [SerializeField] Animator cutsceneAnimator;
    [SerializeField] string animationStartTrigger = "Start";

    bool cutsceneActive;
    bool waitingForGround;
    bool inputLockedByCutscene;

    protected override void Awake()
    {
        base.Awake();

        if (playerTransform == null)
        {
            ResolvePlayer();
        }

        if (movementController == null && playerTransform != null)
        {
            movementController = playerTransform.GetComponent<MovementController>();
        }

        if (cutsceneAnimator == null)
        {
            cutsceneAnimator = GetComponentInChildren<Animator>(true);
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        UnsubscribeFromGrounded();
    }

    protected override void OnEvent(GameStartedEvent eventData)
    {
        Debug.Log("NEWGAMESCENE TRIGGERED");
        TryStartCutscene();
    }

    void TryStartCutscene()
    {
        if (cutsceneActive)
        {
            return;
        }

        if (HasPlayedIntro())
        {
            EventBus.Publish(new IntroStartEvent(false));
            DisableCutscene();
            return;
        }

        EventBus.Publish(new IntroStartEvent(true));
        BeginCutscene();
    }

    bool HasPlayedIntro()
    {
        DataPersistenceManager manager = DataPersistenceManager.Instance
            ?? FindFirstObjectByType<DataPersistenceManager>(FindObjectsInactive.Include);
        if (manager == null || manager.CurrentData == null || manager.CurrentData.playerData == null)
        {
            return false;
        }

        return manager.CurrentData.playerData.introPlayed;
    }

    void BeginCutscene()
    {
        cutsceneActive = true;

        if (startPanel != null && !startPanel.activeSelf)
        {
            startPanel.SetActive(true);
        }

        if (playerTransform == null)
        {
            ResolvePlayer();
        }

        Transform target = startPoint != null
            ? startPoint
            : (startPanel != null ? startPanel.transform : null);
        if (playerTransform != null && target != null)
        {
            playerTransform.SetPositionAndRotation(target.position, target.rotation);
        }

        if (movementController != null)
        {
            //movementController.SetPlayerInputAllowed(false);
            //inputLockedByCutscene = true;
            movementController.SetMovementState(MovementState.Cutscene, 0f);
        }

        if (cutsceneAnimator != null && !string.IsNullOrWhiteSpace(animationStartTrigger))
        {
            cutsceneAnimator.SetTrigger(animationStartTrigger);
        }
    }

    public void AnimationDone()
    {
        Debug.Log("ANIMATION DONE");
        if (!cutsceneActive)
        {
            Debug.LogWarning("[ANIMATION DONE] Cutscene is inactive");
            return;
        }

        if (startPanel != null && startPanel.activeSelf)
        {
            startPanel.SetActive(false);
            Debug.Log("Panel disabled");
        } else
        {
            Debug.LogError("Failed to inactivate panel");
        }

        SubscribeToGrounded();
    }

    void SubscribeToGrounded()
    {
        if (waitingForGround)
        {
            return;
        }

        waitingForGround = EventBus.Subscribe<GroundedChangedEvent>(HandleGroundedChanged);
    }

    void UnsubscribeFromGrounded()
    {
        if (!waitingForGround)
        {
            return;
        }

        EventBus.Unsubscribe<GroundedChangedEvent>(HandleGroundedChanged);
        waitingForGround = false;
    }

    void HandleGroundedChanged(GroundedChangedEvent eventData)
    {
        if (!eventData.IsGrounded)
        {
            return;
        }

        Debug.Log("Ending animation and disabling self...");

        UnsubscribeFromGrounded();
        AnimationClosing();
    }

    public void AnimationClosing()
    {
        if (!cutsceneActive)
        {
            Debug.LogWarning("[ANIMATION CLOSING] Cutscene is inactive");
            return;
        }

        cutsceneActive = false;

        if (movementController != null)
        {
            movementController.SetMovementState(MovementState.Default, 0f);
            // if (inputLockedByCutscene)
            // {
            //     movementController.SetPlayerInputAllowed(true);
            //     inputLockedByCutscene = false;
            // }
        }

        MarkIntroPlayed();
        EventBus.Publish(new IntroCompletedEvent());
        DisableCutscene();
    }

    void MarkIntroPlayed()
    {
        DataPersistenceManager manager = DataPersistenceManager.Instance
            ?? FindFirstObjectByType<DataPersistenceManager>(FindObjectsInactive.Include);
        if (manager == null || manager.CurrentData == null || manager.CurrentData.playerData == null)
        {
            return;
        }

        manager.CurrentData.playerData.introPlayed = true;
    }

    void DisableCutscene()
    {
        UnsubscribeFromGrounded();
        if (startPanel != null && startPanel.activeSelf)
        {
            startPanel.SetActive(false);
        }
        EventBus.Publish(new IntroUpdateEvent());
        gameObject.SetActive(false);
    }

    void ResolvePlayer()
    {
        MovementController controller = movementController != null
            ? movementController
            : FindFirstObjectByType<MovementController>(FindObjectsInactive.Include);
        if (controller != null)
        {
            movementController = controller;
            playerTransform = controller.transform;
        }
    }
}
